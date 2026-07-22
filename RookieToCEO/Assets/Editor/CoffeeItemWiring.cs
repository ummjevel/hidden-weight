using System.IO;
using RookieToCEO.Core;
using RookieToCEO.Gameplay.Enemies;
using RookieToCEO.Gameplay.Items;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RookieToCEO.EditorTools
{
    // GDD 4번(회복 아이템) 폴리싱: 커피(아메리카노) 프리팹을 만들고, 이미 만들어져 있는 적 5종
    // 프리팹(Assets/Prefabs/Enemies/*.prefab)의 coffeeDropPrefab 필드를 채운다.
    // 적 프리팹은 PrefabAndSceneBuilder가 "이미 있으면 건너뜀" 방식이라 재생성이 아니라
    // PrefabUtility.LoadPrefabContents로 기존 애셋을 직접 열어서 필드만 덧붙인다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.CoffeeItemWiring.WireAll -quit
    public static class CoffeeItemWiring
    {
        private const string ItemsFolder = "Assets/Prefabs/Items";
        private const string CoffeePrefabPath = ItemsFolder + "/Coffee.prefab";
        private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
        private const string BalanceDataPath = "Assets/ScriptableObjects/BalanceData.asset";

        private static readonly string[] EnemyPrefabNames =
        {
            "EmailEnvelope", "DocumentStack", "PostItRush", "MeetingCalendar", "ClaimPhone",
        };

        public static void WireAll()
        {
            var balanceData = AssetDatabase.LoadAssetAtPath<BalanceData>(BalanceDataPath);
            var coffeePrefab = BuildCoffeePrefab(balanceData);

            foreach (var name in EnemyPrefabNames)
            {
                WireCoffeeIntoEnemyPrefab(name, coffeePrefab);
            }

            Debug.Log("[CoffeeItemWiring] 커피 드롭 연결 완료");
        }

        private static GameObject BuildCoffeePrefab(BalanceData balanceData)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CoffeePrefabPath);
            if (existing != null) return existing;

            EnsureFolder(ItemsFolder);

            var go = new GameObject("Coffee");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholder/Square.png");
            renderer.color = new Color(0.36f, 0.2f, 0.09f); // 아메리카노 - 진한 갈색
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // 작은 픽업 아이템

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            go.AddComponent<CoffeeDrop>();

            var so = new SerializedObject(go.GetComponent<CoffeeDrop>());
            so.FindProperty("balanceData").objectReferenceValue = balanceData;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CoffeePrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[CoffeeItemWiring] 생성: {CoffeePrefabPath}");
            return prefab;
        }

        private static void WireCoffeeIntoEnemyPrefab(string enemyName, GameObject coffeePrefab)
        {
            var path = $"{EnemyPrefabFolder}/{enemyName}.prefab";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[CoffeeItemWiring] 프리팹을 못 찾음: {path}");
                return;
            }

            var contentsRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var enemy = contentsRoot.GetComponent<EnemyBase>();
                if (enemy == null)
                {
                    Debug.LogWarning($"[CoffeeItemWiring] EnemyBase 컴포넌트가 없음: {path}");
                    return;
                }

                var so = new SerializedObject(enemy);
                var prop = so.FindProperty("coffeeDropPrefab");
                prop.objectReferenceValue = coffeePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contentsRoot, path);
                Debug.Log($"[CoffeeItemWiring] coffeeDropPrefab 연결: {path}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contentsRoot);
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
