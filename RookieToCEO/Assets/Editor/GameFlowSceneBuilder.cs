using RookieToCEO.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RookieToCEO.EditorTools
{
    // GDD 14번(최종 플레이 흐름)을 실제로 재생시키기 위한 마지막 조각: Bootstrap 씬(지속되는
    // Player + GameFlowManager)과 Ending 씬을 만들고, Day/Night/Boss와 함께 Build Settings에
    // 등록한다. Build Settings 등록이 없으면 SceneManager.LoadScene(이름)이 배치모드 밖(실제
    // 빌드)에서 작동하지 않는다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.GameFlowSceneBuilder.BuildAll -quit
    public static class GameFlowSceneBuilder
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string EndingScenePath = "Assets/Scenes/Ending.unity";

        public static void BuildAll()
        {
            BuildBootstrapScene();
            BuildEndingScene();
            ConfigureBuildSettings();

            Debug.Log("[GameFlowSceneBuilder] Bootstrap/Ending 씬 생성 + Build Settings 등록 완료");
        }

        private static void BuildBootstrapScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError("[GameFlowSceneBuilder] Player.prefab이 없음 - 먼저 PrefabAndSceneBuilder.BuildDayScene을 실행해야 함");
                return;
            }

            var scene = System.IO.File.Exists(BootstrapScenePath)
                ? EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            DestroyIfExists("Player");
            DestroyIfExists("GameFlowManager");

            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = Vector3.zero;

            var flowManagerGo = new GameObject("GameFlowManager");
            var flowManager = flowManagerGo.AddComponent<GameFlowManager>();

            var so = new SerializedObject(flowManager);
            so.FindProperty("player").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void BuildEndingScene()
        {
            var scene = System.IO.File.Exists(EndingScenePath)
                ? EditorSceneManager.OpenScene(EndingScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            DestroyIfExists("EndingCanvas");
            DestroyIfExists("EventSystem");

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 6f;
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.transform.rotation = Quaternion.identity;
            }

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("EndingCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("EndingText", typeof(RectTransform));
            textGo.transform.SetParent(canvasGo.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 36;
            // GDD 13번 마지막 줄: "CEO 웨이브 방어 성공 -> 기존 CEO 퇴사 -> 주인공 CEO 취임 -> 엔딩".
            text.text = "기존 CEO 퇴사.\n당신이 새로운 CEO로 취임합니다.\n\n- The End -";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EndingScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/Day.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Night.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Boss.unity", true),
                new EditorBuildSettingsScene(EndingScenePath, true),
            };
        }

        private static void DestroyIfExists(string name)
        {
            GameObject found;
            while ((found = GameObject.Find(name)) != null)
            {
                Object.DestroyImmediate(found);
            }
        }
    }
}
