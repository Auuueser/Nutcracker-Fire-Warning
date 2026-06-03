using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NutcrackerShotUI;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "aueser.lethalcompany.nutcrackerfirewarning";
    public const string PluginName = "Nutcracker Fire Warning";
    public const string PluginVersion = "1.0.5";

    internal static ManualLogSource Log { get; private set; }

    private Harmony harmony;

    private void Awake()
    {
        Log = Logger;
        NutcrackerShotConfig.Bind(Config);
        CreateMonitor();
        harmony = new Harmony(PluginGuid);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded. enabled={NutcrackerShotConfig.EnableMod.Value}, UI fire window={NutcrackerShotConfig.EnableUiFireWindow.Value}, model outline={NutcrackerShotConfig.EnableModelOutlineFireWindow.Value}, debug={NutcrackerShotConfig.EnableDebugLogs.Value}.");
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }

    private static void CreateMonitor()
    {
        GameObject monitorObject = new GameObject("NutcrackerShotUIMonitor");
        monitorObject.hideFlags = HideFlags.HideAndDontSave;
        monitorObject.AddComponent<NutcrackerShotMonitor>();
    }
}
