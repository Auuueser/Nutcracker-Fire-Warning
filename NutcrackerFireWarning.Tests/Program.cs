using System;
using System.Collections.Generic;
using System.IO;
using NutcrackerShotUI;

internal static class Program
{
    private static int Main()
    {
        int failures = 0;

        failures += Check(
            "Chinese config is selected by stable LC Chinese Project plugin GUID",
            NutcrackerConfigLanguageDetector.ShouldUseChineseConfig(new[] { "cn.codex.v81testchn" }, null));

        string pluginRoot = Path.Combine(Path.GetTempPath(), "nutcracker-fire-warning-tests", Guid.NewGuid().ToString("N"));
        try
        {
            string pluginDll = Path.Combine(pluginRoot, "BepInEx", "plugins", "V81TestChn", "V81TestChn.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(pluginDll));
            File.WriteAllBytes(pluginDll, Array.Empty<byte>());

            failures += Check(
                "Chinese config is selected by LC Chinese Project DLL path without version checks",
                NutcrackerConfigLanguageDetector.ShouldUseChineseConfig(Array.Empty<string>(), Path.Combine(pluginRoot, "BepInEx", "plugins")));
        }
        finally
        {
            if (Directory.Exists(pluginRoot))
            {
                Directory.Delete(pluginRoot, recursive: true);
            }
        }

        failures += Check(
            "English config remains selected when LC Chinese Project is absent",
            !NutcrackerConfigLanguageDetector.ShouldUseChineseConfig(new[] { "other.plugin" }, null));

        failures += Check(
            "Fire window model warning phase has priority over chase tint",
            NutcrackerModelWarningPhaseSelector.Select(modelEnabled: true, stateTintEnabled: true, fireWindowActive: true, chaseActive: true) == ModelWarningPhase.FireWindow);

        failures += Check(
            "Chase tint phase is selected when chasing but not in fire window",
            NutcrackerModelWarningPhaseSelector.Select(modelEnabled: true, stateTintEnabled: true, fireWindowActive: false, chaseActive: true) == ModelWarningPhase.Chase);

        failures += Check(
            "Model warning phase is none when model warning is disabled",
            NutcrackerModelWarningPhaseSelector.Select(modelEnabled: false, stateTintEnabled: true, fireWindowActive: true, chaseActive: true) == ModelWarningPhase.None);

        failures += Check(
            "Default model state tint is enabled",
            NutcrackerShotConfig.DefaultEnableModelStateTint);

        failures += Check(
            "Default extra model fire-window overlay is disabled",
            !NutcrackerShotConfig.DefaultEnableModelOutlineFireWindow);

        if (failures > 0)
        {
            Console.Error.WriteLine($"{failures} test(s) failed.");
            return 1;
        }

        Console.WriteLine("All tests passed.");
        return 0;
    }

    private static int Check(string name, bool passed)
    {
        if (passed)
        {
            Console.WriteLine($"PASS {name}");
            return 0;
        }

        Console.Error.WriteLine($"FAIL {name}");
        return 1;
    }
}
