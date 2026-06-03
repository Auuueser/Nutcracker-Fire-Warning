using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;

namespace NutcrackerShotUI;

internal static class NutcrackerConfigLanguageDetector
{
    internal const string ChineseProjectGuid = "cn.codex.v81testchn";
    private const string ChineseProjectFolderName = "V81TestChn";
    private const string ChineseProjectDllName = "V81TestChn.dll";

    public static bool ShouldUseChineseConfig()
    {
        try
        {
            if (Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(ChineseProjectGuid))
            {
                return true;
            }
        }
        catch
        {
            // Fall through to file-system detection. Config language is non-critical.
        }

        return ShouldUseChineseConfig(null, Paths.PluginPath);
    }

    internal static bool ShouldUseChineseConfig(IEnumerable<string> pluginGuids, string pluginRoot)
    {
        if (ContainsChineseProjectGuid(pluginGuids))
        {
            return true;
        }

        return ContainsChineseProjectDll(pluginRoot);
    }

    private static bool ContainsChineseProjectGuid(IEnumerable<string> pluginGuids)
    {
        if (pluginGuids == null)
        {
            return false;
        }

        foreach (string pluginGuid in pluginGuids)
        {
            if (string.Equals(pluginGuid, ChineseProjectGuid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsChineseProjectDll(string pluginRoot)
    {
        if (string.IsNullOrWhiteSpace(pluginRoot) || !Directory.Exists(pluginRoot))
        {
            return false;
        }

        string expectedPath = Path.Combine(pluginRoot, ChineseProjectFolderName, ChineseProjectDllName);
        if (File.Exists(expectedPath) || File.Exists(Path.Combine(pluginRoot, ChineseProjectDllName)))
        {
            return true;
        }

        try
        {
            foreach (string file in Directory.EnumerateFiles(pluginRoot, ChineseProjectDllName, SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(file), ChineseProjectDllName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
