using HarmonyLib;

namespace NutcrackerShotUI;

internal readonly struct NutcrackerCombatState
{
    public readonly bool AimingGun;
    public readonly bool ReloadingGun;
    public readonly float TimeSinceFiringGun;
    public readonly float AimDuration;

    public NutcrackerCombatState(bool aimingGun, bool reloadingGun, float timeSinceFiringGun, float aimDuration)
    {
        AimingGun = aimingGun;
        ReloadingGun = reloadingGun;
        TimeSinceFiringGun = timeSinceFiringGun;
        AimDuration = aimDuration;
    }
}

internal static class NutcrackerCombatStateReader
{
    private static readonly AccessTools.FieldRef<NutcrackerEnemyAI, bool> AimingGunRef = CreateFieldRef<bool>("aimingGun");
    private static readonly AccessTools.FieldRef<NutcrackerEnemyAI, bool> ReloadingGunRef = CreateFieldRef<bool>("reloadingGun");
    private static readonly AccessTools.FieldRef<NutcrackerEnemyAI, float> TimeSinceFiringGunRef = CreateFieldRef<float>("timeSinceFiringGun");

    public static bool TryRead(NutcrackerEnemyAI nutcracker, out NutcrackerCombatState combatState)
    {
        combatState = default;

        if (nutcracker == null || AimingGunRef == null || ReloadingGunRef == null || TimeSinceFiringGunRef == null)
        {
            return false;
        }

        combatState = new NutcrackerCombatState(
            AimingGunRef(nutcracker),
            ReloadingGunRef(nutcracker),
            TimeSinceFiringGunRef(nutcracker),
            GetAimDuration(nutcracker));

        return true;
    }

    public static float GetAimDuration(NutcrackerEnemyAI nutcracker)
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

    private static AccessTools.FieldRef<NutcrackerEnemyAI, T> CreateFieldRef<T>(string fieldName)
    {
        try
        {
            return AccessTools.FieldRefAccess<NutcrackerEnemyAI, T>(fieldName);
        }
        catch
        {
            return null;
        }
    }
}
