using HarmonyLib;

namespace NutcrackerShotUI;

[HarmonyPatch(typeof(NutcrackerEnemyAI))]
internal static class NutcrackerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.Update))]
    private static void UpdatePostfix(NutcrackerEnemyAI __instance)
    {
        if (!NutcrackerShotConfig.IsModEnabled() || __instance == null || __instance.isEnemyDead)
        {
            return;
        }

        if (!NutcrackerCombatStateReader.TryRead(__instance, out NutcrackerCombatState combatState))
        {
            return;
        }

        NutcrackerShotIndicator indicator = NutcrackerShotIndicator.GetExisting(__instance);
        if (indicator == null && NutcrackerShotIndicator.ShouldCreateForCombatState(__instance, combatState))
        {
            indicator = NutcrackerShotIndicator.For(__instance);
        }

        indicator?.ObserveCombatState(combatState);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.AimGunClientRpc))]
    private static void AimGunClientRpcPostfix(NutcrackerEnemyAI __instance)
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        NutcrackerShotIndicator.For(__instance)?.BeginAim(NutcrackerCombatStateReader.GetAimDuration(__instance));
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.FireGunClientRpc))]
    private static void FireGunClientRpcPostfix(NutcrackerEnemyAI __instance)
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        NutcrackerShotIndicator.For(__instance)?.MarkFired();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
    private static void ReloadGunClientRpcPostfix(NutcrackerEnemyAI __instance)
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        NutcrackerShotIndicator.For(__instance)?.BeginReload(1.74f);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NutcrackerEnemyAI.KillEnemy))]
    private static void KillEnemyPostfix(NutcrackerEnemyAI __instance)
    {
        NutcrackerShotIndicator.Remove(__instance);
    }
}
