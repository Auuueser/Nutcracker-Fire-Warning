namespace NutcrackerShotUI;

internal enum ModelWarningPhase
{
    None,
    Chase,
    FireWindow
}

internal static class NutcrackerModelWarningPhaseSelector
{
    public static ModelWarningPhase Select(bool modelEnabled, bool stateTintEnabled, bool fireWindowActive, bool chaseActive)
    {
        if (!modelEnabled)
        {
            return ModelWarningPhase.None;
        }

        if (fireWindowActive)
        {
            return ModelWarningPhase.FireWindow;
        }

        return stateTintEnabled && chaseActive ? ModelWarningPhase.Chase : ModelWarningPhase.None;
    }
}
