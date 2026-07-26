using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HiddenWeight.EditorTools
{
    // 프로젝트 최초 세팅 자동화. 레이어, URP 에셋, 지역별 Volume 프로파일, PlayerSettings를
    // 코드로 남겨 두면 프로젝트를 다시 만들어도 동일한 상태를 재현할 수 있다.
    // EditorApplication.Exit는 호출하지 않는다 — 다른 빌더(예: Task 13의 씬 생성기)가
    // 이 메서드를 그대로 재사용하기 때문이다.
    public static class ProjectSetup
    {
        // 8~14번 슬롯에 순서대로 채워 넣을 사용자 정의 레이어. (기획 문서 2.1절)
        private static readonly string[] LayerNames =
        {
            "Ground", "Wall", "Player", "PlayerHushed", "Enemy", "Hazard", "Interactable",
        };

        // 지역별 색보정 수치 (설계 문서 6.1절 = 기획서 7.1절)
        private static readonly (string zoneName, float exposure, float saturation, float hue, float contrast)[] ZoneColorGrading =
        {
            ("Prologue", 0f, 0f, 0f, 0f),
            ("Residue", -1.2f, -45f, -15f, 0f),
            ("Gaze", -0.6f, -20f, 25f, -15f),
            ("Fracture", 0.8f, 30f, 40f, 0f),
        };

        public static void Run()
        {
            RegisterLayers();
            SetupLayerCollisionMatrix();
            SetupUniversalRenderPipeline();
            CreateVolumeProfiles();
            ConfigurePlayerSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 1. 레이어 등록: TagManager.asset을 SerializedObject로 열어 8번 슬롯부터 채운다.
        private static void RegisterLayers()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            var layersProp = tagManager.FindProperty("layers");

            for (int i = 0; i < LayerNames.Length; i++)
            {
                var layerSlot = layersProp.GetArrayElementAtIndex(8 + i);
                layerSlot.stringValue = LayerNames[i];
            }

            tagManager.ApplyModifiedProperties();
        }

        // 2. 레이어 충돌 행렬: 숨죽이기 상태(PlayerHushed)는 적과 충돌하지 않는다.
        private static void SetupLayerCollisionMatrix()
        {
            int hushedLayer = LayerMask.NameToLayer("PlayerHushed");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            Physics2D.IgnoreLayerCollision(hushedLayer, enemyLayer, true);
        }

        // 3. URP 에셋 생성: Renderer2DData + UniversalRenderPipelineAsset을 만들고
        //    GraphicsSettings와 모든 Quality 레벨에 연결한다.
        private static void SetupUniversalRenderPipeline()
        {
            EnsureSettingsFolder();

            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, "Assets/Settings/HiddenWeight_Renderer2D.asset");

            var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, "Assets/Settings/HiddenWeight_URP.asset");

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            int originalLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipelineAsset;
            }
            QualitySettings.SetQualityLevel(originalLevel, applyExpensiveChanges: false);
        }

        // 4. Volume 프로파일: 지역 4종 + 자각 전용 1종.
        private static void CreateVolumeProfiles()
        {
            foreach (var zone in ZoneColorGrading)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                // VolumeProfile.Add<T>()는 컴포넌트를 메모리에만 만든다.
                // 에셋에 실제로 저장하려면 CreateAsset 이후 AddObjectToAsset으로 서브에셋으로 붙여야 한다.
                AssetDatabase.CreateAsset(profile, $"Assets/Settings/Volume_{zone.zoneName}.asset");

                var colorAdjustments = profile.Add<ColorAdjustments>(true);
                colorAdjustments.postExposure.value = zone.exposure;
                colorAdjustments.saturation.value = zone.saturation;
                colorAdjustments.hueShift.value = zone.hue;
                colorAdjustments.contrast.value = zone.contrast;
                AssetDatabase.AddObjectToAsset(colorAdjustments, profile);
            }

            // 자각(L 홀드) 전용: 채도 -80 + Vignette로 "채도를 잃는 연출" (기획서 7.2절)
            var awarenessProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(awarenessProfile, "Assets/Settings/Volume_Awareness.asset");

            var awarenessColor = awarenessProfile.Add<ColorAdjustments>(true);
            awarenessColor.saturation.value = -80f;
            AssetDatabase.AddObjectToAsset(awarenessColor, awarenessProfile);

            var vignette = awarenessProfile.Add<Vignette>(true);
            vignette.intensity.value = 0.4f;
            AssetDatabase.AddObjectToAsset(vignette, awarenessProfile);
        }

        // 5. PlayerSettings: 회사명·제품명·기본 해상도.
        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "NHN Hackerton";
            PlayerSettings.productName = "Hidden Weight";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsFullScreen = false;
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }
    }
}
