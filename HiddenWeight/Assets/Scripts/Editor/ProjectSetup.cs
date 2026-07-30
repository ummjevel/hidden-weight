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
        // 8~15번 슬롯에 순서대로 채워 넣을 사용자 정의 레이어. (기획 문서 2.1절)
        // EnemyHitbox: 접촉 피해 전용 트리거 자식이 쓰는 레이어. Enemy 본체 레이어는 Player와
        // 물리 충돌을 꺼서(밀지 않게) 두되, 접촉 피해 판정만은 이 레이어의 트리거로 계속 받는다.
        private static readonly string[] LayerNames =
        {
            "Ground", "Wall", "Player", "PlayerHushed", "Enemy", "Hazard", "Interactable", "EnemyHitbox",
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

        // Run() 전체를 다시 돌리면 URP 파이프라인/렌더러 에셋이 새 GUID로 재생성되어 ZoneData의
        // Volume 참조가 깨진다(README 참고). 레이어 이름·충돌 행렬만 필요할 때는 이 둘만 다시 돈다 —
        // 둘 다 몇 번을 다시 실행해도 같은 결과라 안전하다.
        [MenuItem("Hidden Weight/Fix/Apply Enemy Layer Setup")]
        public static void ApplyEnemyLayerSetup()
        {
            RegisterLayers();
            SetupLayerCollisionMatrix();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetup] EnemyHitbox 레이어 등록 + 충돌 행렬(Player-Enemy 무시) 적용 완료");
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
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int enemyHitboxLayer = LayerMask.NameToLayer("EnemyHitbox");
            Physics2D.IgnoreLayerCollision(hushedLayer, enemyLayer, true);
            Physics2D.IgnoreLayerCollision(hushedLayer, enemyHitboxLayer, true);

            // 적 본체와 플레이어는 서로 밀지 않는다(둘 다 Dynamic Rigidbody2D라 그냥 두면 부딪힌
            // 쪽이 물리적으로 떠밀린다). 접촉 피해는 별도의 EnemyHitbox 트리거로 계속 받으므로
            // 이 레이어 쌍만 물리 충돌을 꺼도 안전하다.
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }

        // 3. URP 에셋 생성: Renderer2DData + UniversalRenderPipelineAsset을 만들고
        //    GraphicsSettings와 모든 Quality 레벨에 연결한다.
        //
        //    재실행 대비: Run()을 다시 실행하면 이전 실행이 만든 에셋이 같은 경로에 이미 있고,
        //    그 에셋이 여전히 "현재 활성 파이프라인"으로 GraphicsSettings/QualitySettings에 걸려 있다.
        //    이 상태에서 곧바로 CreateAsset으로 렌더러 데이터 파일을 덮어쓰면, 활성 파이프라인이
        //    한순간 렌더러를 잃어 "Default Renderer is missing" 에러가 찍힌다. 그래서 지우기 전에
        //    참조부터 전부 비우고, 그다음 이전 에셋을 지우고, 마지막에 새로 만들어 다시 연결한다.
        private static void SetupUniversalRenderPipeline()
        {
            EnsureSettingsFolder();

            const string rendererPath = "Assets/Settings/HiddenWeight_Renderer2D.asset";
            const string pipelinePath = "Assets/Settings/HiddenWeight_URP.asset";

            GraphicsSettings.defaultRenderPipeline = null;
            int originalLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = null;
            }

            DeleteAssetIfExists(pipelinePath);
            DeleteAssetIfExists(rendererPath);

            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, rendererPath);

            var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, pipelinePath);

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipelineAsset;
            }
            QualitySettings.SetQualityLevel(originalLevel, applyExpensiveChanges: false);
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
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
