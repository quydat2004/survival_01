using Survival.Backend;
using Survival.UI;
using Survival.World;
using UnityEngine;

namespace Survival.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private static bool initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnSceneLoad()
        {
            EnsureGameSystems();
        }

        private void Awake()
        {
            EnsureGameSystems();
        }

        public static void EnsureGameSystems()
        {
            if (initialized && MatchManager.Instance != null)
                return;

            initialized = true;

            ConfigureCrispGraphicsQuality();

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.nearClipPlane = 0.18f;
            cam.farClipPlane = 720f;
            cam.allowMSAA = true;
            cam.allowHDR = true;

            Light light = FindFirstObjectByType<Light>();
            if (light == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                light = lightGo.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.82f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.35f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject systemsRoot = GameObject.Find("Survival01_Systems");
            if (systemsRoot == null)
            {
                systemsRoot = new GameObject("Survival01_Systems");
                if (Application.isPlaying)
                    DontDestroyOnLoad(systemsRoot);
            }

            if (SupabaseManager.Instance == null && systemsRoot.GetComponent<SupabaseManager>() == null)
                systemsRoot.AddComponent<SupabaseManager>();

            if (BattleRoyaleMapGenerator.Instance == null && systemsRoot.GetComponent<BattleRoyaleMapGenerator>() == null)
                systemsRoot.AddComponent<BattleRoyaleMapGenerator>();

            if (BlueZoneController.Instance == null && systemsRoot.GetComponent<BlueZoneController>() == null)
                systemsRoot.AddComponent<BlueZoneController>();

            if (AirDropManager.Instance == null && systemsRoot.GetComponent<AirDropManager>() == null)
                systemsRoot.AddComponent<AirDropManager>();

            if (MatchManager.Instance == null && systemsRoot.GetComponent<MatchManager>() == null)
                systemsRoot.AddComponent<MatchManager>();

            if (SurvivalUIManager.Instance == null && systemsRoot.GetComponent<SurvivalUIManager>() == null)
                systemsRoot.AddComponent<SurvivalUIManager>();
        }

        private static void ConfigureCrispGraphicsQuality()
        {
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 160f;
            QualitySettings.lodBias = 2.0f;
            Application.targetFrameRate = 120;
        }
    }
}
