using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NutcrackerShotUI;

[HarmonyPatch(typeof(NutcrackerEnemyAI))]
internal static class NutcrackerPatches
{
    private static readonly FieldInfo AimingGunField = AccessTools.Field(typeof(NutcrackerEnemyAI), "aimingGun");
    private static readonly FieldInfo ReloadingGunField = AccessTools.Field(typeof(NutcrackerEnemyAI), "reloadingGun");
    private static readonly FieldInfo TimeSinceFiringGunField = AccessTools.Field(typeof(NutcrackerEnemyAI), "timeSinceFiringGun");

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.Update))]
    private static void UpdatePostfix(NutcrackerEnemyAI __instance)
    {
        if (__instance == null || __instance.isEnemyDead)
        {
            return;
        }

        bool aimingGun = ReadBool(AimingGunField, __instance);
        bool reloadingGun = ReadBool(ReloadingGunField, __instance);
        float timeSinceFiringGun = ReadFloat(TimeSinceFiringGunField, __instance);

        NutcrackerShotIndicator.For(__instance)
            .ObserveCombatState(aimingGun, reloadingGun, timeSinceFiringGun, GetAimDuration(__instance));
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.AimGunClientRpc))]
    private static void AimGunClientRpcPostfix(NutcrackerEnemyAI __instance)
    {
        NutcrackerShotIndicator.For(__instance).BeginAim(GetAimDuration(__instance));
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.FireGunClientRpc))]
    private static void FireGunClientRpcPostfix(NutcrackerEnemyAI __instance)
    {
        NutcrackerShotIndicator.For(__instance).MarkFired();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
    private static void ReloadGunClientRpcPostfix(NutcrackerEnemyAI __instance)
    {
        NutcrackerShotIndicator.For(__instance).BeginReload(1.74f);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.KillEnemy))]
    private static void KillEnemyPostfix(NutcrackerEnemyAI __instance)
    {
        NutcrackerShotIndicator.Remove(__instance);
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
}
