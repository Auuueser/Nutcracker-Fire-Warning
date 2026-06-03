using BepInEx.Configuration;

namespace NutcrackerShotUI;

internal static class NutcrackerShotConfig
{
    internal const bool DefaultEnableModelOutlineFireWindow = false;
    internal const bool DefaultEnableModelStateTint = true;

    private static bool useChineseDescriptions;

    public static ConfigEntry<bool> EnableMod { get; private set; }

    public static ConfigEntry<bool> EnableUiFireWindow { get; private set; }

    public static ConfigEntry<bool> EnableModelOutlineFireWindow { get; private set; }

    public static ConfigEntry<bool> EnableModelStateTint { get; private set; }

    public static ConfigEntry<float> ModelOutlineWidth { get; private set; }

    public static ConfigEntry<ModelOutlineMode> ModelOutlineMode { get; private set; }

    public static ConfigEntry<ModelPulseMode> ModelPulseMode { get; private set; }

    public static ConfigEntry<float> MeshOutlineScale { get; private set; }

    public static ConfigEntry<float> ModelPulseIntensity { get; private set; }

    public static ConfigEntry<float> ModelPulseAlpha { get; private set; }

    public static ConfigEntry<float> ModelChaseTintAlpha { get; private set; }

    public static ConfigEntry<float> ModelChaseTintIntensity { get; private set; }

    public static ConfigEntry<float> ModelFireWindowTintAlpha { get; private set; }

    public static ConfigEntry<float> ModelFireWindowTintIntensity { get; private set; }

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
        useChineseDescriptions = NutcrackerConfigLanguageDetector.ShouldUseChineseConfig();

        EnableMod = config.Bind(
            "General",
            "EnableMod",
            true,
            Description(
                "Enable Nutcracker Fire Warning. When disabled, the plugin does not create or update warning UI, model warnings, or fallback scans.",
                "启用 Nutcracker Fire Warning 总开关。关闭后不会创建或更新预警 UI、模型预警或备用扫描。"));

        EnableUiFireWindow = config.Bind(
            "Warnings",
            "EnableUiFireWindow",
            true,
            Description(
                "Show the world-space side warning bar, including countdown, FIRE pulse, reload bar, and pre-aim danger bar.",
                "显示胡桃身侧的世界空间竖条 UI，包括倒计时、FIRE 脉冲、换弹条和预瞄危险提示。"));

        EnableModelOutlineFireWindow = config.Bind(
            "Warnings",
            "EnableModelOutlineFireWindow",
            DefaultEnableModelOutlineFireWindow,
            Description(
                "Enable the extra fire-window-only model overlay: red-white pulse, clone shell, or screen box depending on ModelOutlineMode and ModelPulseMode. Usually keep this disabled when EnableModelStateTint is enabled.",
                "启用仅在最终开火窗口出现的额外模型叠加预警：根据 ModelOutlineMode 和 ModelPulseMode 显示红白脉冲、克隆外壳或屏幕框。启用 EnableModelStateTint 时通常保持关闭。"));

        EnableModelStateTint = config.Bind(
            "Warnings",
            "EnableModelStateTint",
            DefaultEnableModelStateTint,
            Description(
                "Enable the recommended model state warning: white while the Nutcracker is chasing a target, red during the final fire window.",
                "启用推荐的模型状态预警：胡桃追击目标时模型变白，进入最终开火窗口时模型变红。"));

        ModelOutlineWidth = config.Bind(
            "Warnings",
            "ModelOutlineWidth",
            0.075f,
            Description(
                "Line width for ScreenBox mode. In MeshSilhouette mode this is the normal-expanded edge thickness.",
                "ScreenBox 模式的线宽；MeshSilhouette 模式下为法线扩展轮廓厚度。"));

        ModelOutlineMode = config.Bind(
            "Warnings",
            "ModelOutlineMode",
            NutcrackerShotUI.ModelOutlineMode.MeshSilhouette,
            Description(
                "Model outline style. MeshSilhouette follows the Nutcracker model; ScreenBox draws a reliable HUD fallback rectangle.",
                "模型轮廓样式。MeshSilhouette 贴合胡桃模型；ScreenBox 绘制稳定的屏幕空间备用矩形。"));

        ModelPulseMode = config.Bind(
            "Warnings",
            "ModelPulseMode",
            NutcrackerShotUI.ModelPulseMode.SourcePulse,
            Description(
                "Implementation used by the extra fire-window overlay in MeshSilhouette mode. SourcePulse recolors the original model; CloneShell adds a cloned shell; Both enables both.",
                "MeshSilhouette 模式下额外开火窗口叠加层的实现方式。SourcePulse 重染原模型；CloneShell 添加克隆外壳；Both 同时启用两者。"));

