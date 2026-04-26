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

    private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.55f);
    private static readonly Color PreAimColor = new Color(1f, 0.72f, 0.12f, 0.55f);
    private static readonly Color AimColor = new Color(1f, 0.12f, 0.06f, 0.92f);
    private static readonly Color FiredColor = new Color(1f, 1f, 1f, 0.96f);
    private static readonly Color ReloadColor = new Color(0.2f, 0.65f, 1f, 0.55f);

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
    private bool observedAimingLastFrame;
    private bool observedReloadingLastFrame;

    public static NutcrackerShotIndicator For(NutcrackerEnemyAI nutcracker)
    {
        if (nutcracker == null)
        {
            return null;
        }

        NutcrackerShotIndicator indicator = nutcracker.GetComponentInChildren<NutcrackerShotIndicator>(true);
        if (indicator != null)
        {
            return indicator;
        }

        GameObject holder = new GameObject("NutcrackerShotUI");
        holder.transform.SetParent(nutcracker.transform, worldPositionStays: false);
        indicator = holder.AddComponent<NutcrackerShotIndicator>();
        indicator.Initialize(nutcracker);
        return indicator;
    }

    public static void Remove(NutcrackerEnemyAI nutcracker)
    {
        if (nutcracker == null)
        {
            return;
        }

        NutcrackerShotIndicator indicator = nutcracker.GetComponentInChildren<NutcrackerShotIndicator>(true);
        if (indicator != null)
        {
            Destroy(indicator.gameObject);
        }
    }

    public void BeginAim(float duration)
    {
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
        state = WarningState.Reloading;
        stateStartedAt = Time.time;
        stateDuration = duration;
        SetVisible(true);
    }

    public void ObserveCombatState(bool aimingGun, bool reloadingGun, float timeSinceFiringGun, float aimDuration)
    {
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
            SetModelFireWindow(false);
        }

        if (reloadingGun && !observedReloadingLastFrame && state != WarningState.Aiming)
        {
            BeginReload(1.74f);
        }

        observedAimingLastFrame = aimingGun;
        observedReloadingLastFrame = reloadingGun;
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
    }

    private void OnDisable()
    {
        SetModelFireWindow(false);
    }

    private void LateUpdate()
    {
        if (nutcracker == null || nutcracker.isEnemyDead)
        {
            Destroy(gameObject);
            return;
        }

        UpdateTransform();
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
                UpdatePreAim();
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
        fill.color = color;
        fill.fillAmount = reverse ? 1f - progress : progress;
        SetCountdownText(null, Color.white);
        SetCriticalPulse(0f);
        SetModelFireWindow(false);

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

        fill.fillAmount = progress;
        fill.color = Color.Lerp(AimColor, FiredColor, criticalAmount * 0.45f);

        if (remaining <= fireWindowSeconds)
        {
            SetModelFireWindow(ShouldShowModelFireWindow());

            if (IsUiFireWindowEnabled())
            {
                float pulse = 0.45f + Mathf.PingPong(Time.time * 12f, 0.55f);
                SetCriticalPulse(pulse);
                SetCountdownText("FIRE", Color.Lerp(AimColor, FiredColor, pulse));
            }
            else
            {
                SetCriticalPulse(0f);
                SetCountdownText(remaining.ToString("0.0"), AimColor.WithAlpha(0.96f));
            }
        }
        else
        {
            SetCriticalPulse(0f);
            SetModelFireWindow(false);
            SetCountdownText(remaining.ToString("0.0"), AimColor.WithAlpha(0.96f));
        }

        if (progress >= 1f)
        {
            state = WarningState.Idle;
            fill.fillAmount = 1f;
            SetModelFireWindow(false);

            if (IsUiFireWindowEnabled())
            {
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
        float danger = GetPreAimDanger();
        lastPreAimAmount = Mathf.MoveTowards(lastPreAimAmount, danger, Time.deltaTime * 4f);

        if (lastPreAimAmount <= 0.01f)
        {
            SetVisible(false);
            fill.fillAmount = 0f;
            SetCountdownText(null, Color.white);
            SetCriticalPulse(0f);
            SetModelFireWindow(false);
            return;
        }

        SetVisible(true);
        fill.color = PreAimColor;
        fill.fillAmount = Mathf.Clamp01(lastPreAimAmount);
        SetCountdownText("!", PreAimColor.WithAlpha(0.92f));
        SetCriticalPulse(0f);
        SetModelFireWindow(false);
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
        float distance = toPlayer.magnitude;
        float maxDistance = GetPreAimMaxDistance();
        if (distance > maxDistance)
        {
            return 0f;
        }

        if (Vector3.Angle(nutcracker.gun.shotgunRayPoint.forward, toPlayer) > 35f)
        {
            return 0f;
        }

        if (Physics.Linecast(shotgunPosition, playerCameraPosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            return 0f;
        }

        float distanceFactor = 1f - Mathf.Clamp01(distance / maxDistance);
        float angleFactor = 1f - Mathf.Clamp01(Vector3.Angle(nutcracker.gun.shotgunRayPoint.forward, toPlayer) / 35f);
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
        if (canvas != null && canvas.enabled != visible)
        {
            canvas.enabled = visible;
        }
    }

    private void SetCountdownText(string text, Color color)
    {
        if (countdownText == null)
        {
            return;
        }

        countdownText.enabled = !string.IsNullOrEmpty(text);
        countdownText.text = text ?? string.Empty;
        countdownText.color = color;
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

    private void SetModelFireWindow(bool active)
    {
        modelOutline?.Update(active);
    }

    private bool ShouldShowModelFireWindow()
    {
        if (!IsModelFireWindowEnabled())
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

    private static bool IsUiFireWindowEnabled()
    {
        return NutcrackerShotConfig.EnableUiFireWindow == null || NutcrackerShotConfig.EnableUiFireWindow.Value;
    }

    private static bool IsModelFireWindowEnabled()
    {
        return NutcrackerShotConfig.EnableModelOutlineFireWindow == null || NutcrackerShotConfig.EnableModelOutlineFireWindow.Value;
    }

    private static float GetFireWindowSeconds()
    {
        return NutcrackerShotConfig.FireWindowSeconds == null
            ? DefaultCriticalAimWindow
            : Mathf.Clamp(NutcrackerShotConfig.FireWindowSeconds.Value, 0.05f, 2f);
    }

    private static float GetPreAimMaxDistance()
    {
        return NutcrackerShotConfig.PreAimMaxDistance == null
            ? PreAimMaxDistance
            : Mathf.Clamp(NutcrackerShotConfig.PreAimMaxDistance.Value, 1f, 100f);
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
