using RookieToCEO.Core;
using RookieToCEO.Gameplay;
using RookieToCEO.Gameplay.Night;
using RookieToCEO.Gameplay.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // GDD 10~12번(밤 탐방 구조/발각과 실패/무기 획득 과정)의 프로토타입 범위를 Night 씬에
    // 코드로 배치한다: 경비원 2명, CCTV 1개, 조사 대상 1개, 출구 1개, 책상 장애물 몇 개.
    // PrefabAndSceneBuilder(Day 씬)와 같은 패턴 - GUI 드래그 대신 배치모드로 무인 실행한다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.NightScenePrefabBuilder.BuildNightScene -quit
    public static class NightScenePrefabBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string NightPrefabFolder = "Assets/Prefabs/Night";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string NightScenePath = "Assets/Scenes/Night.unity";

        public static void BuildNightScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError("[NightScenePrefabBuilder] Player.prefab이 없음 - 먼저 PrefabAndSceneBuilder.BuildDayScene을 실행해야 함");
                return;
            }

            EnsureFolder(NightPrefabFolder);

            var guardPrefab = BuildSensorPrefab<GuardSensor>("Guard", new Color(0.3f, 0.3f, 0.3f), halfAngle: 40f, range: 4f);
            var cctvPrefab = BuildSensorPrefab<CctvSensor>("Cctv", Color.black, halfAngle: 55f, range: 6f);
            var investigationPrefab = BuildInteractPrefab<InvestigationPoint>("InvestigationPoint", new Color(0.3f, 0.9f, 0.4f));
            var exitPrefab = BuildExitPrefab();
            var deskPrefab = BuildDeskPrefab();

            PopulateNightScene(playerPrefab, guardPrefab, cctvPrefab, investigationPrefab, exitPrefab, deskPrefab);

            Debug.Log("[NightScenePrefabBuilder] Night 씬 배치 완료");
        }

        private static GameObject BuildSensorPrefab<TSensor>(string name, Color color, float halfAngle, float range)
            where TSensor : Component
        {
            var path = $"{NightPrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            AddSquareSprite(go, color);
            var sensor = go.AddComponent<TSensor>();

            var so = new SerializedObject(sensor);
            so.FindProperty("halfAngleDegrees").floatValue = halfAngle;
            so.FindProperty("range").floatValue = range;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[NightScenePrefabBuilder] 생성: {path}");
            return prefab;
        }

        private static GameObject BuildInteractPrefab<TInteract>(string name, Color color)
            where TInteract : Component
        {
            var path = $"{NightPrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            AddSquareSprite(go, color);
            go.AddComponent<TInteract>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[NightScenePrefabBuilder] 생성: {path}");
            return prefab;
        }

        private static GameObject BuildExitPrefab()
        {
            var path = $"{NightPrefabFolder}/ExitPoint.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("ExitPoint");
            AddSquareSprite(go, new Color(0.3f, 0.5f, 1f));

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.2f, 1.2f);

            go.AddComponent<ExitPoint>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[NightScenePrefabBuilder] 생성: {path}");
            return prefab;
        }

        // GDD 10번 "책상 장애물" - 스크립트 없이 그냥 길을 막는 정적 장애물.
        private static GameObject BuildDeskPrefab()
        {
            var path = $"{NightPrefabFolder}/DeskObstacle.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("DeskObstacle");
            AddSquareSprite(go, new Color(0.5f, 0.35f, 0.2f));
            go.transform.localScale = new Vector3(1.5f, 1f, 1f);
            go.AddComponent<BoxCollider2D>(); // isTrigger 기본값 false - 물리적으로 길을 막는다

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[NightScenePrefabBuilder] 생성: {path}");
            return prefab;
        }

        private static void AddSquareSprite(GameObject go, Color color)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");
            renderer.color = color;
        }

        private static void PopulateNightScene(
            GameObject playerPrefab, GameObject guardPrefab, GameObject cctvPrefab,
            GameObject investigationPrefab, GameObject exitPrefab, GameObject deskPrefab)
        {
            var scene = EditorSceneManager.OpenScene(NightScenePath, OpenSceneMode.Single);

            DestroyIfExists("Player");
            DestroyIfExists("NightManager");
            DestroyIfExists("Guard");
            DestroyIfExists("Cctv");
            DestroyIfExists("InvestigationPoint");
            DestroyIfExists("ExitPoint");
            DestroyIfExists("DeskObstacle");

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 7f;
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.transform.rotation = Quaternion.identity;
            }

            var playerStart = new Vector3(0f, -6f, 0f);
            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = playerStart;

            // 밤 탐방 중에는 낮 무기(자동 공격)가 필요 없으니 꺼둔다 - 발각시 추격전 없이
            // 즉시 실패 처리되므로 전투가 아니라 잠입에 집중시킨다(GDD 11번).
            var shotgun = playerInstance.GetComponent<KeyboardShotgunWeapon>();
            if (shotgun != null) shotgun.enabled = false;

            var investigationInstance = (GameObject)PrefabUtility.InstantiatePrefab(investigationPrefab);
            investigationInstance.transform.position = new Vector3(0f, 6f, 0f);

            var exitInstance = (GameObject)PrefabUtility.InstantiatePrefab(exitPrefab);
            exitInstance.transform.position = new Vector3(-5f, -6f, 0f);

            // 경비원 2명 (GDD 10번) - 조사 지점으로 가는 길목 좌우에 배치, 서로 다른 방향을 본다.
            var guard1 = (GameObject)PrefabUtility.InstantiatePrefab(guardPrefab);
            guard1.transform.position = new Vector3(-2f, 0f, 0f);
            guard1.transform.rotation = Quaternion.identity; // 위쪽(조사 지점 방향)을 본다

            var guard2 = (GameObject)PrefabUtility.InstantiatePrefab(guardPrefab);
            guard2.transform.position = new Vector3(2f, 2f, 0f);
            guard2.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // 왼쪽을 본다

            // CCTV 1개 - 방 중앙 위쪽에서 플레이어 진입로를 넓게 감시.
            var cctvInstance = (GameObject)PrefabUtility.InstantiatePrefab(cctvPrefab);
            cctvInstance.transform.position = new Vector3(0f, 3f, 0f);
            cctvInstance.transform.rotation = Quaternion.Euler(0f, 0f, 180f); // 아래쪽(플레이어 시작점 방향)을 본다

            // 책상 장애물 몇 개 - 시야를 피해 돌아가야 하는 지형을 만든다.
            var desk1 = (GameObject)PrefabUtility.InstantiatePrefab(deskPrefab);
            desk1.transform.position = new Vector3(-3f, 3f, 0f);
            var desk2 = (GameObject)PrefabUtility.InstantiatePrefab(deskPrefab);
            desk2.transform.position = new Vector3(3f, -2f, 0f);
            var desk3 = (GameObject)PrefabUtility.InstantiatePrefab(deskPrefab);
            desk3.transform.position = new Vector3(1f, 4.5f, 0f);

            var nightManagerGo = new GameObject("NightManager");
            var nightManager = nightManagerGo.AddComponent<NightManager>();
            ConfigureNightManager(nightManager, playerInstance);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // NightManager의 player/weaponRewardComponent가 private [SerializeField]라 SerializedObject로 채운다.
        private static void ConfigureNightManager(NightManager nightManager, GameObject playerInstance)
        {
            var so = new SerializedObject(nightManager);
            so.FindProperty("player").objectReferenceValue = playerInstance.GetComponent<PlayerController>();

            // GDD 12번: 1층 밤 보상은 스테이플러 연사.
            so.FindProperty("weaponRewardComponent").objectReferenceValue = playerInstance.GetComponent<StaplerRapidFireWeapon>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyIfExists(string name)
        {
            GameObject found;
            while ((found = GameObject.Find(name)) != null)
            {
                Object.DestroyImmediate(found);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
