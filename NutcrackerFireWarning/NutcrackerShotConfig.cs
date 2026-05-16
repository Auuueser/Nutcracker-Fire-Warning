using BepInEx.Configuration;

namespace NutcrackerShotUI;

internal static class NutcrackerShotConfig
{
    public static ConfigEntry<bool> EnableUiFireWindow { get; private set; }

    public static ConfigEntry<bool> EnableModelOutlineFireWindow { get; private set; }

    public static ConfigEntry<float> ModelOutlineWidth { get; private set; }

    public static ConfigEntry<ModelOutlineMode> ModelOutlineMode { get; private set; }

    public static ConfigEntry<ModelPulseMode> ModelPulseMode { get; private set; }

    public static ConfigEntry<float> MeshOutlineScale { get; private set; }

    public static ConfigEntry<float> ModelPulseIntensity { get; private set; }

    public static ConfigEntry<float> ModelPulseAlpha { get; private set; }

    public static ConfigEntry<float> ModelWarningMaxDistance { get; private set; }

    public static ConfigEntry<bool> ModelWarningRequireCameraVisible { get; private set; }

    public static ConfigEntry<float> FireWindowSeconds { get; private set; }

    public static ConfigEntry<float> PreAimMaxDistance { get; private set; }

    public static ConfigEntry<float> MonitorActiveScanInterval { get; private set; }

    public static ConfigEntry<float> MonitorIdleScanInterval { get; private set; }

    public static ConfigEntry<bool> EnableDebugLogs { get; private set; }

    public static ConfigEntry<bool> DumpModelAudit { get; private set; }

    public static void Bind(ConfigFile config)
    {
        EnableUiFireWindow = config.Bind(
            "Warnings",
            "EnableUiFireWindow",
            true,
            "Show the world-space side warning bar, including countdown, FIRE pulse, reload bar, and pre-aim danger bar.");

        EnableModelOutlineFireWindow = config.Bind(
            "Warnings",
            "EnableModelOutlineFireWindow",
            true,
            "Flash a red-white outline around the Nutcracker model during the final shot window.");

        ModelOutlineWidth = config.Bind(
            "Warnings",
            "ModelOutlineWidth",
            0.075f,
            "Line width for ScreenBox mode. In MeshSilhouette mode this is the normal-expanded edge thickness.");

        ModelOutlineMode = config.Bind(
            "Warnings",
            "ModelOutlineMode",
            NutcrackerShotUI.ModelOutlineMode.MeshSilhouette,
            "Model outline style. MeshSilhouette follows the Nutcracker model; ScreenBox draws a reliable HUD fallback rectangle.");

        ModelPulseMode = config.Bind(
            "Warnings",
            "ModelPulseMode",
            NutcrackerShotUI.ModelPulseMode.SourcePulse,
            "Model warning implementation for MeshSilhouette mode. SourcePulse recolors the original model; CloneShell adds a cloned shell; Both enables both.");

        MeshOutlineScale = config.Bind(
            "Warnings",
            "MeshOutlineScale",
            1f,
            "Extra transform scale for MeshSilhouette outline clones. Keep this at 1 for the most exact model edge.");

        ModelPulseIntensity = config.Bind(
            "Warnings",
            "ModelPulseIntensity",
            4f,
            "Emission multiplier for model red-white pulse.");

        ModelPulseAlpha = config.Bind(
            "Warnings",
            "ModelPulseAlpha",
            0.92f,
            "Alpha used by model red-white pulse colors.");

        ModelWarningMaxDistance = config.Bind(
            "Warnings",
            "ModelWarningMaxDistance",
            45f,
            "Maximum local-player distance for model fire-window warnings. Set to 0 or lower to disable distance filtering.");

        ModelWarningRequireCameraVisible = config.Bind(
            "Warnings",
            "ModelWarningRequireCameraVisible",
            false,
            "Only show model fire-window warnings when the Nutcracker is inside the local camera viewport.");

        FireWindowSeconds = config.Bind(
            "Warnings",
            "FireWindowSeconds",
            0.35f,
            "Seconds before the predicted shot when fire-window warnings activate.");

        PreAimMaxDistance = config.Bind(
            "Warnings",
            "PreAimMaxDistance",
            30f,
            "Maximum distance for the yellow pre-aim danger bar. Set to 0 or lower to disable the pre-aim bar.");

        MonitorActiveScanInterval = config.Bind(
            "Performance",
            "MonitorActiveScanInterval",
            0.1f,
            "Fallback monitor scan interval while Nutcrackers are present.");

        MonitorIdleScanInterval = config.Bind(
            "Performance",
            "MonitorIdleScanInterval",
            0.75f,
            "Fallback monitor scan interval while no Nutcrackers are present.");

        EnableDebugLogs = config.Bind(
            "Debug",
            "EnableDebugLogs",
            false,
            "Log aim/fire warning events to BepInEx LogOutput.log.");

        DumpModelAudit = config.Bind(
            "Debug",
            "DumpModelAudit",
            false,
            "Log Nutcracker renderer and mesh names/counts once when the model outline is built.");
    }
}

internal enum ModelOutlineMode
{
    MeshSilhouette,
    ScreenBox
}

internal enum ModelPulseMode
{
    SourcePulse,
    CloneShell,
    Both
}
