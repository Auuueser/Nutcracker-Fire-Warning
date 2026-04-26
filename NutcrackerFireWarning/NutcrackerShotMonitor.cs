using System.Reflection;
using UnityEngine;

namespace NutcrackerShotUI;

internal sealed class NutcrackerShotMonitor : MonoBehaviour
{
    private static readonly FieldInfo AimingGunField = typeof(NutcrackerEnemyAI).GetField("aimingGun", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ReloadingGunField = typeof(NutcrackerEnemyAI).GetField("reloadingGun", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TimeSinceFiringGunField = typeof(NutcrackerEnemyAI).GetField("timeSinceFiringGun", BindingFlags.Instance | BindingFlags.NonPublic);

    private float nextScanTime;
    private int lastObservedCount = -1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
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

            bool aimingGun = ReadBool(AimingGunField, nutcracker);
            bool reloadingGun = ReadBool(ReloadingGunField, nutcracker);
            float timeSinceFiringGun = ReadFloat(TimeSinceFiringGunField, nutcracker);

            NutcrackerShotIndicator.For(nutcracker)
                .ObserveCombatState(aimingGun, reloadingGun, timeSinceFiringGun, GetAimDuration(nutcracker));
        }
    }

    private static float GetAimDuration(NutcrackerEnemyAI nutcracker)
    {
        if (nutcracker.enemyHP <= 1)
        {
            return 0.5f;
        }

        ShotgunItem gun = nutcracker.gun;
        if (gun != null && gun.shellsLoaded == 1)
        {
            return 1.3f;
        }

        return 1.75f;
    }

    private static bool ReadBool(FieldInfo field, NutcrackerEnemyAI instance)
    {
        return field != null && field.GetValue(instance) is bool value && value;
    }

    private static float ReadFloat(FieldInfo field, NutcrackerEnemyAI instance)
    {
        return field != null && field.GetValue(instance) is float value ? value : 0f;
    }

    private static float GetNextScanInterval(int observedCount)
    {
        float interval = observedCount > 0
            ? GetConfigInterval(NutcrackerShotConfig.MonitorActiveScanInterval, 0.1f)
            : GetConfigInterval(NutcrackerShotConfig.MonitorIdleScanInterval, 0.75f);

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
