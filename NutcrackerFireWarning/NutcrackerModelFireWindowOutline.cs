using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace NutcrackerShotUI;

internal sealed class NutcrackerModelFireWindowOutline
{
    private const float ScreenPadding = 18f;
    private const float MinScreenWidth = 72f;
    private const float MinScreenHeight = 120f;

    private readonly NutcrackerEnemyAI nutcracker;
    private readonly Material meshMaterial;
    private readonly List<MeshOutlineClone> meshClones = new List<MeshOutlineClone>();
    private readonly List<SourceRendererPulse> sourcePulses = new List<SourceRendererPulse>();

    private GameObject screenRoot;
    private Canvas screenCanvas;
    private RectTransform screenCanvasRect;
    private RectTransform screenBoxRect;
    private Image screenTop;
    private Image screenBottom;
    private Image screenLeft;
    private Image screenRight;

    private Renderer[] cachedRenderers;
    private float nextRendererRefreshTime;
    private bool meshBuilt;
    private bool loggedMeshVisible;
    private bool loggedScreenVisible;

    public NutcrackerModelFireWindowOutline(NutcrackerEnemyAI nutcracker, Transform parent)
    {
        this.nutcracker = nutcracker;
        meshMaterial = CreateMeshMaterial();
    }

    public void Dispose()
    {
        ClearMeshClones();

        if (meshMaterial != null)
        {
            Object.Destroy(meshMaterial);
        }

        if (screenRoot != null)
        {
            Object.Destroy(screenRoot);
        }
    }

    public void Update(bool active)
    {
        if (!active || nutcracker == null)
        {
            loggedMeshVisible = false;
            loggedScreenVisible = false;
            SetMeshVisible(false);
            SetScreenVisible(false);
            return;
        }

        if (GetMode() == ModelOutlineMode.ScreenBox)
        {
            SetMeshVisible(false);
            UpdateScreenBox();
        }
        else
        {
            SetScreenVisible(false);
            UpdateMeshSilhouette();
        }
    }

