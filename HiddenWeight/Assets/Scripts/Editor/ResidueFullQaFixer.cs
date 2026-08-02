using HiddenWeight.UI;
using HiddenWeight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.IO;

namespace HiddenWeight.EditorTools
{
    public static partial class ZoneSceneBuilder
    {
        const string ResidueFullScenePath = "Assets/Scenes/Zone_Residue_Full.unity";
        const string ResidueR02ScenePath = "Assets/Scenes/Room_Residue_R02.unity";

        // R02의 하부 우회로에서 머리 위 충돌만 느껴지고 다리 조각의 밑면이 보이지 않던 문제를
        // 정식 Full 씬과 방 단독 QA 씬에 함께 수정한다. 충돌 크기와 위치는 바꾸지 않는다.
        [MenuItem("Hidden Weight/Fix Residue R02 Invisible Ceiling")]
        public static void PatchResidueR02InvisibleCeiling()
        {
            ResidueModularArtSlicer.SliceAll();
            UseArtRoot("Assets/Art/Residue");

            PatchR02BridgeScene(ResidueFullScenePath);
            PatchR02BridgeScene(ResidueR02ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ResidueFullQaFixer] R02 다리 A/B 하부 가시성 수정 완료 (Full + Room)");
        }

        static void PatchR02BridgeScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var bridgeA = GameObject.Find("R02_Bridge_A");
            var bridgeB = GameObject.Find("R02_Bridge_B");
            if (bridgeA == null || bridgeB == null)
                throw new System.InvalidOperationException($"R02 다리 조각을 찾지 못했다: {scenePath}");

            ApplyResiduePlatformV3(bridgeA, "ResiduePlatformShort", 2.8f);
            ApplyResiduePlatformV3(bridgeB, "ResiduePlatformShort", 2.3f);
            EditorSceneManager.SaveScene(scene);
        }

