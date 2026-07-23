using System.IO;
using RookieToCEO.Core;
using RookieToCEO.Gameplay;
using RookieToCEO.Gameplay.Enemies;
using RookieToCEO.Gameplay.Skills;
using RookieToCEO.Gameplay.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // GUI로 프리팹을 드래그해서 만드는 대신, 플레이어/적 프리팹을 코드로 생성하고 Day 씬에
    // 배치하는 자동화 스크립트. 스프라이트는 아직 없으므로(docs/DEVELOPMENT_PLAN.md 아트
    // 파이프라인 방침) 색이 다른 정사각형(프로그래머 아트)으로 표시한다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.PrefabAndSceneBuilder.BuildDayScene -quit
    public static class PrefabAndSceneBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
        private const string BalanceDataPath = "Assets/ScriptableObjects/BalanceData.asset";
        private const string DayScenePath = "Assets/Scenes/Day.unity";

        public static void BuildDayScene()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(EnemyPrefabFolder);

            var balanceData = AssetDatabase.LoadAssetAtPath<BalanceData>(BalanceDataPath);

            var playerPrefab = BuildPlayerPrefab(balanceData);
            var emailPrefab = BuildEnemyPrefab<EmailEnvelopeEnemy>("EmailEnvelope", new Color(1f, 0.92f, 0.4f), balanceData);
            var documentPrefab = BuildEnemyPrefab<DocumentStackEnemy>("DocumentStack", new Color(0.55f, 0.4f, 0.2f), balanceData);
            var postItPrefab = BuildEnemyPrefab<PostItRushEnemy>("PostItRush", new Color(1f, 0.5f, 0.7f), balanceData);
            var meetingPrefab = BuildEnemyPrefab<MeetingCalendarEnemy>("MeetingCalendar", new Color(0.4f, 0.6f, 1f), balanceData);
            var claimPhonePrefab = BuildEnemyPrefab<ClaimPhoneEnemy>("ClaimPhone", new Color(0.9f, 0.2f, 0.2f), balanceData);

            PopulateDayScene(playerPrefab, emailPrefab, documentPrefab, postItPrefab, meetingPrefab, claimPhonePrefab);

            Debug.Log("[PrefabAndSceneBuilder] Day 씬 배치 완료");
        }

        private static GameObject BuildPlayerPrefab(BalanceData balanceData)
        {
            var path = $"{PrefabFolder}/Player.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("Player");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            AddSquareSprite(go, new Color(1f, 1f, 1f));

            go.AddComponent<PlayerController>();
            go.AddComponent<KeyboardShotgunWeapon>(); // 시작 무기(GDD 3번) - 처음부터 활성화

            // 스테이플러/업무 떠넘기기/퇴사 통보는 각 밤 조사를 마쳐야 실제 보유(GDD 12번)하므로
            // 프리팹에는 붙여두되 비활성 상태로 시작한다. NightManager가 조사 성공 시 활성화한다.
            var stapler = go.AddComponent<StaplerRapidFireWeapon>();
            stapler.enabled = false;
            var workDump = go.AddComponent<WorkDumpSkill>();
            workDump.enabled = false;
            var ultimate = go.AddComponent<ResignationUltimate>();
            ultimate.enabled = false;

            AssignBalanceData(stapler, balanceData);
            AssignBalanceData(go.GetComponent<KeyboardShotgunWeapon>(), balanceData);
            AssignBalanceData(workDump, balanceData);
            AssignBalanceData(ultimate, balanceData);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabAndSceneBuilder] 생성: {path}");
            return prefab;
        }

        private static GameObject BuildEnemyPrefab<TEnemy>(string name, Color color, BalanceData balanceData)
            where TEnemy : EnemyBase
        {
            var path = $"{EnemyPrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;

            AddSquareSprite(go, color);

            var enemy = go.AddComponent<TEnemy>();
            AssignBalanceData(enemy, balanceData);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabAndSceneBuilder] 생성: {path}");
            return prefab;
        }

        // 도트 스프라이트가 준비되기 전까지 색이 다른 정사각형으로 구분한다(프로그래머 아트).
        // Unity 내장 리소스 이름에 기대는 대신, 흰색 정사각형 PNG를 직접 만들어 확실하게 동작시킨다.
        private static void AddSquareSprite(GameObject go, Color color)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetOrCreatePlaceholderSprite();
            renderer.color = color;
        }

        private const string PlaceholderArtFolder = "Assets/Art/Placeholder";
        private const string PlaceholderSquarePath = PlaceholderArtFolder + "/Square.png";

        private static Sprite GetOrCreatePlaceholderSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSquarePath);
            if (existing != null) return existing;

            EnsureFolder(PlaceholderArtFolder);

            // docs/DEVELOPMENT_PLAN.md 아트 파이프라인 방침(32x32, PPU 32)에 맞춘 흰색 정사각형.
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(PlaceholderSquarePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PlaceholderSquarePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSquarePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSquarePath);
        }

        // balanceData 필드가 private [SerializeField]라 SerializedObject로 직접 대입한다.
        private static void AssignBalanceData(Object component, BalanceData balanceData)
        {
            if (component == null || balanceData == null) return;

            var so = new SerializedObject(component);
            var prop = so.FindProperty("balanceData");
            if (prop == null) return;

            prop.objectReferenceValue = balanceData;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PopulateDayScene(
            GameObject playerPrefab, GameObject emailPrefab, GameObject documentPrefab,
            GameObject postItPrefab, GameObject meetingPrefab, GameObject claimPhonePrefab)
        {
            var scene = EditorSceneManager.OpenScene(DayScenePath, OpenSceneMode.Single);

            // 이 메서드를 두 번 이상 돌리면 Player/매니저가 중복 생성되므로, 이전에 만들어둔 것이
            // 있으면 먼저 지운다 (재실행해도 항상 같은 결과가 나오도록).
            DestroyIfExists("Player");
            DestroyIfExists("EnemyRegistry");
            DestroyIfExists("SpawnManager");
            DestroyIfExists("DayWaveManager");

            // 2D 탑뷰이므로 메인 카메라를 정사영(orthographic)으로 맞춘다.
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 6f;
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.transform.rotation = Quaternion.identity;
                // 기본 Skybox를 그대로 두면 orthographic 카메라에서 사막색처럼 이상하게 보인다.
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            }

            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = Vector3.zero;
            var player = playerInstance.GetComponent<PlayerController>();

            var registryGo = new GameObject("EnemyRegistry");
            registryGo.AddComponent<EnemyRegistry>();

            var spawnManagerGo = new GameObject("SpawnManager");
            var spawnManager = spawnManagerGo.AddComponent<SpawnManager>();
            ConfigureSpawnManager(spawnManager, emailPrefab, documentPrefab, postItPrefab, meetingPrefab, claimPhonePrefab);

            var dayWaveGo = new GameObject("DayWaveManager");
            var dayWaveManager = dayWaveGo.AddComponent<DayWaveManager>();
            ConfigureDayWaveManager(dayWaveManager, spawnManager, player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // SpawnManager의 floor/enemyPrefabs가 private [SerializeField]라 SerializedObject로 채운다.
        private static void ConfigureSpawnManager(
            SpawnManager spawnManager, GameObject emailPrefab, GameObject documentPrefab,
            GameObject postItPrefab, GameObject meetingPrefab, GameObject claimPhonePrefab)
        {
            var so = new SerializedObject(spawnManager);
            so.FindProperty("floor").intValue = 1;

            var list = so.FindProperty("enemyPrefabs");
            list.ClearArray();
            AddPrefabEntry(list, EnemyType.EmailEnvelope, emailPrefab);
            AddPrefabEntry(list, EnemyType.DocumentStack, documentPrefab);
            AddPrefabEntry(list, EnemyType.PostItRush, postItPrefab);
            AddPrefabEntry(list, EnemyType.MeetingCalendar, meetingPrefab);
            AddPrefabEntry(list, EnemyType.ClaimPhone, claimPhonePrefab);

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

        private static void ConfigureDayWaveManager(DayWaveManager dayWaveManager, SpawnManager spawnManager, PlayerController player)
        {
            var so = new SerializedObject(dayWaveManager);
            so.FindProperty("floor").intValue = 1;
            so.FindProperty("spawnManager").objectReferenceValue = spawnManager;
            so.FindProperty("player").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // AssetDatabase.CreateFolder는 한 단계씩만 만들 수 있고 부모가 없으면 실패하므로,
        // "Assets/Art/Placeholder"처럼 중첩된 경로도 안전하게 만들 수 있게 재귀적으로 처리한다.
        // GameObject.Find는 첫 매치만 찾으므로, 이전 실행에서 중복 생성된 게 여러 개 있어도
        // 전부 지울 때까지 반복한다.
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

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
