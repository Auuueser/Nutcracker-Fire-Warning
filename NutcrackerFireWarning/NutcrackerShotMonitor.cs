using UnityEngine;

namespace NutcrackerShotUI;

internal sealed class NutcrackerShotMonitor : MonoBehaviour
{
    private float nextScanTime;
    private int lastObservedCount = -1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            nextScanTime = Time.time + GetConfigInterval(NutcrackerShotConfig.MonitorIdleScanInterval, 2f);
            return;
        }

        if (Time.time < nextScanTime)
        {
            return;
        }

        NutcrackerEnemyAI[] nutcrackers = FindObjectsOfType<NutcrackerEnemyAI>();
        nextScanTime = Time.time + GetNextScanInterval(nutcrackers.Length);

        if (IsDebugLoggingEnabled() && nutcrackers.Length != lastObservedCount)
        {
            lastObservedCount = nutcrackers.Length;
            Plugin.Log.LogInfo($"Nutcracker monitor found {nutcrackers.Length} NutcrackerEnemyAI instance(s).");
        }

        for (int i = 0; i < nutcrackers.Length; i++)
        {
            NutcrackerEnemyAI nutcracker = nutcrackers[i];
            if (nutcracker == null || nutcracker.isEnemyDead)
            {
                continue;
            }

            if (!NutcrackerCombatStateReader.TryRead(nutcracker, out NutcrackerCombatState combatState))
            {
                continue;
            }

            NutcrackerShotIndicator indicator = NutcrackerShotIndicator.GetExisting(nutcracker);
            if (indicator == null && NutcrackerShotIndicator.ShouldCreateForCombatState(nutcracker, combatState))
            {
                indicator = NutcrackerShotIndicator.For(nutcracker);
            }

            indicator?.ObserveCombatState(combatState);
        }
    }

    private static float GetNextScanInterval(int observedCount)
    {
        float interval = observedCount > 0
            ? GetConfigInterval(NutcrackerShotConfig.MonitorActiveScanInterval, 0.5f)
            : GetConfigInterval(NutcrackerShotConfig.MonitorIdleScanInterval, 2f);

        return Mathf.Clamp(interval, 0.02f, 5f);
    }

    private static float GetConfigInterval(BepInEx.Configuration.ConfigEntry<float> entry, float fallback)
    {
        return entry == null ? fallback : entry.Value;
    }

    private static bool IsDebugLoggingEnabled()
    {
        return NutcrackerShotConfig.EnableDebugLogs != null && NutcrackerShotConfig.EnableDebugLogs.Value;
    }
}