        // 정식 플레이가 아직 사용하는 Full 씬에 R07→R08 QA 수정만 반영한다.
        // 방별 씬 빌더의 같은 수정과 좌표를 공유하되 다른 지역 씬은 열거나 저장하지 않는다.
        [MenuItem("Hidden Weight/Fix Residue Full R07-R08 QA Issues")]
        public static void PatchResidueFullQaIssues()
        {
            ResidueArtSlicer.SliceAll();
            ResidueModularArtSlicer.SliceAll();
            UseArtRoot("Assets/Art/Residue");

            Scene scene = EditorSceneManager.OpenScene(ResidueFullScenePath, OpenSceneMode.Single);
            var tilemap = Object.FindAnyObjectByType<Tilemap>();
            var r07 = GameObject.Find("Room07")?.GetComponent<Room>();
            var r08Left = GameObject.Find("R08_Chimney_L")?.GetComponent<BoxCollider2D>();
            var r08Right = GameObject.Find("R08_Chimney_R")?.GetComponent<BoxCollider2D>();
            if (tilemap == null || r07 == null || r08Left == null || r08Right == null)
                throw new System.InvalidOperationException("잔재 Full 씬의 R07/R08 필수 지형을 찾지 못했다.");

            PatchR07ExitFloor(r07);
            PatchR07StepVisual(r07);
            PatchR08Chimney(tilemap, r08Left, r08Right);
            PatchR05RewindHint();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ResidueFullQaFixer] R07 바닥·계단·운반자 슬라이스·R08 굴뚝·R05 안내 수정 완료");
        }

        // 잔재 방 씬과 현재 정식 플레이용 Full 씬의 배경만 V3 원경으로 교체한다.
        // 응시·균열 씬은 검색 단계에서 제외하고, 방 구조·충돌·몬스터는 수정하지 않는다.
        [MenuItem("Hidden Weight/Art/Apply Residue Modular V3 Environment")]
        public static void PatchResidueV3Environment()
        {
            ResidueModularArtSlicer.SliceAll();
            SingleRoomBackgroundBuilder.BuildTraversalArtPalette();

            int sceneCount = 0;
            int backgroundCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName != "Zone_Residue_Full"
                    && !fileName.StartsWith("Room_Residue_", System.StringComparison.Ordinal))
                    continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var locked in Object.FindObjectsByType<CameraLockedRoomBackground>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Room room = locked.GetComponentInParent<Room>();
                    if (room == null) continue;

                    string backgroundPath = SingleRoomBackgroundBuilder.ResidueBackgroundPath(
                        room, "Assets/Art/Residue");
                    SingleRoomBackgroundBuilder.ConfigureBackgroundImport(backgroundPath);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
                    var renderer = locked.GetComponent<SpriteRenderer>();
                    if (sprite == null || renderer == null)
                        throw new System.InvalidOperationException(
                            $"잔재 V3 배경 적용 실패: {path} / {room.name}");

                    renderer.sprite = sprite;
                    var serialized = new SerializedObject(locked);
                    serialized.FindProperty("backgroundTint").colorValue =
                        new Color(0.82f, 0.86f, 0.94f, 0.82f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(renderer);
                    EditorUtility.SetDirty(locked);
                    backgroundCount++;
                    changed = true;
                }

                if (changed)
                {
                    EditorSceneManager.SaveScene(scene);
                    sceneCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ResidueV3] 잔재 씬 {sceneCount}개, 배경 {backgroundCount}개 교체 완료");
        }

        static void PatchR07ExitFloor(Room room)
        {
            var old = GameObject.Find("R07_ExitFloorVisual");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("R07_ExitFloorVisual");
            go.transform.SetParent(room.transform.parent.parent, false);
            go.transform.position = new Vector3(room.WorldBounds.min.x + 26.5f,
                room.WorldBounds.min.y + 6.5f, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Art("Terrain_r1_c2");
            if (renderer.sprite == null)
                throw new System.InvalidOperationException("R07 출구 바닥 스프라이트를 찾지 못했다.");
            renderer.sortingOrder = 3;
            FitSprite(renderer, 7f, 2f);
        }

        static void PatchR07StepVisual(Room room)
        {
            Vector2 expectedCenter = (Vector2)room.WorldBounds.min + new Vector2(21.5f, 6.5f);
            BoxCollider2D step = null;
            float bestDistance = float.MaxValue;
            foreach (var candidate in Object.FindObjectsByType<BoxCollider2D>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name != "SafePlatform" || !room.WorldBounds.Contains(candidate.bounds.center))
                    continue;
                float distance = Vector2.Distance(candidate.bounds.center, expectedCenter);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                step = candidate;
            }
            if (step == null || bestDistance > 0.25f)
                throw new System.InvalidOperationException("R07 중간 계단 충돌체를 찾지 못했다.");

            ApplyResiduePlatformV3(step.gameObject, "ResiduePlatformShort", 3.2f);
            var fakeWall = GameObject.Find("R07_StairVisual");
            if (fakeWall != null) Object.DestroyImmediate(fakeWall);
        }

        static void PatchR08Chimney(Tilemap tilemap, BoxCollider2D left, BoxCollider2D right)
        {
            int originX = Mathf.RoundToInt(left.transform.position.x) - 4;
            int originY = Mathf.RoundToInt(left.transform.position.y) - 8;
            var groundTile = tilemap.GetTile(new Vector3Int(originX, originY + 1, 0));
            if (groundTile == null)
                throw new System.InvalidOperationException("R08 안전 착지대에 사용할 바닥 타일을 찾지 못했다.");

            // 굴뚝 안을 가로막던 local x=2~10, y=10 타일을 제거한다.
            for (int x = originX + 2; x < originX + 11; x++)
                tilemap.SetTile(new Vector3Int(x, originY + 10, 0), null);

            // 두 벽의 안쪽 면 사이를 비워 둔 채 오른쪽 바깥에만 착지대를 만든다.
            for (int x = originX + 8; x < originX + 12; x++)
                tilemap.SetTile(new Vector3Int(x, originY + 12, 0), groundTile);

            float innerWidth = right.bounds.min.x - left.bounds.max.x;
            if (Mathf.Abs(innerWidth - 3f) > 0.05f)
                throw new System.InvalidOperationException($"R08 굴뚝 유효 폭이 3이 아니다: {innerWidth:F2}");
        }

        static void PatchR05RewindHint()
        {
            var room = GameObject.Find("Room05")?.GetComponent<Room>();
            if (room == null) return;

            foreach (var hint in Object.FindObjectsByType<TutorialHint>())
            {
                if (!room.WorldBounds.Contains(hint.transform.position)) continue;
                var serialized = new SerializedObject(hint);
                serialized.FindProperty("message").stringValue =
                    "K를 계속 누르면 구조물이 복원됩니다.";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