    private void UpdateMeshSilhouette()
    {
        EnsureMeshClones();

        if (meshClones.Count == 0 && sourcePulses.Count == 0)
        {
            SetMeshVisible(false);
            return;
        }

        float pulseAlpha = GetPulseAlpha();
        float pulseIntensity = GetPulseIntensity();
        Color color = Color.Lerp(new Color(1f, 0.02f, 0.02f, pulseAlpha), new Color(1f, 1f, 1f, pulseAlpha), Mathf.PingPong(Time.time * 10f, 1f));
        SetMaterialColor(meshMaterial, color, pulseIntensity);

        float outlineWidth = Mathf.Max(0.01f, NutcrackerShotConfig.ModelOutlineWidth.Value);
        float outlineScale = Mathf.Max(1f, NutcrackerShotConfig.MeshOutlineScale.Value);
        int visibleCount = 0;
        int pulseCount = 0;

        for (int i = 0; i < sourcePulses.Count; i++)
        {
            if (sourcePulses[i].Apply(color, pulseIntensity))
            {
                pulseCount++;
            }
        }

        for (int i = 0; i < meshClones.Count; i++)
        {
            if (meshClones[i].Update(color, outlineWidth, outlineScale))
            {
                visibleCount++;
            }
        }

        if (visibleCount == 0 && pulseCount == 0)
        {
            SetMeshVisible(false);
            return;
        }

        if (IsDebugLoggingEnabled() && !loggedMeshVisible)
        {
            loggedMeshVisible = true;
            string shaderName = meshClones.Count > 0 ? meshClones[0].GetShaderSummary() : "none";
            Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} mesh silhouette outline visible. CloneRenderers={visibleCount} SourcePulses={pulseCount} Width={outlineWidth:0.000} Scale={outlineScale:0.000} Shader={shaderName}");
        }
    }

    private void EnsureMeshClones()
    {
        if (meshBuilt)
        {
            return;
        }

        meshBuilt = true;
        RefreshRenderersIfNeeded(force: true);

        if (cachedRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (!IsModelOutlineRenderer(renderer))
            {
                continue;
            }

            if (ShouldUseCloneShell())
            {
                MeshOutlineClone clone = MeshOutlineClone.TryCreate(renderer, meshMaterial);
                if (clone != null)
                {
                    meshClones.Add(clone);
                }
            }

            if (ShouldUseSourcePulse())
            {
                SourceRendererPulse pulse = SourceRendererPulse.TryCreate(renderer);
                if (pulse != null)
                {
                    sourcePulses.Add(pulse);
                }
            }
        }

        if (IsDebugLoggingEnabled())
        {
            Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} built mesh silhouette outline clones: {meshClones.Count}, source pulses: {sourcePulses.Count}");
        }

        if (ShouldDumpModelAudit())
        {
            DumpModelAudit();
        }
    }

    private void DumpModelAudit()
    {
        if (cachedRenderers == null)
        {
            return;
        }

        Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} model audit: renderers={cachedRenderers.Length}, outlineClones={meshClones.Count}");

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null || renderer.GetComponentInParent<NutcrackerShotIndicator>() != null)
            {
                continue;
            }

            string meshName = "none";
            int vertexCount = 0;
            int subMeshCount = 0;

            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null && skinned.sharedMesh != null)
            {
                meshName = skinned.sharedMesh.name;
                vertexCount = skinned.sharedMesh.vertexCount;
                subMeshCount = skinned.sharedMesh.subMeshCount;
            }
            else
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    meshName = filter.sharedMesh.name;
                    vertexCount = filter.sharedMesh.vertexCount;
                    subMeshCount = filter.sharedMesh.subMeshCount;
                }
            }

            Plugin.Log.LogInfo($"Nutcracker model audit renderer[{i}]: type={renderer.GetType().Name}, eligible={IsModelOutlineRenderer(renderer)}, path={GetPath(renderer.transform)}, mesh={meshName}, vertices={vertexCount}, subMeshes={subMeshCount}, materials={renderer.sharedMaterials.Length}");
        }
    }

    private void ClearMeshClones()
    {
        for (int i = 0; i < meshClones.Count; i++)
        {
            meshClones[i].Dispose();
        }

        meshClones.Clear();

        for (int i = 0; i < sourcePulses.Count; i++)
        {
            sourcePulses[i].Dispose();
        }

        sourcePulses.Clear();
        meshBuilt = false;
    }

    private void SetMeshVisible(bool visible)
    {
        for (int i = 0; i < meshClones.Count; i++)
        {
            meshClones[i].SetVisible(visible);
        }

        if (!visible)
        {
            for (int i = 0; i < sourcePulses.Count; i++)
            {
                sourcePulses[i].Restore();
            }
        }
    }

    private void UpdateScreenBox()
    {
        EnsureScreenBox();

        if (!TryGetBounds(out Bounds bounds))
        {
            loggedScreenVisible = false;
            SetScreenVisible(false);
            return;
        }

        Camera camera = GetActiveCamera();
        if (camera == null || !TryProjectBounds(bounds, camera, out Rect screenRect))
        {
            loggedScreenVisible = false;
            SetScreenVisible(false);
            return;
        }

        screenCanvasRect.sizeDelta = new Vector2(Screen.width, Screen.height);

        float width = Mathf.Max(MinScreenWidth, screenRect.width + ScreenPadding * 2f);
        float height = Mathf.Max(MinScreenHeight, screenRect.height + ScreenPadding * 2f);
        Vector2 center = screenRect.center;
        screenBoxRect.anchoredPosition = new Vector2(center.x - Screen.width * 0.5f, center.y - Screen.height * 0.5f);
        screenBoxRect.sizeDelta = new Vector2(width, height);

        float lineWidth = Mathf.Max(3f, NutcrackerShotConfig.ModelOutlineWidth.Value * 120f);
        SetStripThickness(screenTop.rectTransform, lineWidth, horizontal: true);
        SetStripThickness(screenBottom.rectTransform, lineWidth, horizontal: true);
        SetStripThickness(screenLeft.rectTransform, lineWidth, horizontal: false);
        SetStripThickness(screenRight.rectTransform, lineWidth, horizontal: false);

        Color color = Color.Lerp(new Color(1f, 0.02f, 0.02f, 0.96f), Color.white, Mathf.PingPong(Time.unscaledTime * 10f, 1f));
        screenTop.color = color;
        screenBottom.color = color;
        screenLeft.color = color;
        screenRight.color = color;

        SetScreenVisible(true);

        if (IsDebugLoggingEnabled() && !loggedScreenVisible)
        {
            loggedScreenVisible = true;
            Plugin.Log.LogInfo($"Nutcracker #{nutcracker.thisEnemyIndex} screen outline visible. Rect={width:0}x{height:0} Center={center.x:0},{center.y:0}");
        }
    }

    private void EnsureScreenBox()
    {
        if (screenRoot != null)
        {
            return;
        }

        screenRoot = new GameObject("NutcrackerFireWindowScreenOutline");
        screenRoot.hideFlags = HideFlags.HideAndDontSave;

        screenCanvas = screenRoot.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 2500;
        screenCanvasRect = screenCanvas.GetComponent<RectTransform>();

        GameObject boxObject = new GameObject("ProjectedModelBox");
        boxObject.transform.SetParent(screenRoot.transform, worldPositionStays: false);
        screenBoxRect = boxObject.AddComponent<RectTransform>();
        screenBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        screenBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        screenBoxRect.pivot = new Vector2(0.5f, 0.5f);

        screenTop = CreateStrip("Top", screenBoxRect);
        screenBottom = CreateStrip("Bottom", screenBoxRect);
        screenLeft = CreateStrip("Left", screenBoxRect);
        screenRight = CreateStrip("Right", screenBoxRect);

        AnchorHorizontal(screenTop.rectTransform, 1f);
        AnchorHorizontal(screenBottom.rectTransform, 0f);
        AnchorVertical(screenLeft.rectTransform, 0f);
        AnchorVertical(screenRight.rectTransform, 1f);
        SetScreenVisible(false);
    }

    private static Material CreateMeshMaterial()
    {
        Shader shader = Shader.Find("HDRP/Unlit") ?? Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
        Material material = new Material(shader);
        material.name = "NutcrackerMeshSilhouetteOutlineMaterial";
        material.hideFlags = HideFlags.HideAndDontSave;
        ConfigureOutlineMaterial(material);
        SetMaterialColor(material, new Color(1f, 0.02f, 0.02f, 0.86f));
        return material;
    }

    private static void ConfigureOutlineMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        material.renderQueue = 5500;
        material.SetOverrideTag("RenderType", "Transparent");

        TrySetInt(material, "_Cull", (int)CullMode.Off);
        TrySetInt(material, "_CullMode", (int)CullMode.Off);
        TrySetInt(material, "_CullModeForward", (int)CullMode.Off);
        TrySetInt(material, "_SrcBlend", (int)BlendMode.SrcAlpha);
        TrySetInt(material, "_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        TrySetInt(material, "_AlphaSrcBlend", (int)BlendMode.SrcAlpha);
        TrySetInt(material, "_AlphaDstBlend", (int)BlendMode.OneMinusSrcAlpha);
        TrySetInt(material, "_ZWrite", 0);
        TrySetInt(material, "_ZTest", (int)CompareFunction.Always);
        TrySetInt(material, "_ZTestTransparent", (int)CompareFunction.Always);
        TrySetInt(material, "_ZTestDepthEqualForOpaque", (int)CompareFunction.Always);
        TrySetFloat(material, "_SurfaceType", 1f);
        TrySetFloat(material, "_BlendMode", 0f);
        TrySetFloat(material, "_AlphaCutoffEnable", 0f);
        TrySetFloat(material, "_EnableFogOnTransparent", 0f);
        TrySetFloat(material, "_UseEmissiveIntensity", 1f);
        TrySetFloat(material, "_EmissiveIntensity", 2.5f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ALPHA");
    }

    private static void SetMaterialColor(Material material, Color color, float emissionMultiplier = 3f)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_UnlitColor"))
        {
            material.SetColor("_UnlitColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        Color emissionColor = new Color(color.r * emissionMultiplier, color.g * emissionMultiplier, color.b * emissionMultiplier, color.a);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emissionColor);
        }

        if (material.HasProperty("_EmissiveColor"))
        {
            material.SetColor("_EmissiveColor", emissionColor);
        }

        if (material.HasProperty("_EmissiveColorLDR"))
        {
            material.SetColor("_EmissiveColorLDR", emissionColor);
        }
    }

    private static void TrySetInt(Material material, string property, int value)
    {
        if (material != null && material.HasProperty(property))
        {
            material.SetInt(property, value);
        }
    }

    private static void TrySetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static Image CreateStrip(string name, Transform parent)
    {
        GameObject stripObject = new GameObject(name);
        stripObject.transform.SetParent(parent, worldPositionStays: false);
        Image image = stripObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }

    private static void AnchorHorizontal(RectTransform strip, float y)
    {
        strip.anchorMin = new Vector2(0f, y);
        strip.anchorMax = new Vector2(1f, y);
        strip.pivot = new Vector2(0.5f, 0.5f);
        strip.anchoredPosition = Vector2.zero;
    }

    private static void AnchorVertical(RectTransform strip, float x)
    {
        strip.anchorMin = new Vector2(x, 0f);
        strip.anchorMax = new Vector2(x, 1f);
        strip.pivot = new Vector2(0.5f, 0.5f);
        strip.anchoredPosition = Vector2.zero;
    }

    private static void SetStripThickness(RectTransform strip, float thickness, bool horizontal)
    {
        strip.sizeDelta = horizontal ? new Vector2(0f, thickness) : new Vector2(thickness, 0f);
    }

    private static bool TryProjectBounds(Bounds bounds, Camera camera, out Rect screenRect)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        bool anyVisible = false;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);
            if (screen.z <= 0.01f)
            {
                continue;
            }

            anyVisible = true;
            minX = Mathf.Min(minX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxX = Mathf.Max(maxX, screen.x);
            maxY = Mathf.Max(maxY, screen.y);
        }

        if (!anyVisible)
        {
            screenRect = default;
            return false;
        }

        minX = Mathf.Clamp(minX, 0f, Screen.width);
        maxX = Mathf.Clamp(maxX, 0f, Screen.width);
        minY = Mathf.Clamp(minY, 0f, Screen.height);
        maxY = Mathf.Clamp(maxY, 0f, Screen.height);

        if (maxX <= minX || maxY <= minY)
        {
            screenRect = default;
            return false;
        }

        screenRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        RefreshRenderersIfNeeded(force: false);
        bounds = default;
        bool hasBounds = false;

        if (cachedRenderers == null)
        {
            return false;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (!IsModelOutlineRenderer(renderer) || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void RefreshRenderersIfNeeded(bool force)
    {
        if (!force && cachedRenderers != null && Time.time < nextRendererRefreshTime)
        {
            return;
        }

        cachedRenderers = nutcracker.GetComponentsInChildren<Renderer>(includeInactive: false);
        nextRendererRefreshTime = Time.time + 1f;
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

    private void SetScreenVisible(bool visible)
    {
        if (screenCanvas != null && screenCanvas.enabled != visible)
        {
            screenCanvas.enabled = visible;
        }
    }

    private static ModelOutlineMode GetMode()
    {
        return NutcrackerShotConfig.ModelOutlineMode == null
            ? ModelOutlineMode.MeshSilhouette
            : NutcrackerShotConfig.ModelOutlineMode.Value;
    }

    private static ModelPulseMode GetPulseMode()
    {
        return NutcrackerShotConfig.ModelPulseMode == null
            ? ModelPulseMode.SourcePulse
            : NutcrackerShotConfig.ModelPulseMode.Value;
    }

    private static bool ShouldUseSourcePulse()
    {
        ModelPulseMode mode = GetPulseMode();
        return mode == ModelPulseMode.SourcePulse || mode == ModelPulseMode.Both;
    }

    private static bool ShouldUseCloneShell()
    {
        ModelPulseMode mode = GetPulseMode();
        return mode == ModelPulseMode.CloneShell || mode == ModelPulseMode.Both;
    }

    private static float GetPulseIntensity()
    {
        return NutcrackerShotConfig.ModelPulseIntensity == null
            ? 4f
            : Mathf.Clamp(NutcrackerShotConfig.ModelPulseIntensity.Value, 0f, 12f);
    }

    private static float GetPulseAlpha()
    {
        return NutcrackerShotConfig.ModelPulseAlpha == null
            ? 0.92f
            : Mathf.Clamp01(NutcrackerShotConfig.ModelPulseAlpha.Value);
    }

    private static bool IsDebugLoggingEnabled()
    {
        return NutcrackerShotConfig.EnableDebugLogs != null && NutcrackerShotConfig.EnableDebugLogs.Value;
    }

    private static bool ShouldDumpModelAudit()
    {
        return NutcrackerShotConfig.DumpModelAudit != null && NutcrackerShotConfig.DumpModelAudit.Value;
    }

    private static bool IsModelOutlineRenderer(Renderer renderer)
    {
        if (renderer == null || renderer is LineRenderer || renderer.GetType().Name == "ParticleSystemRenderer")
        {
            return false;
        }

        if (renderer.GetComponentInParent<NutcrackerShotIndicator>() != null)
        {
            return false;
        }

        string path = GetPath(renderer.transform);
        if (ContainsAny(path, "MapDot", "ScanNode", "BloodSpurtParticle", "Collider"))
        {
            return false;
        }

        SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
        if (skinned != null)
        {
            string meshName = skinned.sharedMesh == null ? string.Empty : skinned.sharedMesh.name;
            return ContainsAny(path, "LOD0") || ContainsAny(meshName, "LOD0");
        }

        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            return false;
        }

        string staticMeshName = filter.sharedMesh.name;
        return ContainsAny(path, "Gun", "Shotgun") || ContainsAny(staticMeshName, "Gun", "Shotgun");
    }

    private static bool ContainsAny(string value, params string[] fragments)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (int i = 0; i < fragments.Length; i++)
        {
            if (value.IndexOf(fragments[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private sealed class MeshOutlineClone
    {
        private readonly Renderer source;
        private readonly GameObject cloneObject;
        private readonly MeshFilter cloneFilter;
        private readonly MeshRenderer cloneMeshRenderer;
        private readonly SkinnedMeshRenderer cloneSkinnedRenderer;
        private readonly SkinnedMeshRenderer skinnedSource;
        private readonly Mesh sourceSkinnedMesh;
        private readonly Mesh sourceStaticMesh;
        private readonly Mesh expandedMesh;
        private readonly Material[] outlineMaterials;
        private readonly Vector3 sourceLocalScale;
        private readonly bool canExpandMesh;
        private float lastOutlineWidth = -1f;

        private MeshOutlineClone(Renderer source, GameObject cloneObject, MeshFilter cloneFilter, MeshRenderer cloneMeshRenderer, SkinnedMeshRenderer cloneSkinnedRenderer, SkinnedMeshRenderer skinnedSource, Mesh sourceSkinnedMesh, Mesh sourceStaticMesh, Mesh expandedMesh, Material[] outlineMaterials, bool canExpandMesh)
        {
            this.source = source;
            this.cloneObject = cloneObject;
            this.cloneFilter = cloneFilter;
            this.cloneMeshRenderer = cloneMeshRenderer;
            this.cloneSkinnedRenderer = cloneSkinnedRenderer;
            this.skinnedSource = skinnedSource;
            this.sourceSkinnedMesh = sourceSkinnedMesh;
            this.sourceStaticMesh = sourceStaticMesh;
            this.expandedMesh = expandedMesh;
            this.outlineMaterials = outlineMaterials;
            this.canExpandMesh = canExpandMesh;
            sourceLocalScale = source.transform.localScale;
        }

        public static MeshOutlineClone TryCreate(Renderer source, Material material)
        {
            if (source == null || material == null)
            {
                return null;
            }

            Mesh mesh = null;
            Mesh expandedMesh = null;
            Mesh sourceStaticMesh = null;
            Mesh sourceSkinnedMesh = null;
            bool canExpandMesh = false;
            SkinnedMeshRenderer skinned = source as SkinnedMeshRenderer;

            if (skinned != null)
            {
                if (skinned.sharedMesh == null)
                {
                    return null;
                }

                sourceSkinnedMesh = skinned.sharedMesh;
                canExpandMesh = sourceSkinnedMesh.isReadable;
                if (canExpandMesh)
                {
                    expandedMesh = Object.Instantiate(sourceSkinnedMesh);
                    expandedMesh.name = $"{source.name}_NutcrackerOutlineExpandedMesh";
                    expandedMesh.MarkDynamic();
                    mesh = expandedMesh;
                }
                else
                {
                    mesh = sourceSkinnedMesh;
                }
            }
            else
            {
                MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                if (sourceFilter == null || sourceFilter.sharedMesh == null)
                {
                    return null;
                }

                sourceStaticMesh = sourceFilter.sharedMesh;
                canExpandMesh = sourceStaticMesh.isReadable;
                if (canExpandMesh)
                {
                    expandedMesh = Object.Instantiate(sourceStaticMesh);
                    expandedMesh.name = $"{source.name}_NutcrackerOutlineExpandedMesh";
                    mesh = expandedMesh;
                }
                else
                {
                    mesh = sourceStaticMesh;
                }
            }

            GameObject cloneObject = new GameObject($"{source.name}_NutcrackerMeshOutline");
            cloneObject.hideFlags = HideFlags.HideAndDontSave;
            cloneObject.layer = source.gameObject.layer;
            cloneObject.transform.SetParent(source.transform.parent, worldPositionStays: false);
            MeshFilter cloneFilter = null;
            MeshRenderer cloneMeshRenderer = null;
            SkinnedMeshRenderer cloneSkinnedRenderer = null;
            Material[] outlineMaterials = CreateOutlineMaterials(source.sharedMaterials, material);

            if (skinned != null)
            {
                cloneSkinnedRenderer = cloneObject.AddComponent<SkinnedMeshRenderer>();
                cloneSkinnedRenderer.sharedMesh = mesh;
                cloneSkinnedRenderer.sharedMaterials = outlineMaterials;
                cloneSkinnedRenderer.bones = skinned.bones;
                cloneSkinnedRenderer.rootBone = skinned.rootBone;
                cloneSkinnedRenderer.quality = skinned.quality;
                cloneSkinnedRenderer.updateWhenOffscreen = true;
                cloneSkinnedRenderer.localBounds = ExpandBounds(skinned.localBounds, 0.25f);
                ConfigureCloneRenderer(cloneSkinnedRenderer);
            }
            else
            {
                cloneFilter = cloneObject.AddComponent<MeshFilter>();
                cloneMeshRenderer = cloneObject.AddComponent<MeshRenderer>();
                cloneFilter.sharedMesh = mesh;
                cloneMeshRenderer.sharedMaterials = outlineMaterials;
                ConfigureCloneRenderer(cloneMeshRenderer);
            }

            cloneObject.SetActive(false);

            MeshOutlineClone clone = new MeshOutlineClone(source, cloneObject, cloneFilter, cloneMeshRenderer, cloneSkinnedRenderer, skinned, sourceSkinnedMesh, sourceStaticMesh, expandedMesh, outlineMaterials, canExpandMesh);
            clone.CopyTransform(1f);
            return clone;
        }

        public bool Update(Color color, float outlineWidth, float scale)
        {
            if (source == null || cloneObject == null || !source.enabled)
            {
                SetVisible(false);
                return false;
            }

            if (skinnedSource != null)
            {
                if (canExpandMesh && expandedMesh != null && !Mathf.Approximately(lastOutlineWidth, outlineWidth))
                {
                    ExpandMeshAlongNormals(sourceSkinnedMesh, expandedMesh, outlineWidth);
                    cloneSkinnedRenderer.sharedMesh = expandedMesh;
                    cloneSkinnedRenderer.localBounds = ExpandBounds(skinnedSource.localBounds, outlineWidth + 0.25f);
                }
            }
            else if (canExpandMesh && expandedMesh != null && !Mathf.Approximately(lastOutlineWidth, outlineWidth))
            {
                ExpandMeshAlongNormals(sourceStaticMesh, expandedMesh, outlineWidth);
                cloneFilter.sharedMesh = expandedMesh;
            }

            lastOutlineWidth = outlineWidth;
            SetMaterialsColor(color);
            CopyTransform(scale);
            SetVisible(true);
            return true;
        }

        public string GetShaderSummary()
        {
            if (outlineMaterials == null || outlineMaterials.Length == 0 || outlineMaterials[0] == null || outlineMaterials[0].shader == null)
            {
                return "none";
            }

            return outlineMaterials[0].shader.name;
        }

        public void SetVisible(bool visible)
        {
            if (cloneObject != null && cloneObject.activeSelf != visible)
            {
                cloneObject.SetActive(visible);
            }
        }

        public void Dispose()
        {
            if (expandedMesh != null)
            {
                Object.Destroy(expandedMesh);
            }

            if (outlineMaterials != null)
            {
                for (int i = 0; i < outlineMaterials.Length; i++)
                {
                    if (outlineMaterials[i] != null)
                    {
                        Object.Destroy(outlineMaterials[i]);
                    }
                }
            }

            if (cloneObject != null)
            {
                Object.Destroy(cloneObject);
            }
        }

        private void CopyTransform(float scale)
        {
            Transform sourceTransform = source.transform;
            Transform cloneTransform = cloneObject.transform;
            cloneTransform.localPosition = sourceTransform.localPosition;
            cloneTransform.localRotation = sourceTransform.localRotation;
            cloneTransform.localScale = sourceLocalScale * scale;
        }

        private void SetMaterialsColor(Color color)
        {
            if (outlineMaterials == null)
            {
                return;
            }

            for (int i = 0; i < outlineMaterials.Length; i++)
            {
                SetMaterialColor(outlineMaterials[i], color);
            }
        }

        private static void ConfigureCloneRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.forceRenderingOff = false;
            renderer.enabled = true;
        }

        private static Bounds ExpandBounds(Bounds bounds, float amount)
        {
            bounds.Expand(amount);
            return bounds;
        }

        private static void ExpandMeshAlongNormals(Mesh sourceMesh, Mesh targetMesh, float outlineWidth)
        {
            if (sourceMesh == null || targetMesh == null)
            {
                return;
            }

            Vector3[] vertices = sourceMesh.vertices;
            Vector3[] normals = sourceMesh.normals;

            if (normals == null || normals.Length != vertices.Length)
            {
                sourceMesh.RecalculateNormals();
                normals = sourceMesh.normals;
            }

            Vector3[] expandedVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 normal = normals.Length > i ? normals[i].normalized : Vector3.zero;
                expandedVertices[i] = vertices[i] + normal * outlineWidth;
            }

            targetMesh.Clear();
            targetMesh.vertices = expandedVertices;
            targetMesh.normals = normals;
            targetMesh.tangents = sourceMesh.tangents;
            targetMesh.uv = sourceMesh.uv;
            targetMesh.uv2 = sourceMesh.uv2;
            targetMesh.colors = sourceMesh.colors;
            targetMesh.subMeshCount = sourceMesh.subMeshCount;

            for (int i = 0; i < sourceMesh.subMeshCount; i++)
            {
                targetMesh.SetTriangles(sourceMesh.GetTriangles(i), i);
            }

            Bounds expandedBounds = sourceMesh.bounds;
            expandedBounds.Expand(outlineWidth * 2f + 0.02f);
            targetMesh.bounds = expandedBounds;
        }

        private static Material[] CreateOutlineMaterials(Material[] sourceMaterials, Material fallbackMaterial)
        {
            int count = sourceMaterials == null ? 0 : sourceMaterials.Length;
            count = Mathf.Max(1, count);
            Material[] materials = new Material[count];
            for (int i = 0; i < materials.Length; i++)
            {
                Material sourceMaterial = sourceMaterials != null && i < sourceMaterials.Length ? sourceMaterials[i] : null;
                Material outlineMaterial = sourceMaterial != null ? Object.Instantiate(sourceMaterial) : Object.Instantiate(fallbackMaterial);
                outlineMaterial.name = $"{(sourceMaterial != null ? sourceMaterial.name : "Fallback")}_NutcrackerOutlineMaterial";
                outlineMaterial.hideFlags = HideFlags.HideAndDontSave;
                ConfigureOutlineMaterial(outlineMaterial);
                SetMaterialColor(outlineMaterial, new Color(1f, 0.02f, 0.02f, 0.86f));
                materials[i] = outlineMaterial;
            }

            return materials;
        }
    }

    private sealed class SourceRendererPulse
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly int EmissiveColorLdrId = Shader.PropertyToID("_EmissiveColorLDR");

        private readonly Renderer renderer;
        private readonly MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock pulseBlock = new MaterialPropertyBlock();
        private bool capturedOriginalBlock;
        private bool hadOriginalBlock;
        private bool active;

        private SourceRendererPulse(Renderer renderer)
        {
            this.renderer = renderer;
        }

        public static SourceRendererPulse TryCreate(Renderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            return new SourceRendererPulse(renderer);
        }

        public bool Apply(Color color, float emissionMultiplier)
        {
            if (renderer == null || !renderer.enabled)
            {
                Restore();
                return false;
            }

            CaptureOriginalBlockIfNeeded();

            renderer.GetPropertyBlock(pulseBlock);
            Color emissionColor = new Color(color.r * emissionMultiplier, color.g * emissionMultiplier, color.b * emissionMultiplier, 1f);
            pulseBlock.SetColor(BaseColorId, color);
            pulseBlock.SetColor(ColorId, color);
            pulseBlock.SetColor(UnlitColorId, color);
            pulseBlock.SetColor(EmissionColorId, emissionColor);
            pulseBlock.SetColor(EmissiveColorId, emissionColor);
            pulseBlock.SetColor(EmissiveColorLdrId, emissionColor);
            renderer.SetPropertyBlock(pulseBlock);
            active = true;
            return true;
        }

        public void Restore()
        {
            if (renderer == null || !active)
            {
                return;
            }

            if (hadOriginalBlock)
            {
                renderer.SetPropertyBlock(originalBlock);
            }
            else
            {
                renderer.SetPropertyBlock(null);
            }

            active = false;
        }

        public void Dispose()
        {
            Restore();
        }

        private void CaptureOriginalBlockIfNeeded()
        {
            if (capturedOriginalBlock)
            {
                return;
            }

            renderer.GetPropertyBlock(originalBlock);
            hadOriginalBlock = !originalBlock.isEmpty;
            capturedOriginalBlock = true;
        }
    }
}
