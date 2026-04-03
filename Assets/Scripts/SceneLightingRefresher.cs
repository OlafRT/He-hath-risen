using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

[DisallowMultipleComponent]
public class SceneLightingRefresher : MonoBehaviour
{
    private static SceneLightingRefresher _instance;

    [SerializeField] private string onlyForScene = "";

    [SerializeField] private bool reapplySkybox = true;
    [SerializeField] private bool updateEnvironmentGI = true;
    [SerializeField] private bool rerenderRealtimeReflectionProbes = true;

    [Header("Clean up carry-over (DDOL scene)")]
    [SerializeField] private bool disableDontDestroyLights = true;
    [SerializeField] private bool disableDontDestroyReflectionProbes = true;
    [SerializeField] private bool disableDontDestroyVolumes = true;

#if UNITY_RENDER_PIPELINE_UNIVERSAL
    [SerializeField] private bool enforceURPPostFX = true;

    [SerializeField] private LayerMask forceVolumeLayerMask = default;

    [SerializeField] private int desiredRendererIndex = -1;

    [SerializeField] private bool rebuildVolumeStack = true;
#endif

    private void Awake()
    {
        if (_instance) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(onlyForScene) && scene.name != onlyForScene) return;
        StartCoroutine(RefreshLightingNextFrames(scene));
    }

    private System.Collections.IEnumerator RefreshLightingNextFrames(Scene loadedScene)
    {
        yield return null;
        yield return null;

        if (reapplySkybox) RenderSettings.skybox = RenderSettings.skybox;
        if (updateEnvironmentGI) DynamicGI.UpdateEnvironment();

        if (rerenderRealtimeReflectionProbes)
        {
            var probes = FindObjectsOfType<ReflectionProbe>(true);
            foreach (var p in probes)
                if (p && p.mode != UnityEngine.Rendering.ReflectionProbeMode.Baked)
                    p.RenderProbe();
        }

        var ddol = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (ddol.IsValid())
        {
            foreach (var root in ddol.GetRootGameObjects())
            {
                if (disableDontDestroyLights)
                    foreach (var l in root.GetComponentsInChildren<Light>(true)) l.enabled = false;

                if (disableDontDestroyReflectionProbes)
                    foreach (var rp in root.GetComponentsInChildren<ReflectionProbe>(true)) rp.enabled = false;

                if (disableDontDestroyVolumes)
                    foreach (var v in root.GetComponentsInChildren<Volume>(true)) v.enabled = false;
            }
        }

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        var cam = Camera.main;
        var camData = cam ? cam.GetComponent<UniversalAdditionalCameraData>() : null;
        if (camData)
        {
            if (enforceURPPostFX) camData.renderPostProcessing = true;

            if (desiredRendererIndex >= 0 && desiredRendererIndex < camData.scriptableRendererDataList.Count)
                camData.SetRenderer(desiredRendererIndex);

            if (forceVolumeLayerMask.value != 0)
            {
                var old = camData.volumeLayerMask;
                camData.volumeLayerMask = 0;
                yield return null;
                camData.volumeLayerMask = forceVolumeLayerMask;
            }
        }

        if (rebuildVolumeStack)
        {
            var vols = FindObjectsOfType<Volume>(true);
            foreach (var v in vols) { var was = v.enabled; v.enabled = false; v.enabled = was; }
        }

        var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        if (urpAsset)
        {
            if (!urpAsset.supportsCameraDepthTexture || !urpAsset.supportsCameraOpaqueTexture)
                Debug.LogWarning("[SceneLightingRefresher] URP asset lacks Depth/Opaque textures. AO/tonemapping may look darker after scene load.");
        }
#endif
    }
}
