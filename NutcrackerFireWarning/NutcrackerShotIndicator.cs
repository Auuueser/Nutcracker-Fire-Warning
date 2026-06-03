using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.UI;

namespace NutcrackerShotUI;

internal sealed class NutcrackerShotIndicator : MonoBehaviour
{
    private const float BarWidth = 0.09f;
    private const float BarHeight = 1.35f;
    private const float SideOffset = 1.08f;
    private const float HeightOffset = 2.05f;
    private const float FiredHoldTime = 0.35f;
    private const float DefaultCriticalAimWindow = 0.35f;
    private const float PreAimMaxDistance = 30f;
    private const float PreAimCheckInterval = 0.12f;
    private const float PreAimAngleDegrees = 35f;

    private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.55f);
    private static readonly Color PreAimColor = new Color(1f, 0.72f, 0.12f, 0.55f);
    private static readonly Color AimColor = new Color(1f, 0.12f, 0.06f, 0.92f);
    private static readonly Color FiredColor = new Color(1f, 1f, 1f, 0.96f);
    private static readonly Color ReloadColor = new Color(0.2f, 0.65f, 1f, 0.55f);
    private static readonly float PreAimMinimumDot = Mathf.Cos(PreAimAngleDegrees * Mathf.Deg2Rad);
    private static readonly string[] CountdownLabels = CreateCountdownLabels();
    private static readonly Dictionary<NutcrackerEnemyAI, NutcrackerShotIndicator> Indicators = new Dictionary<NutcrackerEnemyAI, NutcrackerShotIndicator>();

    private NutcrackerEnemyAI nutcracker;
    private Canvas canvas;
    private Image background;
    private Image fill;
    private Image rim;
    private Image criticalPulse;
    private Text countdownText;
    private NutcrackerModelFireWindowOutline modelOutline;

    private WarningState state;
    private float stateStartedAt;
    private float stateDuration;
    private float lastPreAimAmount;
    private float nextPreAimCheckTime;
    private float cachedPreAimDanger;
    private bool observedAimingLastFrame;
    private bool observedReloadingLastFrame;
    private NutcrackerCombatState lastCombatState;

    public static NutcrackerShotIndicator For(NutcrackerEnemyAI nutcracker)
    {
        if (!NutcrackerShotConfig.IsModEnabled() || nutcracker == null)
        {
            return null;
        }

        NutcrackerShotIndicator indicator = GetExisting(nutcracker);
        if (indicator != null)
        {
            return indicator;
        }

        GameObject holder = new GameObject("NutcrackerShotUI");
        holder.transform.SetParent(nutcracker.transform, worldPositionStays: false);
        indicator = holder.AddComponent<NutcrackerShotIndicator>();
        indicator.Initialize(nutcracker);
        Indicators[nutcracker] = indicator;
        return indicator;
    }

    public static NutcrackerShotIndicator GetExisting(NutcrackerEnemyAI nutcracker)
    {
        if (nutcracker == null)
        {
            return null;
        }

        if (Indicators.TryGetValue(nutcracker, out NutcrackerShotIndicator indicator))
        {
            if (indicator != null)
            {
                return indicator;
            }

            Indicators.Remove(nutcracker);
        }

        return null;
    }

    public static bool ShouldCreateForCombatState(NutcrackerEnemyAI nutcracker, NutcrackerCombatState combatState)
    {
        if (!NutcrackerShotConfig.IsModEnabled() || nutcracker == null || nutcracker.isEnemyDead)
        {
            return false;
        }

        if (combatState.AimingGun || combatState.ReloadingGun)
        {
            return true;
        }

        if (IsModelStateTintEnabled() && IsChaseTintCandidate(nutcracker, combatState))
        {
            return true;
        }

        return IsUiFireWindowEnabled()
            && GetPreAimMaxDistance() > 0f
            && nutcracker.currentBehaviourStateIndex == 2
            && nutcracker.gun != null
            && nutcracker.gun.shellsLoaded > 0;
    }

    public static void Remove(NutcrackerEnemyAI nutcracker)
    {
        if (nutcracker == null)
        {
            return;
        }

        NutcrackerShotIndicator indicator = GetExisting(nutcracker);
        if (indicator != null)
        {
            Indicators.Remove(nutcracker);
            Destroy(indicator.gameObject);
        }
    }

    public void BeginAim(float duration)
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        if (duration <= 0f)
        {
            duration = 0.5f;
        }

        state = WarningState.Aiming;
        stateStartedAt = Time.time;
        stateDuration = duration;
        SetVisible(true);

        if (IsDebugLoggingEnabled())
        {
            Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} aim warning started. Duration={duration:0.00}s Shells={nutcracker.gun?.shellsLoaded ?? -1} HP={nutcracker.enemyHP}");
        }
    }

    public void MarkFired()
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        state = WarningState.Fired;
        stateStartedAt = Time.time;
        stateDuration = FiredHoldTime;
        SetVisible(true);

        if (IsDebugLoggingEnabled())
        {
            Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} fired.");
        }
    }

    public void BeginReload(float duration)
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        state = WarningState.Reloading;
        stateStartedAt = Time.time;
        stateDuration = duration;
        SetVisible(true);
    }

    public void ObserveCombatState(bool aimingGun, bool reloadingGun, float timeSinceFiringGun, float aimDuration)
    {
        if (!NutcrackerShotConfig.IsModEnabled())
        {
            return;
        }

        if (aimingGun)
        {
            float duration = Mathf.Max(0.05f, aimDuration);
            float elapsed = Mathf.Clamp(timeSinceFiringGun, 0f, duration);

            if (!observedAimingLastFrame || state != WarningState.Aiming)
            {
                state = WarningState.Aiming;
                stateDuration = duration;
                stateStartedAt = Time.time - elapsed;
                SetVisible(true);

                if (IsDebugLoggingEnabled())
                {
                    Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} aim warning observed from Update. Duration={duration:0.00}s Elapsed={elapsed:0.00}s Shells={nutcracker.gun?.shellsLoaded ?? -1} HP={nutcracker.enemyHP}");
                }
            }
        }
        else if (observedAimingLastFrame && state == WarningState.Aiming)
        {
            state = WarningState.Idle;
            SetCriticalPulse(0f);
            UpdateModelWarningPhase(fireWindowActive: false);
        }

        if (reloadingGun && !observedReloadingLastFrame && state != WarningState.Aiming)
        {
            BeginReload(1.74f);
        }

        observedAimingLastFrame = aimingGun;
        observedReloadingLastFrame = reloadingGun;
    }

    public void ObserveCombatState(NutcrackerCombatState combatState)
    {
        lastCombatState = combatState;
        ObserveCombatState(combatState.AimingGun, combatState.ReloadingGun, combatState.TimeSinceFiringGun, combatState.AimDuration);
    }

    private void Initialize(NutcrackerEnemyAI owner)
    {
        nutcracker = owner;
        BuildWorldBar();
        modelOutline = new NutcrackerModelFireWindowOutline(owner, transform);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        modelOutline?.Dispose();

        if (nutcracker != null && Indicators.TryGetValue(nutcracker, out NutcrackerShotIndicator indicator) && indicator == this)
        {
            Indicators.Remove(nutcracker);
        }
    }

    private void OnDisable()
    {
        SetModelWarningPhase(ModelWarningPhase.None);
    }

    private void LateUpdate()
    {
        if (!NutcrackerShotConfig.IsModEnabled() || nutcracker == null || nutcracker.isEnemyDead)
        {
            Destroy(gameObject);
            return;
        }

        if (IsUiFireWindowEnabled())
        {
            UpdateTransform();
        }

        UpdateState();
    }

    private void BuildWorldBar()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(BarWidth, BarHeight);
        canvasRect.localScale = Vector3.one;

        background = CreateImage("Background", transform, BackgroundColor);
        RectTransform bgRect = background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        fill = CreateImage("Fill", background.transform, AimColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        fill.fillAmount = 0f;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(0.012f, 0.012f);
        fillRect.offsetMax = new Vector2(-0.012f, -0.012f);

        rim = CreateImage("Rim", transform, new Color(1f, 1f, 1f, 0.26f));
        RectTransform rimRect = rim.rectTransform;
        rimRect.anchorMin = Vector2.zero;
        rimRect.anchorMax = Vector2.one;
        rimRect.offsetMin = Vector2.zero;
        rimRect.offsetMax = Vector2.zero;

        criticalPulse = CreateImage("CriticalPulse", transform, new Color(1f, 0f, 0f, 0f));
        RectTransform pulseRect = criticalPulse.rectTransform;
        pulseRect.anchorMin = Vector2.zero;
        pulseRect.anchorMax = Vector2.one;
        pulseRect.offsetMin = new Vector2(-0.018f, -0.018f);
        pulseRect.offsetMax = new Vector2(0.018f, 0.018f);

        countdownText = CreateText("Countdown", transform);
        RectTransform textRect = countdownText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.sizeDelta = new Vector2(0.58f, 0.22f);
        textRect.anchoredPosition = new Vector2(0f, 0.08f);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, worldPositionStays: false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, worldPositionStays: false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0.95f);
        text.fontSize = 42;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = 42;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateTransform()
    {
        Transform ownerTransform = nutcracker.transform;
        transform.position = ownerTransform.position + ownerTransform.right * SideOffset + Vector3.up * HeightOffset;

        Camera camera = GetActiveCamera();
        if (camera != null)
        {
            Vector3 toCamera = transform.position - camera.transform.position;
            if (toCamera.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }
    }

    private void UpdateState()
    {
        bool uiEnabled = IsUiFireWindowEnabled();
        if (!uiEnabled)
        {
            HideUiElements();
        }

        switch (state)
        {
            case WarningState.Aiming:
                UpdateAimingState();
                break;
            case WarningState.Fired:
                UpdateTimedState(FiredColor, 1f, hideWhenFinished: true);
                break;
            case WarningState.Reloading:
                UpdateTimedState(ReloadColor, reverse: true, hideWhenFinished: true);
                break;
            default:
                if (uiEnabled)
                {
                    UpdatePreAim();
                }
                else
                {
                    lastPreAimAmount = 0f;
                    UpdateModelWarningPhase(fireWindowActive: false);
                }
                break;
        }
    }

    private void UpdateTimedState(Color color, float alpha, bool hideWhenFinished)
    {
        UpdateTimedState(color.WithAlpha(alpha), reverse: false, hideWhenFinished: hideWhenFinished);
    }

    private void UpdateTimedState(Color color, bool reverse, bool hideWhenFinished)
    {
        float elapsed = Time.time - stateStartedAt;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, stateDuration));

        if (IsUiFireWindowEnabled())
        {
            fill.color = color;
            fill.fillAmount = reverse ? 1f - progress : progress;
            SetCountdownText(null, Color.white);
            SetCriticalPulse(0f);
        }
        else
        {
            HideUiElements();
        }

        UpdateModelWarningPhase(fireWindowActive: false);

        if (progress >= 1f)
        {
            state = WarningState.Idle;
            if (hideWhenFinished)
            {
                SetVisible(false);
            }
        }
    }

    private void UpdateAimingState()
    {
        float elapsed = Time.time - stateStartedAt;
        float duration = Mathf.Max(0.01f, stateDuration);
        float progress = Mathf.Clamp01(elapsed / duration);
        float remaining = Mathf.Max(0f, duration - elapsed);
        float fireWindowSeconds = GetFireWindowSeconds();
        float criticalAmount = 1f - Mathf.Clamp01(remaining / fireWindowSeconds);
        bool uiEnabled = IsUiFireWindowEnabled();

        if (uiEnabled)
        {
            fill.fillAmount = progress;
            fill.color = Color.Lerp(AimColor, FiredColor, criticalAmount * 0.45f);
        }
        else
        {
            HideUiElements();
        }

        if (remaining <= fireWindowSeconds)
        {
            UpdateModelWarningPhase(fireWindowActive: true);

            if (uiEnabled)
            {
                float pulse = 0.45f + Mathf.PingPong(Time.time * 12f, 0.55f);
                SetCriticalPulse(pulse);
                SetCountdownText("FIRE", Color.Lerp(AimColor, FiredColor, pulse));
            }
        }
        else
        {
            UpdateModelWarningPhase(fireWindowActive: false);

            if (uiEnabled)
            {
                SetCriticalPulse(0f);
                SetRemainingCountdown(remaining);
            }
        }

        if (progress >= 1f)
        {
            state = WarningState.Idle;
            UpdateModelWarningPhase(fireWindowActive: false);

            if (uiEnabled)
            {
                fill.fillAmount = 1f;
                SetCountdownText("NOW", FiredColor);
                SetCriticalPulse(0.85f);
            }
            else
            {
                SetCountdownText(null, Color.white);
                SetCriticalPulse(0f);
            }
        }
    }

    private void UpdatePreAim()
    {
        if (Time.time >= nextPreAimCheckTime)
        {
            cachedPreAimDanger = GetPreAimDanger();
            nextPreAimCheckTime = Time.time + PreAimCheckInterval;
        }

        float danger = cachedPreAimDanger;
        lastPreAimAmount = Mathf.MoveTowards(lastPreAimAmount, danger, Time.deltaTime * 4f);

        if (lastPreAimAmount <= 0.01f)
        {
            SetVisible(false);
            fill.fillAmount = 0f;
            SetCountdownText(null, Color.white);
            SetCriticalPulse(0f);
            UpdateModelWarningPhase(fireWindowActive: false);
            return;
        }

        SetVisible(true);
        fill.color = PreAimColor;
        fill.fillAmount = Mathf.Clamp01(lastPreAimAmount);
        SetCountdownText("!", PreAimColor.WithAlpha(0.92f));
        SetCriticalPulse(0f);
        UpdateModelWarningPhase(fireWindowActive: false);
    }

    private float GetPreAimDanger()
    {
        if (nutcracker.currentBehaviourStateIndex != 2 || nutcracker.gun == null || nutcracker.gun.shellsLoaded <= 0)
        {
            return 0f;
        }

        PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;
        if (localPlayer == null || localPlayer.isPlayerDead || localPlayer.gameplayCamera == null)
        {
            return 0f;
        }

        Vector3 playerCameraPosition = localPlayer.gameplayCamera.transform.position;
        Vector3 shotgunPosition = nutcracker.gun.shotgunRayPoint.position;
        Vector3 toPlayer = playerCameraPosition - shotgunPosition;
        float maxDistance = GetPreAimMaxDistance();
        if (maxDistance <= 0f || toPlayer.sqrMagnitude > maxDistance * maxDistance)
        {
            return 0f;
        }

        float distance = toPlayer.magnitude;
        if (distance <= 0.001f)
        {
            return 0f;
        }

        Vector3 toPlayerDirection = toPlayer / distance;
        float aimDot = Vector3.Dot(nutcracker.gun.shotgunRayPoint.forward, toPlayerDirection);
        if (aimDot < PreAimMinimumDot)
        {
            return 0f;
        }

        if (Physics.Linecast(shotgunPosition, playerCameraPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            return 0f;
        }

        float distanceFactor = 1f - Mathf.Clamp01(distance / maxDistance);
        float angleFactor = Mathf.InverseLerp(PreAimMinimumDot, 1f, aimDot);
        return Mathf.Clamp01(0.2f + distanceFactor * 0.35f + angleFactor * 0.45f);
    }

    private static Camera GetActiveCamera()
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;
        if (localPlayer != null && localPlayer.gameplayCamera != null)
        {
            return localPlayer.gameplayCamera;
        }

        return Camera.main;
    }

    private void SetVisible(bool visible)
    {
        bool effectiveVisible = visible && IsUiFireWindowEnabled();
        if (canvas != null && canvas.enabled != effectiveVisible)
        {
            canvas.enabled = effectiveVisible;
        }
    }

    private void HideUiElements()
    {
        SetVisible(false);

        if (fill != null)
        {
            fill.fillAmount = 0f;
        }

        SetCountdownText(null, Color.white);
        SetCriticalPulse(0f);
    }

    private void SetCountdownText(string text, Color color)
    {
        if (countdownText == null)
        {
            return;
        }

        bool textEnabled = !string.IsNullOrEmpty(text);
        string nextText = text ?? string.Empty;

        if (countdownText.enabled != textEnabled)
        {
            countdownText.enabled = textEnabled;
        }

        if (countdownText.text != nextText)
        {
            countdownText.text = nextText;
        }

        countdownText.color = color;
    }

    private void SetRemainingCountdown(float remaining)
    {
        int tenths = Mathf.Clamp(Mathf.RoundToInt(remaining * 10f), 0, CountdownLabels.Length - 1);
        SetCountdownText(CountdownLabels[tenths], AimColor.WithAlpha(0.96f));
    }

    private static string[] CreateCountdownLabels()
    {
        string[] labels = new string[21];
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = (i * 0.1f).ToString("0.0");
        }

        return labels;
    }

    private void SetCriticalPulse(float alpha)
    {
        if (criticalPulse == null)
        {
            return;
        }

        Color color = criticalPulse.color;
        color.a = Mathf.Clamp01(alpha) * 0.55f;
        criticalPulse.color = color;
    }

    private void SetModelWarningPhase(ModelWarningPhase phase)
    {
        modelOutline?.Update(phase);
    }

    private void UpdateModelWarningPhase(bool fireWindowActive)
    {
        bool modelEnabled = IsAnyModelWarningEnabled();
        bool stateTintEnabled = IsModelStateTintEnabled();
        bool showByDistanceAndCamera = modelEnabled && ShouldShowModelWarningByDistanceAndCamera();
        bool fireWindowVisible = fireWindowActive && showByDistanceAndCamera && (IsModelFireWindowEnabled() || stateTintEnabled);
        bool chaseVisible = showByDistanceAndCamera && stateTintEnabled && IsChaseTintCandidate(nutcracker, lastCombatState);

        SetModelWarningPhase(NutcrackerModelWarningPhaseSelector.Select(modelEnabled, stateTintEnabled, fireWindowVisible, chaseVisible));
    }

    private bool ShouldShowModelWarningByDistanceAndCamera()
    {
        if (!IsAnyModelWarningEnabled())
        {
            return false;
        }

        PlayerControllerB localPlayer = GameNetworkManager.Instance?.localPlayerController;
        Camera camera = localPlayer != null && localPlayer.gameplayCamera != null ? localPlayer.gameplayCamera : Camera.main;

        if (localPlayer != null && localPlayer.isPlayerDead)
        {
            return false;
        }

        float maxDistance = GetModelWarningMaxDistance();
        if (maxDistance > 0f)
        {
            Vector3 playerPosition = camera != null ? camera.transform.position : (localPlayer != null ? localPlayer.transform.position : Vector3.zero);
            if (playerPosition != Vector3.zero && Vector3.Distance(playerPosition, nutcracker.transform.position) > maxDistance)
            {
                return false;
            }
        }

        if (NutcrackerShotConfig.ModelWarningRequireCameraVisible != null && NutcrackerShotConfig.ModelWarningRequireCameraVisible.Value)
        {
            if (camera == null)
            {
                return false;
            }

            Vector3 viewportPoint = camera.WorldToViewportPoint(nutcracker.transform.position + Vector3.up * 1.4f);
            if (viewportPoint.z <= 0f || viewportPoint.x < -0.1f || viewportPoint.x > 1.1f || viewportPoint.y < -0.1f || viewportPoint.y > 1.1f)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsChaseTintCandidate(NutcrackerEnemyAI nutcracker, NutcrackerCombatState combatState)
    {
        if (nutcracker == null || nutcracker.isEnemyDead || nutcracker.currentBehaviourStateIndex != 2)
        {
            return false;
        }

        if (combatState.ReloadingGun || nutcracker.gun == null || nutcracker.gun.shellsLoaded <= 0)
        {
            return false;
        }

        if (combatState.AimingGun)
        {
            return true;
        }

        return nutcracker.lastPlayerSeenMoving != -1
            && nutcracker.targetPlayer != null
            && nutcracker.movingTowardsTargetPlayer;
    }

    private static bool IsUiFireWindowEnabled()
    {
        return NutcrackerShotConfig.EnableUiFireWindow == null || NutcrackerShotConfig.EnableUiFireWindow.Value;
    }

    private static bool IsModelFireWindowEnabled()
    {
        return NutcrackerShotConfig.EnableModelOutlineFireWindow == null || NutcrackerShotConfig.EnableModelOutlineFireWindow.Value;
    }

    private static bool IsModelStateTintEnabled()
    {
        return NutcrackerShotConfig.EnableModelStateTint != null && NutcrackerShotConfig.EnableModelStateTint.Value;
    }

    private static bool IsAnyModelWarningEnabled()
    {
        return IsModelFireWindowEnabled() || IsModelStateTintEnabled();
    }

    private static float GetFireWindowSeconds()
    {
        return NutcrackerShotConfig.FireWindowSeconds == null
            ? DefaultCriticalAimWindow
            : Mathf.Clamp(NutcrackerShotConfig.FireWindowSeconds.Value, 0.05f, 2f);
    }

    private static float GetPreAimMaxDistance()
    {
        if (NutcrackerShotConfig.PreAimMaxDistance == null)
        {
            return PreAimMaxDistance;
        }

        float configuredDistance = NutcrackerShotConfig.PreAimMaxDistance.Value;
        return configuredDistance <= 0f ? 0f : Mathf.Clamp(configuredDistance, 1f, 100f);
    }

    private static float GetModelWarningMaxDistance()
    {
        return NutcrackerShotConfig.ModelWarningMaxDistance == null
            ? 45f
            : NutcrackerShotConfig.ModelWarningMaxDistance.Value;
    }

    private static bool IsDebugLoggingEnabled()
    {
        return NutcrackerShotConfig.EnableDebugLogs != null && NutcrackerShotConfig.EnableDebugLogs.Value;
    }

    private enum WarningState
    {
        Idle,
        Aiming,
        Fired,
        Reloading
    }
}

internal static class ColorExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