        MeshOutlineScale = config.Bind(
            "Warnings",
            "MeshOutlineScale",
            1f,
            Description(
                "Extra transform scale for MeshSilhouette outline clones. Keep this at 1 for the most exact model edge.",
                "MeshSilhouette 克隆轮廓的额外缩放。保持 1 可获得最贴近模型边缘的效果。"));

        ModelPulseIntensity = config.Bind(
            "Warnings",
            "ModelPulseIntensity",
            4f,
            Description(
                "Emission multiplier for the extra red-white fire-window overlay.",
                "额外红白开火窗口叠加层的发光强度倍率。"));

        ModelPulseAlpha = config.Bind(
            "Warnings",
            "ModelPulseAlpha",
            0.92f,
            Description(
                "Alpha used by the extra red-white fire-window overlay.",
                "额外红白开火窗口叠加层的不透明度。"));

        ModelChaseTintAlpha = config.Bind(
            "Warnings",
            "ModelChaseTintAlpha",
            0.58f,
            Description(
                "Alpha used when the Nutcracker model is tinted white while chasing.",
                "胡桃追击目标时白色模型染色的不透明度。"));

        ModelChaseTintIntensity = config.Bind(
            "Warnings",
            "ModelChaseTintIntensity",
            1.8f,
            Description(
                "Emission multiplier for the white chase-state model tint.",
                "胡桃追击目标时白色模型染色的发光强度倍率。"));

        ModelFireWindowTintAlpha = config.Bind(
            "Warnings",
            "ModelFireWindowTintAlpha",
            0.96f,
            Description(
                "Alpha used when the Nutcracker model is tinted red during the final fire window.",
                "胡桃进入最终开火窗口时红色模型染色的不透明度。"));

        ModelFireWindowTintIntensity = config.Bind(
            "Warnings",
            "ModelFireWindowTintIntensity",
            5f,
            Description(
                "Emission multiplier for the red fire-window model tint.",
                "胡桃进入最终开火窗口时红色模型染色的发光强度倍率。"));

        ModelWarningMaxDistance = config.Bind(
            "Warnings",
            "ModelWarningMaxDistance",
            45f,
            Description(
                "Maximum local-player distance for model warnings. Set to 0 or lower to disable distance filtering.",
                "模型预警相对本地玩家的最大显示距离。设为 0 或更低可关闭距离过滤。"));

        ModelWarningRequireCameraVisible = config.Bind(
            "Warnings",
            "ModelWarningRequireCameraVisible",
            false,
            Description(
                "Only show model warnings when the Nutcracker is inside the local camera viewport.",
                "仅当胡桃位于本地摄像机视野内时显示模型预警。"));

        FireWindowSeconds = config.Bind(
            "Warnings",
            "FireWindowSeconds",
            0.35f,
            Description(
                "Seconds before the predicted shot when fire-window warnings activate.",
                "预测开火前多少秒进入最终开火窗口预警。"));

        PreAimMaxDistance = config.Bind(
            "Warnings",
            "PreAimMaxDistance",
            30f,
            Description(
                "Maximum distance for the yellow pre-aim danger bar. Set to 0 or lower to disable the pre-aim bar.",
                "黄色预瞄危险条的最大距离。设为 0 或更低可关闭预瞄条。"));

        MonitorActiveScanInterval = config.Bind(
            "Performance",
            "MonitorActiveScanInterval",
            0.5f,
            Description(
                "Fallback monitor scan interval while Nutcrackers are present.",
                "场上存在胡桃时，备用监视器的扫描间隔。"));

        MonitorIdleScanInterval = config.Bind(
            "Performance",
            "MonitorIdleScanInterval",
            2f,
            Description(
                "Fallback monitor scan interval while no Nutcrackers are present.",
                "场上没有胡桃时，备用监视器的扫描间隔。"));

        EnableDebugLogs = config.Bind(
            "Debug",
            "EnableDebugLogs",
            false,
            Description(
                "Log aim/fire warning events to BepInEx LogOutput.log.",
                "将瞄准/开火预警事件记录到 BepInEx LogOutput.log。"));

        DumpModelAudit = config.Bind(
            "Debug",
            "DumpModelAudit",
            false,
            Description(
                "Log Nutcracker renderer and mesh names/counts once when the model outline is built.",
                "构建模型轮廓时记录一次胡桃 Renderer 和 Mesh 的名称/数量。"));
    }

    public static bool IsModEnabled()
    {
        return EnableMod == null || EnableMod.Value;
    }

    private static string Description(string english, string chinese)
    {
        return useChineseDescriptions ? chinese : english;
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
