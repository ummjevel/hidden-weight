using RookieToCEO.Core;
using RookieToCEO.Gameplay;
using RookieToCEO.Gameplay.Boss;
using RookieToCEO.Gameplay.Enemies;
using RookieToCEO.Gameplay.Skills;
using RookieToCEO.Gameplay.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RookieToCEO.EditorTools
{
    // GDD 13번(CEO 최종 웨이브)의 4층 프로토타입을 Boss 씬에 코드로 배치한다: 무적 소환수
    // CeoFinalOrderBoss, 빨간 구역(HazardZone), floor=4 SpawnManager, BossWaveManager,
    // EndingTrigger. Day/Night 씬 빌더와 같은 패턴 - GUI 드래그 없이 배치모드로 무인 실행.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.BossScenePrefabBuilder.BuildBossScene -quit
    public static class BossScenePrefabBuilder
    {
        private const string BossPrefabFolder = "Assets/Prefabs/Boss";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
        private const string BossScenePath = "Assets/Scenes/Boss.unity";

        public static void BuildBossScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var emailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/EmailEnvelope.prefab");
            var documentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/DocumentStack.prefab");
            var postItPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/PostItRush.prefab");
            var meetingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/MeetingCalendar.prefab");

            if (playerPrefab == null || emailPrefab == null)
            {
                Debug.LogError("[BossScenePrefabBuilder] Player/적 프리팹이 없음 - 먼저 PrefabAndSceneBuilder.BuildDayScene을 실행해야 함");
                return;
            }

            EnsureFolder(BossPrefabFolder);

            var hazardPrefab = BuildHazardZonePrefab();
            var ceoPrefab = BuildCeoFinalOrderPrefab(emailPrefab);

            PopulateBossScene(playerPrefab, emailPrefab, documentPrefab, postItPrefab, meetingPrefab, ceoPrefab, hazardPrefab);

            Debug.Log("[BossScenePrefabBuilder] Boss 씬 배치 완료");
        }

        private static GameObject BuildHazardZonePrefab()
        {
            var path = $"{BossPrefabFolder}/HazardZone.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("HazardZone");
            AddSquareSprite(go, new Color(1f, 0.2f, 0.2f, 0.6f)); // 반투명 빨간 구역
            go.transform.localScale = new Vector3(2f, 2f, 1f);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            go.AddComponent<HazardZone>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[BossScenePrefabBuilder] 생성: {path}");
            return prefab;
        }

        private static GameObject BuildCeoFinalOrderPrefab(GameObject summonPrefab)
        {
            var path = $"{BossPrefabFolder}/CeoFinalOrder.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("CeoFinalOrder");
            AddSquareSprite(go, new Color(0.1f, 0.1f, 0.1f)); // 검은 정장 느낌
            go.transform.localScale = new Vector3(1.6f, 1.6f, 1f); // 보스답게 크게

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.6f;

            go.AddComponent<CeoFinalOrderBoss>();

            var so = new SerializedObject(go.GetComponent<CeoFinalOrderBoss>());
            so.FindProperty("summonPrefab").objectReferenceValue = summonPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[BossScenePrefabBuilder] 생성: {path}");
            return prefab;
        }

        private static void AddSquareSprite(GameObject go, Color color)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");
            renderer.color = color;
        }

        private static void PopulateBossScene(
            GameObject playerPrefab, GameObject emailPrefab, GameObject documentPrefab,
            GameObject postItPrefab, GameObject meetingPrefab, GameObject ceoPrefab, GameObject hazardPrefab)
        {
            var scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);

            DestroyIfExists("Player");
            DestroyIfExists("EnemyRegistry");
            DestroyIfExists("SpawnManager");
            DestroyIfExists("BossWaveManager");

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 7f;
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.transform.rotation = Quaternion.identity;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            }

            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = Vector3.zero;

            // 4층은 1~3층 밤을 전부 통과했다는 전제라 GDD 3번대로 모든 무기가 활성화돼 있어야 한다.
            EnableIfPresent<StaplerRapidFireWeapon>(playerInstance);
            EnableIfPresent<WorkDumpSkill>(playerInstance);
            EnableIfPresent<ResignationUltimate>(playerInstance);

            var registryGo = new GameObject("EnemyRegistry");
            registryGo.AddComponent<EnemyRegistry>();

            var spawnManagerGo = new GameObject("SpawnManager");
            var spawnManager = spawnManagerGo.AddComponent<SpawnManager>();
            ConfigureSpawnManager(spawnManager, emailPrefab, documentPrefab, postItPrefab, meetingPrefab, ceoPrefab);

            var bossWaveGo = new GameObject("BossWaveManager");
            var bossWaveManager = bossWaveGo.AddComponent<BossWaveManager>();
            ConfigureBossWaveManager(bossWaveManager, spawnManager, playerInstance, hazardPrefab);
            bossWaveGo.AddComponent<EndingTrigger>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnableIfPresent<T>(GameObject go) where T : Behaviour
        {
            var component = go.GetComponent<T>();
            if (component != null) component.enabled = true;
        }

        private static void ConfigureSpawnManager(
            SpawnManager spawnManager, GameObject emailPrefab, GameObject documentPrefab,
            GameObject postItPrefab, GameObject meetingPrefab, GameObject ceoPrefab)
        {
            var so = new SerializedObject(spawnManager);
            so.FindProperty("floor").intValue = 4;

            var list = so.FindProperty("enemyPrefabs");
            list.ClearArray();
            AddPrefabEntry(list, EnemyType.EmailEnvelope, emailPrefab);
            AddPrefabEntry(list, EnemyType.DocumentStack, documentPrefab);
            AddPrefabEntry(list, EnemyType.PostItRush, postItPrefab);
            AddPrefabEntry(list, EnemyType.MeetingCalendar, meetingPrefab);
            AddPrefabEntry(list, EnemyType.CeoFinalOrder, ceoPrefab);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPrefabEntry(SerializedProperty listProp, EnemyType type, GameObject prefab)
        {
            var index = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(index);
            var element = listProp.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Type").enumValueIndex = (int)type;
            element.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
        }

        private static void ConfigureBossWaveManager(
            BossWaveManager bossWaveManager, SpawnManager spawnManager, GameObject playerInstance, GameObject hazardPrefab)
        {
            var so = new SerializedObject(bossWaveManager);
            so.FindProperty("spawnManager").objectReferenceValue = spawnManager;
            so.FindProperty("player").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            so.FindProperty("hazardZonePrefab").objectReferenceValue = hazardPrefab;
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
