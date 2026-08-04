using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.World;

namespace HiddenWeight.EditorTools
{
    // 잔재 지역(주 동선 12룸 + 비밀 3룸)을 방마다 자기 씬으로 짓는다.
    // 명세: docs/RESIDUE_ROOM_IMPLEMENTATION.md, docs/RESIDUE_LEVEL_DESIGN.md, docs/WORLD_MAP.md
    //
    // 방은 저마다 왼쪽 아래 (0,0)을 쓴다(LEVEL_01_STANDARD 1.1). 한 씬에 한 방만 로드되므로
    // 방끼리 원점을 공유해도 겹치지 않고, 전역 오프셋을 계산할 이유가 사라졌다 — 방 코드의
    // 숫자가 문서에 적힌 숫자와 그대로 1:1이다.
    //
    // 방과 방은 복도가 아니라 포탈 문(RoomDoor)이 잇는다. 어떤 방이 어떤 방과 어디서 붙는지는
    // ResidueRoomLinks.Links 한 곳에만 있다.
    public static partial class ZoneSceneBuilder
    {
        // 잘라 둔 잔재 스프라이트를 이름으로 찾는다(ResidueArtSlicer가 붙인 이름).
        // 없으면 null을 돌려주고, 호출부는 기존 플레이스홀더로 넘어간다 — 아트가 아직 없는
        // 항목 때문에 씬 생성이 실패하면 안 된다.
        static Dictionary<string, Sprite> _residueSprites;

        // 지금 어느 지역의 아트를 집을지. 지역마다 같은 이름의 스프라이트가 있으므로
        // (응시에도 Platform_r1_c1이 있다) 한 폴더만 보게 해야 서로 섞이지 않는다.
        // 각 지역 빌더가 시작할 때 자기 폴더로 바꾼다.
        static string _artRoot = "Assets/Art/Residue";

        // 지역마다 시트 접두사가 다르다(잔재 Terrain / 응시 GazeTerrain). 공용 빌더가 쓰는
        // 논리 이름 앞에 이것을 붙여 실제 스프라이트 이름을 만든다.
        static string _artPrefix = "";

        // 숏컷 겉모습은 접두사 규칙으로 안 풀린다(잔재는 사슬·승강기, 응시는 문 시트라
        // 행·열 위치가 다르다). 그래서 이름을 통째로 지역이 정한다.
        static string _shortcutClosedArt = "Shortcut_ChainBroken";
        static string _shortcutOpenArt = "Shortcut_ChainRestored";
        static string _shortcutLiftClosedArt = "Shortcut_LiftDormant";
        static string _shortcutLiftOpenArt = "Shortcut_LiftActive";

        static void UseArtRoot(string root, string prefix = "",
                               string shortcutClosed = "Shortcut_ChainBroken",
                               string shortcutOpen = "Shortcut_ChainRestored",
                               string liftClosed = "Shortcut_LiftDormant",
                               string liftOpen = "Shortcut_LiftActive")
        {
            _artPrefix = prefix;
            _shortcutClosedArt = shortcutClosed;
            _shortcutOpenArt = shortcutOpen;
            _shortcutLiftClosedArt = liftClosed;
            _shortcutLiftOpenArt = liftOpen;

            if (_artRoot == root) return;

            _artRoot = root;
            _residueSprites = null; // 폴더가 바뀌었으니 캐시를 버린다
        }

        // 논리 이름 → 지역별 실제 스프라이트.
        static Sprite ZoneArt(string logicalName) => Art(_artPrefix + logicalName);

        static Sprite Art(string spriteName)
        {
            if (_residueSprites == null)
            {
                _residueSprites = new Dictionary<string, Sprite>();
                foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { _artRoot }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (asset is Sprite sprite) _residueSprites[sprite.name] = sprite;
                }
            }

            return _residueSprites.TryGetValue(spriteName, out var found) ? found : null;
        }

        // "클립_00, 클립_01 …" 이름의 프레임을 순서대로 모은다.
        static Sprite[] ArtFrames(string clipName)
        {
            var frames = new List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                var frame = Art($"{clipName}_{i:00}");
                if (frame == null) break;
                frames.Add(frame);
            }
            return frames.ToArray();
        }

        // SpriteAnimator를 붙이고 클립을 채운다. 프레임이 하나도 없는 클립은 건너뛴다.
        static void AttachAnimator(GameObject target, SpriteRenderer renderer,
                                   (string clip, float fps, bool loop)[] definitions,
                                   float displayHeight = 0f)
        {
            var valid = new List<(string clip, float fps, bool loop, Sprite[] frames)>();
            foreach (var definition in definitions)
            {
                var frames = ArtFrames(definition.clip);
                if (frames.Length > 0) valid.Add((definition.clip, definition.fps, definition.loop, frames));
            }
            if (valid.Count == 0) return;

            var animator = target.GetComponent<SpriteAnimator>();
            if (animator == null) animator = target.AddComponent<SpriteAnimator>();

            var so = new SerializedObject(animator);
            so.FindProperty("target").objectReferenceValue = renderer;
            so.FindProperty("normalizedHeight").floatValue = displayHeight;

            var clipsProperty = so.FindProperty("clips");
            clipsProperty.arraySize = valid.Count;
            for (int i = 0; i < valid.Count; i++)
            {
                var element = clipsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = valid[i].clip;
                element.FindPropertyRelative("fps").floatValue = valid[i].fps;
                element.FindPropertyRelative("loop").boolValue = valid[i].loop;

                var framesProperty = element.FindPropertyRelative("frames");
                framesProperty.arraySize = valid[i].frames.Length;
                for (int f = 0; f < valid[i].frames.Length; f++)
                    framesProperty.GetArrayElementAtIndex(f).objectReferenceValue = valid[i].frames[f];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 적 종류별 애니메이션 정의(Gameplay/README.md의 권장 FPS).
        static (string, float, bool)[] EnemyClips(string prefix) => new[]
        {
            ($"{prefix}Idle",   6f,  true),
            ($"{prefix}Walk",   10f, true),
            ($"{prefix}Attack", 12f, false),
            ($"{prefix}Hit",    10f, false),
        };

        // 스프라이트를 방 좌표계의 폭·높이에 맞춰 늘린다. 시트 셀 크기가 제각각이라
        // (지형 209x313, 발판 256x341 …) 매번 원본 크기로 나눠 줘야 의도한 칸에 들어맞는다.
        static void FitSprite(SpriteRenderer renderer, float width, float height)
        {
            if (renderer.sprite == null) return;

            var size = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(
                size.x <= 0f ? 1f : width / size.x,
                size.y <= 0f ? 1f : height / size.y,
                1f);
        }

        // R07에 놓인 숏컷 C를 R10 보스 승리가 열어야 하므로 한 번 만든 것을 들고 있는다.
        static Shortcut _shortcutC;
        // R03의 숏컷 A/B는 R05·R08의 되감기 대상이 열어야 하므로 마찬가지로 들고 있는다.
        static Shortcut _shortcutA;
        static Shortcut _shortcutB;

        // 되감기 대상에 "복원되면 이 숏컷을 연다"를 연결한다. 오브젝트가 아니라 숏컷 id를 굽는다 —
        // 대상과 숏컷이 서로 다른 방 씬에 있어 씬을 넘는 참조는 저장되지 않기 때문이다.
        static void LinkRewindToShortcut(GameObject rewindable, string shortcutId, params GameObject[] siblings)
        {
            var component = rewindable.GetComponent<Rewindable>();
            if (component == null || string.IsNullOrEmpty(shortcutId)) return;

            SetField(component, "linkedShortcutId", p => p.stringValue = shortcutId);
            if (siblings == null || siblings.Length == 0) return;

            SetField(component, "requiredSiblings", p =>
            {
                p.arraySize = siblings.Length;
                for (int i = 0; i < siblings.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = siblings[i].GetComponent<Rewindable>();
            });
        }

        // 방 하나를 짓는 동안 쓰는 좌표 변환기. 방 로컬 좌표를 그대로 쓰게 해 준다.
        sealed class RoomCtx
        {
            public Tilemap Map;
            public GameObject Root;
            public Transform Rooms;
            public Vector2Int O;

            // 방마다 지형 결이 달라 보이도록 쓰는 아트 변형 번호(1~3). 같은 시트의 다른 열을
            // 골라 쓴다 — 방이 다 똑같아 보이면 랜드마크로 길을 기억할 수 없다(GAME_DESIGN.md).
            public int Variant = 1;

            // 지형 표면을 잔재 전용 아트로 덮을지. 응시·균열은 전용 아트가 아직 없어서 끄고,
            // 대신 타일맵 자체를 지역 색으로 물들여 구분한다(각 지역 빌더의 TintTerrain).
            // 잔재 아트를 그대로 씌우면 세 지역이 같은 폐허로 보인다.
            public bool FloorArt;

            // 바닥 슬래브를 몇 칸 두께로 채울지. 깊이는 눈에 보이는 두께여야 한다 —
            // 런타임 지형 아트(CameraLockedRoomBackground)는 밟는 면 아래 3유닛까지만
            // 옆면을 그리므로, 그보다 깊게 채운 부분은 아무 그림도 받지 못한 채
            // 충돌 타일맵의 회색 틴트만 남는다. 균열에서 실제로 그랬다: 밝은 대리석
            // 난간 아래로 8유닛짜리 잿빛 덩어리가 물 위에 깔려 있었고, 그것이
            // "바닥이 이상하다"의 정체였다. 지역 빌더가 이 값을 아트가 덮는 깊이로 맞춘다.
            public int Depth = 8;

            public void Floor(int x0, int x1, int top, int depth = 0)
            {
                if (depth <= 0) depth = Depth;
                ZoneSceneBuilder.Floor(Map, O.x + x0, O.x + x1, O.y + top, depth);
                // 충돌은 타일맵이 그대로 맡고, 보이는 표면만 잔재 지형 아트로 덮는다
                // (Environment/README.md: 지형 시트는 "충돌 Tilemap을 대체하지 않는 장식형 전경 모듈").
                if (FloorArt) PlaceFloorArt(Root.transform, P((x0 + x1) * 0.5f, top), x1 - x0, Variant);
            }

            public void Tiles(int x0, int x1, int y0, int y1)
                => PlaceTiles(Map, GroundTile(), O.x + x0, O.x + x1, O.y + y0, O.y + y1);

            public Vector2 P(float x, float y) => new Vector2(O.x + x, O.y + y);

            public GameObject Room(string name, float w, float h)
                => BuildRoom(Rooms, name, P(w * 0.5f, h * 0.5f), new Vector2(w, h));
        }

        // 고정 보상 상자. 지급 여부는 오브젝트가 아니라 ProgressState의 id로 기록되므로
        // 되감기로 상자를 되돌려 놓아도 두 번 주지 않는다.
        static RewardChest BuildRewardChest(Transform parent, string id, Vector2 pos, int currency, bool healthShard)
        {
            var go = new GameObject($"Reward_{id}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.layer = LayerMask.NameToLayer("Interactable");

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = Art(healthShard ? "Item_HealthShard" : "Item_Currency")
                ?? ZoneArt(healthShard ? "ItemToken_00" : "ItemShard_00")
                ?? LoadSprite("Fragment");
            sr.sortingOrder = 5;
            FitSprite(sr, 1f, 1f);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;

            var chest = go.AddComponent<RewardChest>();
            SetField(chest, "rewardId", p => p.stringValue = id);
            SetField(chest, "currency", p => p.intValue = currency);
            SetField(chest, "healthShard", p => p.boolValue = healthShard);
            SetField(chest, "visual", p => p.objectReferenceValue = visual);
            return chest;
        }

        // 조우. 방에 들어오면 관찰 시간 뒤에 잠기고, 단계별로 적을 활성화한다.
        // advanceWhenRemaining[k]는 waves[k+1]이 열리는 "이전 단계 잔존 수" 조건이다(-1이면 시간만).
        static Encounter BuildEncounter(Transform parent, string id, Vector2 center, Vector2 size, bool oneTime,
                                        GameObject[][] waves, int[] advanceWhenRemaining,
                                        RewardChest reward, Shortcut shortcut, string shortcutId = null)
        {
            var go = new GameObject($"Encounter_{id}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            // 전투 중에만 켜지는 잠금 벽 2개(좌우).
            var lockL = BuildSolidBlock(go.transform, "Lock_L",
                new Vector2(center.x - size.x * 0.5f, center.y), new Vector2(1f, size.y), "Wall");
            var lockR = BuildSolidBlock(go.transform, "Lock_R",
                new Vector2(center.x + size.x * 0.5f, center.y), new Vector2(1f, size.y), "Wall");
            lockL.SetActive(false);
            lockR.SetActive(false);

            var encounter = go.AddComponent<Encounter>();
            SetField(encounter, "encounterId", p => p.stringValue = id);
            SetField(encounter, "oneTime", p => p.boolValue = oneTime);
            SetField(encounter, "lockObjects", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = lockL;
                p.GetArrayElementAtIndex(1).objectReferenceValue = lockR;
            });
            SetField(encounter, "waves", p =>
            {
                p.arraySize = waves.Length;
                for (int i = 0; i < waves.Length; i++)
                {
                    var wave = p.GetArrayElementAtIndex(i);
                    var members = wave.FindPropertyRelative("members");
                    members.arraySize = waves[i].Length;
                    for (int j = 0; j < waves[i].Length; j++)
                        members.GetArrayElementAtIndex(j).objectReferenceValue = waves[i][j];

                    // 첫 단계는 즉시. 이후 단계는 "이전 단계 일부 처치" 또는 6초 경과로 열린다
                    // (명세 R09: "낙하 적 처치 또는 6초 경과 후 애도 운반자 진입").
                    wave.FindPropertyRelative("delaySeconds").floatValue = i == 0 ? 0f : 6f;
                    int advanceIndex = i - 1;
                    wave.FindPropertyRelative("advanceWhenRemaining").intValue =
                        i == 0 ? -1
                        : advanceIndex < advanceWhenRemaining.Length ? advanceWhenRemaining[advanceIndex]
                        : -1;
                }
            });
            if (reward != null) SetField(encounter, "victoryReward", p => p.objectReferenceValue = reward);
            if (shortcut != null) SetField(encounter, "victoryShortcut", p => p.objectReferenceValue = shortcut);
            if (!string.IsNullOrEmpty(shortcutId))
                SetField(encounter, "victoryShortcutId", p => p.stringValue = shortcutId);

            return encounter;
        }


        // 바닥 표면을 덮는 직선 지형 모듈. 피벗이 Bottom Center라 표면 y에 그대로 세운다.
        static void PlaceFloorArt(Transform parent, Vector2 surfaceCenter, float width, int variant = 1)
        {
            // 1행 = 직선 바닥. 열을 바꿔 방마다 결을 달리한다.
            var sprite = ZoneArt($"Terrain_r1_c{Mathf.Clamp(variant, 1, 3)}");
            if (sprite == null) return;

            var go = new GameObject("FloorArt");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(surfaceCenter.x, surfaceCenter.y - 1.5f, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1; // 타일맵(0) 바로 위, 플레이어(10) 아래
            renderer.drawMode = SpriteDrawMode.Simple;
            FitSprite(renderer, width, 2f);
        }

        // 생성된 V3 발판을 종횡비 그대로 배치한다. 피벗이 하단 중앙이므로 그림의 윗면을
        // 콜라이더 윗면에 맞추고, 가로·세로를 따로 늘리지 않는다.
        static void ApplyResiduePlatformV3(GameObject block, string spriteName, float worldWidth,
                                           int sortingOrder = 4)
        {
            var sprite = Art(spriteName);
            var collider = block != null ? block.GetComponent<BoxCollider2D>() : null;
            if (sprite == null || collider == null || sprite.bounds.size.x <= 0f) return;

            var old = block.transform.Find("ResiduePlatformV3");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var rootRenderer = block.GetComponent<SpriteRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            var art = new GameObject("ResiduePlatformV3");
            art.transform.SetParent(block.transform, false);
            float uniform = worldWidth / sprite.bounds.size.x;
            Vector3 parentScale = block.transform.lossyScale;
            art.transform.localScale = new Vector3(
                uniform / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                uniform / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);

            float worldBottom = collider.bounds.max.y - sprite.bounds.size.y * uniform;
            art.transform.position = new Vector3(collider.bounds.center.x, worldBottom,
                block.transform.position.z);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }

        static GameObject PlaceResidueStairV3(Transform parent, string name, Vector2 basePosition,
                                              float worldWidth, bool risesRight)
        {
            var sprite = Art(risesRight ? "ResidueStairUpRight" : "ResidueStairUpLeft");
            if (sprite == null || sprite.bounds.size.x <= 0f) return null;

            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(basePosition.x, basePosition.y, 0f);
            float scale = worldWidth / sprite.bounds.size.x;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;
            return go;
        }

        // BuildSolidBlock은 localScale로 충돌 크기를 낸다. 거기에 아트를 직접 얹으면 스프라이트가
        // 그 배율에 딸려 늘어나 버리므로, 스케일이 1인 자식에 그려 넣고 원본 렌더러는 끈다.
        static void ApplyArtOverlay(GameObject block, string spriteName, Vector2 worldSize, int sortingOrder)
        {
            var sprite = Art(spriteName);
            if (sprite == null) return;

            var blockRenderer = block.GetComponent<SpriteRenderer>();
            if (blockRenderer != null) blockRenderer.enabled = false;

            var art = new GameObject("Art");
            art.transform.SetParent(block.transform, false);
            // 부모가 이미 worldSize만큼 확대돼 있으니, 자식은 그 역수로 되돌린 뒤 다시 맞춘다.
            var scale = block.transform.localScale;
            art.transform.localScale = new Vector3(
                scale.x == 0f ? 1f : 1f / scale.x,
                scale.y == 0f ? 1f : 1f / scale.y, 1f);

            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var size = sprite.bounds.size;
            var fitted = art.transform.localScale;
            fitted.x *= size.x <= 0f ? 1f : worldSize.x / size.x;
            fitted.y *= size.y <= 0f ? 1f : worldSize.y / size.y;
            art.transform.localScale = fitted;
        }

        // 프리팹으로 만든 오브젝트의 겉모습만 잔재 아트로 갈아끼운다. 원본 렌더러는 끄고
        // 스케일 1인 자식에 그려 넣는다 — 프리팹의 localScale은 콜라이더 크기와 묶여 있어서
        // 여기서 건드리면 판정이 같이 바뀐다.
        static void ReplaceArt(GameObject target, string spriteName, Vector2 worldSize, int sortingOrder)
        {
            var sprite = Art(spriteName);
            if (sprite == null) return;

            foreach (var existing in target.GetComponentsInChildren<SpriteRenderer>())
                existing.enabled = false;

            var art = new GameObject("Art");
            art.transform.SetParent(target.transform, false);

            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var scale = target.transform.localScale;
            var size = sprite.bounds.size;
            art.transform.localScale = new Vector3(
                (scale.x == 0f ? 1f : 1f / scale.x) * (size.x <= 0f ? 1f : worldSize.x / size.x),
                (scale.y == 0f ? 1f : 1f / scale.y) * (size.y <= 0f ? 1f : worldSize.y / size.y),
                1f);

            // 그림 윗면을 밟는 면에 맞춘다(ApplyPlatformArt와 같은 이유).
            // 발판 시트의 피벗은 아래쪽인데 자식을 원점에 두면 그림이 판정보다 0.55 위로
            // 떠서, 플레이어가 발판 그림 속에 반쯤 파묻힌 채 서 있게 된다.
            var body = target.GetComponent<Collider2D>();
            if (body != null)
            {
                Physics2D.SyncTransforms();
                float delta = body.bounds.max.y - renderer.bounds.max.y;
                if (Mathf.Abs(delta) > 0.001f)
                    art.transform.position += new Vector3(0f, delta, 0f);
            }
        }

        // 충돌 없는 잔재 환경 장식. 피벗이 Bottom Center라 바닥 y에 그대로 세운다.
        static GameObject BuildResidueProp(Transform parent, string name, Vector2 basePos, Vector2 worldSize, string spriteName)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Art(spriteName) ?? LoadSprite("Tile");
            renderer.sortingOrder = -5; // 지형보다 뒤, 배경 중경보다 앞
            FitSprite(renderer, worldSize.x, worldSize.y);
            return go;
        }

        // 잔재 지역 전용 래퍼. 공용 빌더는 다른 지역도 쓰므로 여기서만 겉모습을 덮는다.
        static GameObject ResidueRewindable(Transform parent, Vector2 pos)
        {
            var go = BuildRewindableBlock(parent, pos);
            ReplaceArt(go, "Rewind_r1_c1", new Vector2(1.2f, 1.2f), 4); // 1행 = 파손 상태
            return go;
        }

        static GameObject ResidueCrumbling(Transform parent, Vector2 pos)
        {
            var go = BuildCrumblingPlatform(parent, pos);
            ReplaceArt(go, "Item_CrumbleIntact", new Vector2(3f, 1f), 2);
            AttachPlatformStates(go);
            return go;
        }

        // 발판 상태 4행(ResiduePlatformStates_v1: 균열 → 붕괴 → 파손 정착 → 되감기 복구).
        // ReplaceArt가 만들어 둔 Art 자식에 얹는다. 1행만 대기 루프이고 나머지는 상태 전환용이라
        // 마지막 프레임에서 멈춰야 한다(PROMPTS.md 납품 표).
        static void AttachPlatformStates(GameObject platform)
        {
            var art = platform.transform.Find("Art");
            if (art == null) return;

            var renderer = art.GetComponent<SpriteRenderer>();
            if (renderer == null) return;

            AttachAnimator(art.gameObject, renderer, new[]
            {
                ("PlatformCrack", 8f, true),
                ("PlatformCollapse", 12f, false),
                ("PlatformBroken", 8f, false),
                ("PlatformRestore", 14f, false),
                // 되살아날 때 되돌아갈 "멀쩡한 발판" 그림. 이게 없으면 OmenPlatform이
                // 부활하면서 Play("FracturePlatformSafe")를 불러도 아무 일도 일어나지
                // 않아, 무너진 마지막 프레임이 그대로 남는다.
                ("FracturePlatformSafe", 8f, true),
            }, 1f);

            // 밟기 전에는 멀쩡한 그림 그대로 있어야 한다. 자동 재생을 켜 두면 금 간 1행이
            // 처음부터 돌아가 발판이 늘 부서지기 직전처럼 보인다.
            var animator = art.GetComponent<SpriteAnimator>();
            if (animator != null) SetField(animator, "autoPlay", p => p.boolValue = false);
        }

        // 숏컷 장치의 봉쇄·해제 애니메이션(ResidueRoomTransitions_v1의 1·2행).
        static void AttachSealAnimator(Shortcut shortcut, Vector2 size)
        {
            if (shortcut == null) return;

            var sealObject = new GameObject("SealAnimation");
            sealObject.transform.SetParent(shortcut.transform, false);
            // ResidueRoomTransitions_v1 시트는 피벗이 하단(0.5, 0)이라 렌더러 위치가 곧 "그림의
            // 발밑"이 된다. shortcut 원점은 blocker/opened와 같은 통로 중심(수직 중앙)이므로,
            // 그대로 두면 문 그림이 중심에서 위로만 자라 실제 통로보다 절반(size.y/2)만큼
            // 위로 뜬 것처럼 보인다 — 아래로 그만큼 내려서 통로 중심에 수직으로 맞춘다.
            sealObject.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);

            var renderer = sealObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 6; // 숏컷 본체(5) 바로 위
            FitSprite(renderer, size.x, size.y);

            AttachAnimator(sealObject, renderer, new[]
            {
                ("SealClose", 10f, false),
                ("SealOpen", 10f, false),
            }, size.y);

            var animator = sealObject.GetComponent<SpriteAnimator>();
            if (animator == null) { Object.DestroyImmediate(sealObject); return; }

            SetField(animator, "autoPlay", p => p.boolValue = false);
            SetField(shortcut, "transitionAnimator", p => p.objectReferenceValue = animator);
        }

        // 충돌 연출 4종을 지역에 하나만 놓는다. 호출부는 ImpactVFX.Play로 자리와 종류만 넘긴다.
        // 잔재의 충돌 연출 목록. 지역마다 이름만 다르므로 목록을 인자로 받는다.
        static readonly (string name, string clip, float fps, float height)[] ResidueImpacts =
        {
            ("ImpactMelee", "ImpactMelee", 18f, 1.4f),
            ("ImpactWall", "ImpactWall", 16f, 1.6f),
            ("ImpactLand", "ImpactLand", 14f, 1.0f),
            ("ImpactHeavy", "ImpactHeavy", 16f, 1.8f),

            // 날아가지 않고 그 자리에서 터지는 것들도 같은 재생기로 처리한다.
            ("ProjChargeTrail", "ProjChargeTrail", 16f, 1.6f),  // 돌진 시작 잔상
            ("BossRing", "BossRing", 14f, 3.5f),         // 보스 낙하 착지 고리
            ("BossRupture", "BossRupture", 12f, 3.0f),   // 단계 전환 파열
        };

        // name은 런타임이 부르는 등록 이름, clip은 프레임을 꺼낼 시트 클립 이름이다.
        // 응시처럼 시트 이름에 지역 접두사가 붙은 경우에도 등록 이름은 런타임 호출명
        // ("ImpactMelee")과 일치해야 한다 — 어긋나면 그 연출은 조용히 재생되지 않는다.
        static void BuildImpactVFX(Transform parent,
            (string name, string clip, float fps, float height)[] definitions)
        {

            var valid = new List<(string name, float fps, float height, Sprite[] frames)>();
            foreach (var definition in definitions)
            {
                var frames = ArtFrames(definition.clip);
                if (frames.Length > 0) valid.Add((definition.name, definition.fps, definition.height, frames));
            }
            if (valid.Count == 0) return;

            var go = new GameObject("ImpactVFX");
            go.transform.SetParent(parent, false);
            var vfx = go.AddComponent<ImpactVFX>();

            SetField(vfx, "effects", p =>
            {
                p.arraySize = valid.Count;
                for (int i = 0; i < valid.Count; i++)
                {
                    var element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("name").stringValue = valid[i].name;
                    element.FindPropertyRelative("fps").floatValue = valid[i].fps;
                    element.FindPropertyRelative("displayHeight").floatValue = valid[i].height;

                    var frames = element.FindPropertyRelative("frames");
                    frames.arraySize = valid[i].frames.Length;
                    for (int f = 0; f < valid[i].frames.Length; f++)
                        frames.GetArrayElementAtIndex(f).objectReferenceValue = valid[i].frames[f];
                }
            });
        }

        // 발판(안전·회전·승강기)에 지역 발판 아트를 입힌다. 반드시 스케일 1인 자식에
        // 그린다 — 루트 localScale로 크기를 맞추면 BoxCollider2D가 같은 배율로 줄어,
        // 3x0.5짜리 발판 판정이 0.56x0.05 슬리버가 된다(실제로 안전 발판 15개가 그랬다.
        // 봇은 바닥 위주로 다녀서 못 잡았고, 사람이 밟으면 그냥 통과 낙하한다).
        static void ApplyPlatformArt(GameObject platform, Color? tint = null)
        {
            var sprite = ZoneArt("Platform_r1_c1");
            if (sprite == null) return;

            var rootRenderer = platform.GetComponent<SpriteRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            var art = new GameObject("Art");
            art.transform.SetParent(platform.transform, false);

            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;
            if (tint.HasValue) renderer.color = tint.Value;
            FitSprite(renderer, 3f, 0.8f);

            // 그림 윗면을 **밟는 면**에 맞춘다.
            //
            // 발판 판정은 3x0.5인데 그림은 3x0.8이고, 지역 발판 시트의 피벗은 아래쪽이다.
            // 자식을 원점에 그대로 두면 그림이 판정보다 0.55 위로 떠서, 플레이어가 발판
            // **그림 속에 반쯤 파묻힌 채** 서 있게 된다 — 균열 발판 44개가 전부 그랬다
            // (FractureStructureAuditTool). 피벗이 아래든 가운데든 상관없이 맞도록
            // 실제 경계로 계산한다.
            var body = platform.GetComponent<Collider2D>();
            if (body != null)
            {
                Physics2D.SyncTransforms();
                float delta = body.bounds.max.y - renderer.bounds.max.y;
                if (Mathf.Abs(delta) > 0.001f)
                    art.transform.position += new Vector3(0f, delta, 0f);
            }
        }

        // 회색 블록(벽·천장)에 지역 지형 셀을 이어 붙인다. 블록 하나에 셀 하나를 늘리면
        // 긴 벽에서 그림이 죽처럼 번지므로, 정사각형에 가까운 조각을 긴 축 방향으로 쌓는다.
        // 지역 시트가 없으면(플레이스홀더 단계) 아무것도 하지 않는다 — 회색 블록이 그대로
        // 남아 눈에 띈다.
        //
        // spriteName은 기본이 지형 시트 1행 1열, 즉 **바닥 윗면** 셀이다. 가로로 긴 천장에는
        // 맞지만 세로로 긴 벽에 쓰면 난간과 고드름이 세로로 여덟 번 되풀이돼 파랑·흰색
        // 체크무늬 막대가 된다(F12 전장 벽 두 개가 그랬다). 세로 구조물은 벽 켜 셀을 넘긴다.
        static void ApplyBlockArt(GameObject block, Vector2 worldSize, int sortingOrder = 1,
                                  string spriteName = "Terrain_r1_c1")
        {
            var sprite = ZoneArt(spriteName);
            if (sprite == null) return;

            var blockRenderer = block.GetComponent<SpriteRenderer>();
            if (blockRenderer != null) blockRenderer.enabled = false;

            // BuildSolidBlock은 localScale로 크기를 내므로 자식에서 역보정한다(ApplyArtOverlay와
            // 같은 이유 — 스케일 1인 자식에 그려야 그림이 딸려 늘어나지 않는다).
            var scale = block.transform.localScale;
            var art = new GameObject("Art");
            art.transform.SetParent(block.transform, false);
            art.transform.localScale = new Vector3(
                scale.x == 0f ? 1f : 1f / scale.x,
                scale.y == 0f ? 1f : 1f / scale.y, 1f);

            bool vertical = worldSize.y >= worldSize.x;
            float longSide = vertical ? worldSize.y : worldSize.x;
            float shortSide = vertical ? worldSize.x : worldSize.y;
            // 조각 하나는 짧은 변 크기의 정사각형에 가깝게. 너무 잘게 쪼개지 않도록 상한을 둔다.
            int count = Mathf.Clamp(Mathf.RoundToInt(longSide / Mathf.Max(shortSide, 0.5f)), 1, 16);
            float step = longSide / count;

            for (int i = 0; i < count; i++)
            {
                var piece = new GameObject($"Piece_{i}");
                piece.transform.SetParent(art.transform, false);
                float offset = -longSide * 0.5f + step * (i + 0.5f);
                piece.transform.localPosition = vertical
                    ? new Vector3(0f, offset, 0f)
                    : new Vector3(offset, 0f, 0f);

                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
                FitSprite(renderer,
                    vertical ? worldSize.x : step,
                    vertical ? step : worldSize.y);
            }
        }

        // 방과 방 사이의 평평한 연결 통로. 양쪽 높이를 맞춰 뒀으므로 바닥 한 줄이면 이어진다.
        // 천장을 두어 통로가 "길"로 읽히게 하고, 위로 빠져나가 엉뚱한 방 지붕에 올라서는 것도 막는다.
        //
        // 잔재는 이제 복도 대신 포탈 문을 쓴다. 응시·균열이 아직 한 씬 구조라 남겨 둔 것이고,
        // 두 지역도 방 씬으로 옮기면 이 함수와 BuildShaft는 함께 사라진다.
        static void BuildCorridor(Transform parent, Tilemap map, string name,
                                  float fromX, float toX, int surfaceY)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(fromX, toX));
            int x1 = Mathf.CeilToInt(Mathf.Max(fromX, toX));
            Floor(map, x0, x1, surfaceY);
            var ceiling = BuildSolidBlock(parent, name + "_Ceiling", new Vector2((x0 + x1) * 0.5f, surfaceY + 5.5f),
                new Vector2(x1 - x0, 1f), "Ground");
            ApplyBlockArt(ceiling, new Vector2(x1 - x0, 1f));
        }

        // 비밀방으로 내려가는 수직 통로. 양쪽에 벽점프용 벽을 세워 다시 올라올 수 있게 한다
        // (명세: 비밀방은 "동일 경로 복귀"). 잔재에서는 쓰지 않는다 — BuildCorridor 주석 참고.
        static void BuildShaft(Transform parent, Tilemap map, string name,
                               int centerX, int topY, int bottomY, int width = 4)
        {
            int half = width / 2;
            var left = BuildSolidBlock(parent, name + "_L", new Vector2(centerX - half - 0.5f, (topY + bottomY) * 0.5f),
                new Vector2(1f, topY - bottomY), "Wall");
            var right = BuildSolidBlock(parent, name + "_R", new Vector2(centerX + half + 0.5f, (topY + bottomY) * 0.5f),
                new Vector2(1f, topY - bottomY), "Wall");
            ApplyBlockArt(left, new Vector2(1f, topY - bottomY));
            ApplyBlockArt(right, new Vector2(1f, topY - bottomY));
        }

        // 날아가는 공격체. 지역에 하나만 놓고 적·보스가 이름으로 쏜다.
        // 수치는 명세의 역할 그대로다 — 파편은 빠르고 작게, 충격파는 느리지만 지형에 막히지
        // 않고 바닥을 훑는다(정면 방어를 피해 뒤로 돌아간 플레이어를 밀어내는 것이 목적).
        static readonly (string name, float fps, float speed, float lifetime,
                         float radius, int damage, float height, bool ignoreTerrain)[] ResidueProjectiles =
        {
            ("ProjSplinter",   14f, 8f,   2.0f, 0.4f, 1, 0.8f, false),
            ("ProjClaw",       16f, 9f,   0.7f, 0.6f, 1, 1.2f, false),
            ("ProjShockwave",  14f, 5f,   2.2f, 0.7f, 1, 1.0f, true),
            ("BossWave",       14f, 6f,   3.0f, 0.9f, 1, 2.0f, true),
            ("BossNeedle",     16f, 11f,  2.0f, 0.4f, 1, 1.0f, false),
            ("BossRewindOrb",  14f, 4.5f, 3.5f, 0.8f, 1, 1.4f, false),
        };

        static void BuildProjectileSpawner(Transform parent,
            (string name, float fps, float speed, float lifetime,
             float radius, int damage, float height, bool ignoreTerrain)[] definitions)
        {

            var valid = new List<(string name, float fps, float speed, float lifetime,
                                  float radius, int damage, float height, bool ignoreTerrain, Sprite[] frames)>();
            foreach (var d in definitions)
            {
                var frames = ArtFrames(d.name);
                if (frames.Length > 0)
                    valid.Add((d.name, d.fps, d.speed, d.lifetime, d.radius, d.damage, d.height,
                               d.ignoreTerrain, frames));
            }
            if (valid.Count == 0) return;

            var go = new GameObject("ProjectileSpawner");
            go.transform.SetParent(parent, false);
            var spawner = go.AddComponent<ProjectileSpawner>();

            SetField(spawner, "definitions", p =>
            {
                p.arraySize = valid.Count;
                for (int i = 0; i < valid.Count; i++)
                {
                    var element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("name").stringValue = valid[i].name;
                    element.FindPropertyRelative("fps").floatValue = valid[i].fps;
                    element.FindPropertyRelative("speed").floatValue = valid[i].speed;
                    element.FindPropertyRelative("lifetime").floatValue = valid[i].lifetime;
                    element.FindPropertyRelative("radius").floatValue = valid[i].radius;
                    element.FindPropertyRelative("damage").intValue = valid[i].damage;
                    element.FindPropertyRelative("displayHeight").floatValue = valid[i].height;
                    element.FindPropertyRelative("ignoreTerrain").boolValue = valid[i].ignoreTerrain;

                    var frames = element.FindPropertyRelative("frames");
                    frames.arraySize = valid[i].frames.Length;
                    for (int f = 0; f < valid[i].frames.Length; f++)
                        frames.GetArrayElementAtIndex(f).objectReferenceValue = valid[i].frames[f];
                }
            });
        }

        // 방마다 전경·배경 모션을 한 겹씩 얹는다. 방 순서로 골라 이웃한 방이 같은 움직임을
        // 반복하지 않게 한다 — 전부 같으면 랜드마크로 길을 기억할 수 없다(GAME_DESIGN.md).
        static readonly (string clip, float fps)[] BackgroundMotions =
        {
            ("BgSmoke", 5f), ("BgWindows", 5f), ("BgHand", 3f), ("BgCrowd", 4f),
        };

        static readonly (string clip, float fps)[] ForegroundMotions =
        {
            ("FgChains", 6f), ("FgCage", 6f), ("FgFinger", 4f), ("FgDust", 6f),
        };

        static void BuildRoomMotion(Room room, int index,
                                    (string clip, float fps)[] BackgroundMotions,
                                    (string clip, float fps)[] ForegroundMotions)
        {
            var bounds = room.WorldBounds;

            var background = BackgroundMotions[index % BackgroundMotions.Length];
            var motionBack = BuildMotionLayer(room.transform, "MotionBack", background.clip, background.fps,
                -25, 0.2f, bounds.center + new Vector3(0f, bounds.extents.y * 0.35f, 0f),
                bounds.size.y * 0.55f);
            // 배경과 같이 방 밖에서는 꺼지고, 방 진입 시 패럴랙스가 재기준된다.
            if (motionBack != null) motionBack.AddComponent<RoomVisualCuller>();

            // 전경 모션을 쓰지 않는 지역도 있다(균열). 시트 내용이 방 중앙을 가로막는 형태라
            // "중앙 65%를 비운다"는 전경 규칙과 충돌하기 때문이다 — 그 경우 null을 넘긴다.
            if (ForegroundMotions == null || ForegroundMotions.Length == 0) return;

            var foreground = ForegroundMotions[index % ForegroundMotions.Length];
            // 전경은 시차를 주지 않는다. 카메라와 함께 움직이면 화면 앞을 가리는 느낌이 사라진다.
            BuildMotionLayer(room.transform, "MotionFront", foreground.clip, foreground.fps,
                25, 0f, bounds.center - new Vector3(0f, bounds.extents.y * 0.25f, 0f),
                bounds.size.y * 0.45f);
        }

        static GameObject BuildMotionLayer(Transform parent, string name, string clip, float fps,
                                     int sortingOrder, float parallax, Vector3 position, float height)
        {
            var frames = ArtFrames(clip);
            if (frames.Length == 0) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = sortingOrder;
            // 배경은 어둡게 깔아 게임플레이 가독성을 해치지 않는다(명세 공통 규칙).
            renderer.color = sortingOrder < 0
                ? new Color(1f, 1f, 1f, 0.55f)
                : new Color(1f, 1f, 1f, 0.7f);

            AttachAnimator(go, renderer, new[] { (clip, fps, true) }, height);

            if (parallax > 0f)
            {
                var layer = go.AddComponent<ParallaxLayer>();
                SetField(layer, "multiplier", p => p.floatValue = parallax);
            }
            return go;
        }

        // 방 15개를 각자 씬으로 굽고, 마지막에 플레이어·카메라만 든 셸 씬을 만든다.
        // 방 순서는 예전 한 씬 빌더와 같다 — 숏컷 A/B(R03) · C(R07)를 만든 방이 그것을 쓰는
        // 방(R05·R08·R10)보다 먼저 돌아야 하기 때문이다.
        [MenuItem("Hidden Weight/Build Residue Zone (Rooms)")]
        public static void BuildResidueRooms()
        {
            EnsureScenesFolder();
            // 시트 분할을 씬 생성에 묶는다(RunGazeZone과 같은 이유). 기억의 교관 시트처럼
            // 새로 추가된 항목도 여기서 함께 잘린다.
            ResidueArtSlicer.SliceAll();

            UseArtRoot("Assets/Art/Residue");

            var builders = new (string room, System.Action<RoomCtx> build)[]
            {
                ("R01", BuildR01), ("R02", BuildR02), ("R03", BuildR03), ("R04", BuildR04),
                ("S1",  BuildS1),  ("R05", BuildR05), ("R06", BuildR06), ("S2",  BuildS2),
                ("R07", BuildR07), ("R08", BuildR08), ("R09", BuildR09), ("R10", BuildR10),
                ("R11", BuildR11), ("S3",  BuildS3),  ("R12", BuildR12),
            };

            foreach (var (room, build) in builders)
            {
                var scene = NewScene();
                var ctx = NewRoomCtx();
                build(ctx);

                BuildRoomStart(ctx);
                BuildDoorsFor(ctx, room);
                FinishRoomScene(ctx);
                SaveScene(scene, "Room_Residue_" + room);
            }

            BuildResidueShell();
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ResidueZoneBuilder] 잔재 방 씬 {builders.Length}개와 셸 생성 완료");
        }

        // 방 씬 하나의 빈 골격. 타일맵(바닥)은 반드시 그린다 — 4K 배경은 카메라 고정 벽지라
        // 실제 바닥 위치를 표현하지 못하고, 꺼 두면 바닥 전체가 보이지 않는 충돌이 된다.
        static RoomCtx NewRoomCtx()
        {
            var tilemap = BuildZoneRoot("Residue", out var root);

            // 방 씬은 셸 위에 additive로 얹힌다. GameManager(ScreenFader·AudioManager 동거)는
            // 셸이 하나만 들고 있어야 하므로 공용 킷이 넣어 준 사본을 걷어낸다. 그냥 두면 방을
            // 옮길 때마다 Awake에서 자기 자신을 파괴하는 중복 인스턴스가 하나씩 생긴다.
            var duplicate = Object.FindFirstObjectByType<HiddenWeight.Core.GameManager>();
            if (duplicate != null) Object.DestroyImmediate(duplicate.gameObject);

            var rooms = new GameObject("Rooms");
            rooms.transform.SetParent(root.transform, true);

            return new RoomCtx
            {
                Map = tilemap,
                Root = root,
                Rooms = rooms.transform,
                O = Vector2Int.zero,
                FloorArt = false,
            };
        }

        // 방 씬 하나를 마무리한다. 예전에는 지역 씬 끝에서 한 번만 하던 일들인데,
        // 방이 씬으로 갈라졌으므로 방마다 해야 한다.
        static void FinishRoomScene(RoomCtx c)
        {
            // 충돌 연출과 공격체 발사대는 씬마다 하나씩 필요하다 —
            // 다른 씬에 있는 인스턴스는 이 방의 적이 쓸 수 없다.
            BuildImpactVFX(c.Root.transform, ResidueImpacts);
            BuildProjectileSpawner(c.Root.transform, ResidueProjectiles);

            // 숏컷은 방 빌드 함수가 만들어 정적 필드에 남긴다. 그 방이 만든 것만 붙이고 비운다 —
            // 비우지 않으면 다음 방이 이미 닫힌 씬의 죽은 참조에 애니메이터를 또 붙이려 든다.
            if (_shortcutA != null) AttachSealAnimator(_shortcutA, new Vector2(4f, 2f));
            if (_shortcutB != null) AttachSealAnimator(_shortcutB, new Vector2(3f, 2.5f));
            if (_shortcutC != null) AttachSealAnimator(_shortcutC, new Vector2(4f, 2f));
            _shortcutA = _shortcutB = _shortcutC = null;

            // 방마다 원본 콘셉트 한 장을 카메라에 고정한다.
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Residue");

            ClotheCollisionPlaceholderRenderers(c.Root);
        }

        // 링크 테이블을 훑어 이 방에 속한 문을 전부 세운다. 링크 하나가 양쪽에 문을
        // 하나씩 만들기 때문에, 한쪽만 만들어 못 돌아오는 연결이 생길 수 없다.
        static void BuildDoorsFor(RoomCtx c, string room)
        {
            foreach (var link in ResidueRoomLinks.Links)
            {
                if (link.fromRoom == room)
                    SpawnDoor(c, link.FromDoorId, link.fromSide, link.fromAnchor, link.toRoom, link.ToDoorId,
                        link.requiredShortcutId);

                if (link.toRoom == room)
                    SpawnDoor(c, link.ToDoorId, link.toSide, link.toAnchor, link.fromRoom, link.FromDoorId,
                        link.requiredShortcutId);
            }
        }

        static void SpawnDoor(RoomCtx c, string doorId, Side side, Vector2 anchor,
            string targetRoom, string targetDoorId, string requiredShortcutId = null)
        {
            var go = new GameObject("Door_" + doorId.Replace(':', '_'));
            go.transform.SetParent(c.Root.transform, false);
            go.transform.position = new Vector3(anchor.x, anchor.y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            // 위아래 문은 넓고 얕게, 좌우 문은 좁고 높게. 지나가는 축으로는 얇아야
            // 문 위를 걷다가 의도치 않게 빨려 들어가지 않는다.
            col.size = side == Side.U || side == Side.D
                ? new Vector2(3f, 1.2f)
                : new Vector2(1.2f, 3.5f);

            var door = go.AddComponent<RoomDoor>();
            door.Configure(doorId, side, targetRoom, targetDoorId, RoomDoor.DefaultArrivalOffset(side),
                requiredShortcutId);
        }

        // 문을 거치지 않고 이 방에 들어올 때(지역 첫 진입·테스트) 플레이어가 서는 자리.
        // 방마다 바닥 표면이 y=1~3으로 달라서, 어느 방에서도 지형에 파묻히지 않는 y=4에 둔다.
        static void BuildRoomStart(RoomCtx c)
        {
            var go = new GameObject("RoomStart");
            go.transform.SetParent(c.Root.transform, false);
            go.transform.position = new Vector3(3f, 4f, 0f);
            go.AddComponent<RoomStart>();
        }

        // 셸에는 지형이 없다. 플레이어·카메라·HUD·RoomLoader만 두고 R01을 로드시킨다.
        static void BuildResidueShell()
        {
            var scene = NewScene();
            var root = new GameObject("Zone_Residue");

            // 전역 싱글턴은 셸이 소유한다. 셸에 GameManager가 없으면 같은 씬의 Player가
            // GameManager.Instance보다 먼저 Awake해 NullReference가 쏟아진다(GameManager.cs 주석).
            Spawn("GameManager", Vector3.zero);

            // 씬이 어느 지역인지 직접 선언한다. GameManager가 이 표식으로 ZoneData를 고른다.
            var marker = new GameObject("ZoneMarker");
            marker.transform.SetParent(root.transform, false);
            var zoneMarker = marker.AddComponent<HiddenWeight.Core.ZoneMarker>();
            SetField(zoneMarker, "zone", p => p.enumValueIndex = (int)ZoneId.Residue);

            // 잔재 환경음은 방이 아니라 지역에 속한다. 셸에 두어야 방을 옮길 때마다 끊겼다
            // 다시 시작하지 않는다(예전에는 Full 씬 전용 런타임 보정이 붙여 주던 것이다).
            root.AddComponent<ResidueAmbientAudio>();

            // 플레이어 시작 좌표는 의미가 없다. 진입점이 R01을 로드하면서 RoomStart로 옮긴다.
            PlacePlayerAndCamera(root, new Vector3(3f, 4f, 0f));

            // RoomLoader는 씬 루트로 남긴다. Zone 루트의 자식으로 붙이면 방 씬을 언로드할 때
            // 함께 사라질 위험이 있고, 싱글턴이 중간에 죽으면 전환이 통째로 멈춘다.
            var loaderGo = new GameObject("RoomLoader");
            loaderGo.AddComponent<RoomLoader>();
            loaderGo.AddComponent<ResidueEntryPoint>();

            SaveScene(scene, "Zone_Residue");
        }

        // ---------------- R01 입구 경계 (D0) ----------------
        // 사망 요소도 이동을 막는 요소도 없다. 캐릭터 크기와 배경 규모를 비교하는 방.
        static void BuildR01(RoomCtx c)
        {
            c.Variant = 1;
            c.Floor(0, 26, 2);

            BuildCheckpoint(c.Root.transform, c.P(5f, 3f));

            BuildCurrencyPickup(c.Root.transform, c.P(15f, 3.2f));
            BuildCurrencyPickup(c.Root.transform, c.P(16.5f, 3.2f));
            BuildCurrencyPickup(c.Root.transform, c.P(18f, 3.2f));

            c.Room("Room01", 26f, 14f);
            BuildBoundary(c.Root.transform, "Zone_WestBoundary", c.P(-0.5f, 0f).x);
        }

        // ---------------- R02 애도교 (D1→D2) ----------------
        // "실패해도 다른 길이 된다"를 가르친다. 상부 다리는 보상, 하부 통로는 안전한 우회.
        static void BuildR02(RoomCtx c)
        {
            c.Variant = 1;
            c.Floor(0, 8, 2);
            // 하부 통로. 상부 다리(x 8~14.5) 아래를 지나 "다리 오른쪽 바깥"인 x=17에서 올라온다.
            // 다리 밑에서 올라오게 두면 점프한 머리가 다리 밑면(y=3.5)에 막혀 영영 못 나온다 —
            // 봇이 실제로 그 자리(로컬 14.6)에서 무한 반복했다.
            c.Floor(8, 17, 1);
            c.Floor(17, 20, 3);  // 두 번째 발판
            c.Floor(20, 22, 1);  // 두 번째 발판과 출구 사이 틈 — 실패해도 하부로 떨어질 뿐이다
            c.Floor(22, 28, 3);  // 출구 (28,3)

            // 끊어진 상부 다리. 조각 사이 2유닛 = 표준 점프. 놓쳐도 하부로 떨어질 뿐이다.
            var bridgeA = BuildSolidBlock(c.Root.transform, "R02_Bridge_A", c.P(9.25f, 3.75f), new Vector2(2.5f, 0.5f), "Ground");
            var bridgeB = BuildSolidBlock(c.Root.transform, "R02_Bridge_B", c.P(13.5f, 3.75f), new Vector2(2f, 0.5f), "Ground");
            ApplyResiduePlatformV3(bridgeA, "ResiduePlatformShort", 2.8f);
            ApplyResiduePlatformV3(bridgeB, "ResiduePlatformShort", 2.3f);

            // 두 적은 12유닛 떨어뜨린다 — 동시에 감지되지 않게(명세 10유닛 이상).
            BuildResidueEnemy(c.Root.transform, c.P(6f, 3f), ResidueEnemyKind.Walker);
            BuildResidueEnemy(c.Root.transform, c.P(18f, 4f), ResidueEnemyKind.Walker);

            for (int i = 0; i < 5; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(12.9f + i * 0.5f, 5f));
            BuildHealingPickup(c.Root.transform, c.P(26f, 4f));

            c.Room("Room02", 28f, 14f);
        }

        // ---------------- R03 손바닥 광장 (D1) ----------------
        // 지역의 중앙 허브. 첫 방문에는 남동쪽 낮은 길만 열려 있고, 숏컷 A·B는 보이지만 못 지나간다.
        static void BuildR03(RoomCtx c)
        {
            c.Variant = 2;
            c.Floor(0, 20, 2);   // 서쪽 입구 + 중앙 손바닥 광장
            c.Floor(20, 30, 1);  // 남동쪽 R04로 가는 "가장 낮은 길"

            // 낮은 길을 재화 줄로 강조한다. 텍스트 없이 다음 목표를 알려주는 유도선.
            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(21f + i * 2f, 2.2f));

            BuildResidueEnemy(c.Root.transform, c.P(17f, 3f), ResidueEnemyKind.Walker);

            BuildResidueProp(c.Root.transform, "R03_PalmLandmark", c.P(12f, 3f), new Vector2(7f, 5f), "Prop_r2_c3");

            _shortcutA = BuildShortcut(c.Root.transform, "residue_shortcut_a", c.P(6f, 8f), new Vector2(4f, 0.6f),
                new Color(0.75f, 0.68f, 0.45f));
            _shortcutB = BuildShortcut(c.Root.transform, "residue_shortcut_b", c.P(23f, 5f), new Vector2(3f, 0.6f),
                new Color(0.5f, 0.65f, 0.85f));

            c.Room("Room03", 30f, 18f);
        }

        // ---------------- R04 매몰된 하층 폐허 (D2) ----------------
        // 수직 하강. 모든 상층 추락은 하층 안전 바닥으로 이어지고 피해가 없다.
        static void BuildR04(RoomCtx c)
        {
            c.Variant = 3;
            c.Floor(0, 24, 2);    // 하층 안전 바닥
            c.Floor(0, 6, 18);    // 상층 관찰대 (입구 2,20)

            // 중앙 지그재그 하강 발판. 높이차 2, 수평차 2.5 — 명세 상한 안쪽.
            var zig = new[] { (7f, 16f), (9.5f, 14f), (7f, 12f), (9.5f, 10f), (7f, 8f) };
            for (int i = 0; i < zig.Length; i++)
            {
                // 첫 무너지는 발판은 중간에 하나만 둔다. 밟지 않아도 통과할 수 있어야 한다.
                if (i == 2) ResidueCrumbling(c.Root.transform, c.P(zig[i].Item1, zig[i].Item2));
                else BuildSafePlatform(c.Root.transform, c.P(zig[i].Item1, zig[i].Item2));
            }

            // S1으로 이어지는 부서진 바닥(x=6~9, y=6). 먼지가 떨어지는 자리를 연출로 표시한다.
            BuildDecor(c.Root.transform, "R04_S1_Hint", c.P(7.5f, 6f), new Vector2(3f, 0.4f),
                "Tile", new Color(0.3f, 0.28f, 0.34f));
            BuildSafePlatform(c.Root.transform, c.P(7.5f, 6f));

            // 비대칭 벽 옆 마지막 구간은 통과 경로다. 충돌 없는 벽 그림이 남지 않도록
            // 오른쪽 벽은 만들지 않고 왼쪽 등반 벽만 유지한다.
            BuildResidueWall(c.Root.transform, "R04_Chimney_L", c.P(18f, 8.5f), new Vector2(1f, 7f));

            // 명세: "중층 폭 5유닛 발판, 플레이어가 위에서 먼저 관찰". 폭 3 발판 위에 두면
            // 돌아설 자리가 없어 제자리에서 떠는 것처럼 보인다.
            // 하강용 지그재그 발판(폭 3)과 겹치지 않는 자리에 깐다. 겹치면 적이 발판 위에
            // 올라서서 돌아설 폭이 없어진다.
            c.Tiles(12, 18, 13, 14);
            BuildResidueEnemy(c.Root.transform, c.P(15f, 15f), ResidueEnemyKind.Walker);
            // 두 번째 적은 S1을 확인하러 왼쪽으로 꺾을 때만 만난다. 동쪽 주 출구를 향하는
            // 플레이어의 60~90초 예산을 붙잡지 않는다.
            BuildResidueEnemy(c.Root.transform, c.P(4f, 3f), ResidueEnemyKind.Walker);

            c.Room("Room04", 24f, 22f);
        }

        // ---------------- S1 납골당 (D3 선택) ----------------
        // 짧은 정밀 이동 퍼즐. 즉사도 체력 피해도 쓰지 않는다.
        static void BuildS1(RoomCtx c)
        {
            c.Variant = 3;
            c.Floor(0, 18, 1);   // 실패 시 떨어지는 안전 바닥

            // 폭 1.5 발판 3개를 2.5 / 3 / 3.5 간격으로. 마지막만 무너진다.
            BuildSafePlatform(c.Root.transform, c.P(4f, 6f));
            BuildSafePlatform(c.Root.transform, c.P(8.5f, 7f));
            BuildSafePlatform(c.Root.transform, c.P(13.5f, 8f));
            ResidueCrumbling(c.Root.transform, c.P(16f, 9f));

            BuildStoryFragment(c.Root.transform, c.P(16f, 10.5f), "residue_s1",
                "철망 안에 같은 형상이 반복되어 있다.", EmotionId.None, false);
            for (int i = 0; i < 6; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(15f + i * 0.4f, 10f));

            c.Room("Secret01", 18f, 14f);
        }

        // ---------------- R05 되감기 성소 (D0→D2) ----------------
        // 되감기 획득과 1회 필수·1회 선택 튜토리얼. 실패·적·시간제한이 없다.
        static void BuildR05(RoomCtx c)
        {
            c.Variant = 2;
            c.Floor(0, 12, 2);
            // 1단계 턱은 실측 기본 점프 높이(2.72)보다 높은 3유닛이다. 복원 블록의 상단과
            // 턱 표면을 y=5로 정확히 맞춰, 되감기는 필수지만 모서리 걸림은 없게 한다.
            c.Floor(12, 16, 5);
            // 필수 복원 뒤에는 아래 안전 통로로 곧장 출구까지 갈 수 있다. 위쪽 다리 복원은
            // 같은 입력을 한 번 더 연습하고 재화를 얻는 선택 가지다.
            c.Floor(16, 26, 2);
            c.Floor(23, 26, 4);

            BuildCheckpoint(c.Root.transform, c.P(7f, 3f)); // 체크포인트 2 — 능력 획득 직전

            BuildStoryFragment(c.Root.transform, c.P(8.5f, 4f), "residue_skill",
                "무너진 구조물을 복원한다.", EmotionId.Rewind, false);
            BuildTutorialHint(c.Root.transform, c.P(11f, 6f),
                "K를 계속 누르면 구조물이 복원됩니다.");

            // 대상 1: 바닥(y=2)과 턱(y=5) 사이의 실제 디딤 높이(y=3.5)에 복원된다.
            // 블록 상단 y=4를 밟은 뒤 1유닛 더 올라가므로 밑면에 머리가 걸리지 않는다.
            var primary = ResidueRewindable(c.Root.transform, c.P(10.5f, 3.5f));
            primary.name = "R05_PrimaryRestore";
            // 숏컷 A는 R03 씬에 있다. 오브젝트 참조 대신 id를 구워, 되감는 순간 진행 상태에
            // 열림을 남긴다. R03에 다시 들어서면 사슬다리가 이미 내려와 있다.
            LinkRewindToShortcut(primary, "residue_shortcut_a");

            // 선택 대상: 끊어진 다리 조각. 복원하면 위쪽 재화 가지를 편하게 건넌다.
            ResidueRewindable(c.Root.transform, c.P(20f, 6.5f));
            // 재화를 좁은 직선으로 겹치면 충돌 없는 바닥처럼 보인다. 성긴 호로 띄워
            // 선택 발판을 따라가는 보상임을 명확히 한다.
            BuildCurrencyPickup(c.Root.transform, c.P(18.3f, 8.1f));
            BuildCurrencyPickup(c.Root.transform, c.P(19.8f, 8.7f));
            BuildCurrencyPickup(c.Root.transform, c.P(21.3f, 8.7f));
            BuildCurrencyPickup(c.Root.transform, c.P(22.8f, 8.1f));

            // 사슬은 선택 연출 대상으로 남긴다. 숏컷은 첫 필수 복원에서 이미 열린다.
            var chain = ResidueRewindable(c.Root.transform, c.P(22f, 7f));
            chain.name = "R05_ChainDevice";

            c.Room("Room05", 26f, 14f);
        }

        // ---------------- R06 손가락 내부 (D2→D3) ----------------
        // 되감기와 이동을 결합한다. 매복 적(매달린 손가락)을 처음 소개하는 방.
        static void BuildR06(RoomCtx c)
        {
            c.Variant = 3;
            c.Floor(0, 8, 2);
            // 4유닛 상승. 바닥에 닿는 복원 계단을 발판으로 써야만 오른다(기본 점프 2.72).
            c.Floor(8, 20, 6);
            c.Floor(20, 32, 5);   // 출구 (32,5)

            // 필수 대상. 낮은 바닥에 닿아 있고 상단 y=4가 두 바닥(y=2, y=6)의 중간이다.
            // 부유 블록 아래에 끼는 틈 없이 기본 점프 두 번으로만 오른다.
            var requiredStep = ResidueRewindable(c.Root.transform, c.P(6.5f, 3f));
            requiredStep.name = "R06_RequiredStep";
            requiredStep.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 2f);
            ReplaceArt(requiredStep, "Rewind_r1_c1", new Vector2(2.5f, 2.2f), 4);

            // 매복 적. 착지점 좌우 3유닛은 비워 둔다.
            BuildResidueEnemy(c.Root.transform, c.P(18f, 11f), ResidueEnemyKind.Finger);
            // 상승 구간 바닥이 y=6이므로 그 위에 세운다.
            BuildResidueEnemy(c.Root.transform, c.P(12f, 7f), ResidueEnemyKind.Walker, "WalkerR06");

            // 선택 대상: 복원하면 S2 입구가 열린다. 주 동선 문은 닫히지 않는다.
            ResidueRewindable(c.Root.transform, c.P(21f, 6f));
            BuildDecor(c.Root.transform, "R06_S2_Hint", c.P(20f, 1f), new Vector2(3f, 0.4f),
                "Tile", new Color(0.3f, 0.26f, 0.34f));

            BuildHealingPickup(c.Root.transform, c.P(30f, 6f));
            c.Room("Room06", 32f, 16f);
        }

        // ---------------- S2 죄인의 심층 (D5 선택) ----------------
        // 선택형 잠금 전투. 전투 시작 전 전장을 내려다볼 수 있다.
        static void BuildS2(RoomCtx c)
        {
            c.Variant = 3;
            c.Floor(0, 24, 2);
            BuildSafePlatform(c.Root.transform, c.P(4f, 10f));   // 상부 관찰·안전 발판
            BuildSafePlatform(c.Root.transform, c.P(20f, 10f));

            // 좌우 복원 가능한 벽 — 돌진을 막거나 뒤를 잡는 길을 만든다.
            ResidueRewindable(c.Root.transform, c.P(6f, 3f));
            ResidueRewindable(c.Root.transform, c.P(18f, 3f));

            var walkerA = BuildResidueEnemy(c.Root.transform, c.P(6f, 3f), ResidueEnemyKind.Walker);
            var walkerB = BuildResidueEnemy(c.Root.transform, c.P(18f, 3f), ResidueEnemyKind.Walker);
            var elite = BuildResidueEnemy(c.Root.transform, c.P(12f, 3f), ResidueEnemyKind.Hardened);

            var reward = BuildRewardChest(c.Root.transform, "residue_s2_shard", c.P(12f, 3.5f), 0, true);

            BuildEncounter(c.Root.transform, "residue_s2", c.P(12f, 6f), new Vector2(20f, 10f), true,
                new[] { new[] { walkerA, walkerB }, new[] { elite } },
                new[] { 1, -1 }, reward, null);

            c.Room("Secret02", 24f, 18f);
        }

        // ---------------- R07 갈비 곡선교 (D3) ----------------
        // 높낮이가 있는 다리 전투. 아래는 체력 1 피해 후 직전 안전 발판으로 되돌리는 위험 영역.
        static void BuildR07(RoomCtx c)
        {
            c.Variant = 2;
            c.Floor(0, 8, 3);
            c.Floor(11, 20, 5);   // 넓은 직선교 — 돌진형 구간(폭 9)
            c.Floor(23, 30, 8);   // 출구 (30,8)
            // R07→R08 직전의 높은 바닥은 낭떠러지 위에 떠 있어 런타임 자동 외곽선만으로는
            // 배경에 묻혔다. 충돌과 같은 x=23~30, 표면 y=8에 잔재 바닥을 명시적으로 겹친다.
            PlaceFloorArt(c.Root.transform, c.P(26.5f, 8f), 7f, c.Variant);
            // y=5 → y=8은 한 번에 오르면 +3이라 실측 점프 높이(2.72)를 넘는다. 중간 발판을 둔다.
            BuildSafePlatform(c.Root.transform, c.P(21.5f, 6.5f));
            // 실제 충돌이 없는 아치형 계단 원화는 통과 가능한 거대한 벽처럼 보여 사용하지
            // 않는다. 위에서 만든 SafePlatform의 정식 발판 그림만 이동 경로로 표시한다.

            // 다리 사이 틈 아래의 위험 영역. 복귀 지점은 방 입구가 아니라 직전 안전 발판이다.
            var recovery = new GameObject("R07_Recovery");
            recovery.transform.SetParent(c.Root.transform, false);
            recovery.transform.position = c.P(6f, 4f);
            BuildHazard(c.Root.transform, c.P(15f, -1f), new Vector2(30f, 4f), 1, recovery.transform);

            BuildResidueEnemy(c.Root.transform, c.P(5f, 4f), ResidueEnemyKind.Walker);  // 좁은 발판 전투
            BuildResidueEnemy(c.Root.transform, c.P(16f, 6f), ResidueEnemyKind.Carrier); // 넓은 직선교

            // 예전 돌진 충돌벽은 중간 발판과 겹쳐, 그림은 계단인데 실제로는 보이지 않는
            // 세로벽에 걸리거나 허공에 서는 원인이 됐다. R07 출구는 SafePlatform 하나로 잇는다.

            // 숏컷 C는 R07 쪽에 놓인 사슬다리다. 여는 것은 R10의 중간 보스 승리다.
            _shortcutC = BuildShortcut(c.Root.transform, "residue_shortcut_c", c.P(25f, 12f),
                new Vector2(4f, 0.6f), new Color(0.8f, 0.6f, 0.5f));

            c.Room("Room07", 30f, 18f);
        }

        // ---------------- R08 상층 승강축 (D3) ----------------
        // 수직 이동 숙련. 구간마다 안전 발판이 있고 전체 바닥까지 떨어지지 않는다.
        static void BuildR08(RoomCtx c)
        {
            c.Variant = 3;
            c.Floor(0, 24, 2);

            // 하단 8유닛: 폭 4 벽점프 굴뚝
            // 정상 모서리와 착지 타일이 맞물리면 벽 붙잡기 판정이 풀리지 않는다.
            // 왼쪽은 왕복 벽점프 높이를 확보하고, 오른쪽은 y=13 착지대 윗면과 높이를
            // 일치시킨다. 따라서 정상에서 별도의 높은 턱에 걸리지 않고 우측으로 나간다.
            BuildResidueWall(c.Root.transform, "R08_Chimney_L", c.P(4f, 8.5f), new Vector2(1f, 9f));
            BuildResidueWall(c.Root.transform, "R08_Chimney_R", c.P(8f, 8.5f), new Vector2(1f, 9f));
            // 예전에는 x=2~11 발판이 굴뚝 내부까지 가로질러 local y=10에 천장처럼 생겼다.
            // 굴뚝 내부는 끝까지 비우고, 정상에서 벽 점프로 잡을 수 있는 오른쪽 바깥에만
            // 착지대를 두어 다음 이동 발판 구간으로 옮겨 서게 한다.
            c.Tiles(8, 12, 12, 13);  // 굴뚝 우측 상단 안전 착지대

            // 중단 10유닛: 왕복 이동 발판 2개
            var lowerLift = BuildMovingPlatform(c.Root.transform, c.P(13f, 13f), new Vector2(4f, 0f), 4f);
            lowerLift.name = "R08_MovingLower";
            var upperLift = BuildMovingPlatform(c.Root.transform, c.P(13f, 15.5f), new Vector2(4f, 0f), 4f);
            upperLift.name = "R08_MovingUpper";
            var upperStep = BuildSafePlatform(c.Root.transform, c.P(14.25f, 18f));
            upperStep.name = "R08_UpperStep";
            c.Tiles(16, 22, 20, 21);

            // 상단 6유닛: 무너지는 발판 2개와 안전 벽면
            ResidueCrumbling(c.Root.transform, c.P(14f, 23f));
            ResidueCrumbling(c.Root.transform, c.P(18f, 25f));
            c.Tiles(20, 24, 26, 27); // 북동 출구 (22,26)

            // 어느 도르래든 하나를 되감으면 연쇄 작동으로 R03 숏컷 B가 열린다. 두 번째는
            // 위험 발판을 택한 숙련 플레이어가 더 가까운 장치를 쓰는 단축 선택이다.
            var pulleyA = ResidueRewindable(c.Root.transform, c.P(12f, 21f));
            var pulleyB = ResidueRewindable(c.Root.transform, c.P(19f, 26f));
            pulleyA.name = "R08_PulleySafe";
            pulleyB.name = "R08_PulleyFast";
            // 숏컷 B도 R03 씬에 있어 R05의 숏컷 A와 같이 id로 연결한다.
            LinkRewindToShortcut(pulleyA, "residue_shortcut_b");
            LinkRewindToShortcut(pulleyB, "residue_shortcut_b");

            c.Room("Room08", 24f, 30f);
        }

        // ---------------- R09 끊어진 상층 고가교 (D4) ----------------
        // 전투 중 무엇을 먼저 복원할지 고르게 한다.
        static void BuildR09(RoomCtx c)
        {
            c.Variant = 2;
            // 초반 관찰 발판과 중앙 전투 구간은 이어 둔다. 사이를 구덩이로 두면 낙하 → 위험 영역
            // 복귀 → 다시 낙하가 반복돼 주 동선이 성립하지 않는다(봇이 여기서 멈췄다).
            c.Floor(0, 24, 3);    // 초반 관찰 발판 + 중앙 전투 구간(14유닛)
            c.Floor(28, 32, 4);   // 출구 (32,4) — 4.5유닛 대시 선택 경로 너머

            // 정예는 주 동선 위가 아니라 위쪽 선택 분기에 둔다. 안전 발판을 타고 올라간
            // 플레이어만 추가 전투와 보상을 치르므로, 재방문 때는 곧장 출구로 달릴 수 있다.
            BuildSafePlatform(c.Root.transform, c.P(6.5f, 5.5f));
            BuildSafePlatform(c.Root.transform, c.P(9f, 7.5f));
            c.Tiles(7, 15, 9, 10);

            var recovery = new GameObject("R09_Recovery");
            recovery.transform.SetParent(c.Root.transform, false);
            recovery.transform.position = c.P(4f, 4f);
            BuildHazard(c.Root.transform, c.P(16f, -1f), new Vector2(32f, 4f), 1, recovery.transform);

            var finger = BuildResidueEnemy(c.Root.transform, c.P(13f, 11f), ResidueEnemyKind.Finger);
            var carrier = BuildResidueEnemy(c.Root.transform, c.P(20f, 4f), ResidueEnemyKind.Carrier);
            var elite = BuildResidueEnemy(c.Root.transform, c.P(11f, 10f), ResidueEnemyKind.Hardened);

            // 복원 대상 둘을 서로 다른 자리에 둔다 — 다리는 안전한 주 동선, 벽은 정예 공략용.
            ResidueRewindable(c.Root.transform, c.P(25f, 4f)); // 다리
            ResidueRewindable(c.Root.transform, c.P(21f, 4f)); // 방어벽

            var mainEncounter = BuildEncounter(c.Root.transform, "residue_r09_main", c.P(16f, 5f), new Vector2(20f, 6f), false,
                new[] { new[] { finger }, new[] { carrier } }, new[] { 0 }, null, null);
            mainEncounter.ConfigureTraversalLock(false);

            var reward = BuildRewardChest(c.Root.transform, "residue_r09_elite", c.P(14f, 10.5f), 15, false);
            var eliteEncounter = BuildEncounter(c.Root.transform, "residue_r09_elite", c.P(11f, 11f), new Vector2(10f, 6f), true,
                new[] { new[] { elite } }, System.Array.Empty<int>(), reward, null);
            eliteEncounter.ConfigureTraversalLock(false);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(25.5f + i * 0.5f, 5f)); // 대시 선택 경로 보상

            c.Room("Room09", 32f, 16f);
        }

        // ---------------- R10 손목 감시탑 (D4, 중간 보스) ----------------
        static void BuildR10(RoomCtx c)
        {
            c.Variant = 1;
            c.Floor(0, 24, 3);
            // 상시 외벽은 두지 않는다. Encounter가 전투 시작 뒤에만 Lock_L/Lock_R을 켠다.
            // 두 체계를 겹치면 입구부터 막혀 보스 전장에 들어갈 수 없다.
            // 승리 뒤 오른쪽 출구(높이 7)까지 두 번의 일반 점프로 올라간다. 예전 y=9 발판은
            // 바닥에서 첫 점프로 닿지 않아 보스를 쓰러뜨리고도 방을 나갈 수 없었다.
            var exitStepLow = BuildSafePlatform(c.Root.transform, c.P(19.25f, 5f));
            exitStepLow.name = "R10_ExitStep_Low";
            var exitStepHigh = BuildSafePlatform(c.Root.transform, c.P(22f, 7f));
            exitStepHigh.name = "R10_ExitStep_High";

            // 체크포인트 3은 전장 "바깥"이다 — 재진입 30초 이내 목표를 지키려면 문 앞이어야 한다.
            BuildCheckpoint(c.Root.transform, c.P(2f, 4f));

            // 좌우 복원 방어벽 — 돌진을 막는 데 쓴다.
            ResidueRewindable(c.Root.transform, c.P(8f, 4f));
            ResidueRewindable(c.Root.transform, c.P(16f, 4f));

            // 감시 파동을 섞는다. 전장이 넓어 붙기만 해서는 안 되게 만드는 원거리 압박이다.
            var boss = BuildBoss(c.Root.transform, c.P(18f, 5f), "Enemy_Residue_Watcher", 8,
                new[]
                {
                    BossController.Move.GroundSweep, BossController.Move.Projectile,
                    BossController.Move.Charge, BossController.Move.Slam,
                },
                new[] { 0.5f }, new Color(0.66f, 0.5f, 0.45f),
                moveClips: new[] { "WatcherAnimSweep", "WatcherAnimSweep",
                                   "WatcherAnimCharge", "WatcherAnimDrop" },
                phaseClip: "WatcherAnimStun");
            // BuildBoss의 공통 2배 확대는 최종 보스용 화면 점유에 가깝다. R10은 플레이어의
            // 약 2배 높이(아트 약 4유닛)만 차지하게 줄이고, 실루엣 중심을 판정 중앙에 맞춘다.
            boss.transform.localScale = Vector3.one * 1.25f;
            boss.GetComponentInChildren<SpriteAnimator>()?.LockReferenceCenterToLocalX(0f);
            boss.GetComponent<BossController>().ConfigureDifficulty(
                1.6f, 8f, 0.9f, 1.25f, 1.4f, 1.25f, 4.2f);
            SetField(boss.GetComponent<BossController>(), "projectileName", p => p.stringValue = "BossWave");
            var watcherPresentation = boss.AddComponent<ResidueBossPresentationGuard>();
            watcherPresentation.Configure("WatcherAnimIdle");
            watcherPresentation.ConfigureArenaFloor(c.P(0f, 3f).y);
            boss.AddComponent<ResidueFinalBossDeathCleanup>();

            var reward = BuildRewardChest(c.Root.transform, "residue_r10_boss", c.P(12f, 4.5f), 40, false);
            // R09 적 처치 여부와 무관하게 이 방에 도착하면 시작하는 필수 중간 보스.
            // 전투가 시작된 뒤에는 승리할 때까지 전장을 잠그고, 승리하면 큰 재화와 숏컷을 연다.
            // 숏컷 C를 여는 것은 이 보스의 승리다(LEVEL_21_RESIDUE_ROOMS.md R10 "승리 후 R07 숏컷").
            // 숏컷 자체는 R07 씬에 있으므로 오브젝트가 아니라 id로 연결한다.
            var midBossEncounter = BuildEncounter(c.Root.transform, "residue_r10_boss", c.P(12f, 7f), new Vector2(20f, 10f), true,
                new[] { new[] { boss } }, new int[0], reward, _shortcutC, "residue_shortcut_c");
            SetField(midBossEncounter, "victoryObjects", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = exitStepLow;
                p.GetArrayElementAtIndex(1).objectReferenceValue = exitStepHigh;
            });

            c.Room("Room10", 24f, 18f);
        }

        // ---------------- R11 후회의 회랑 (D1→D3) ----------------
        // 보스 전 정적. 전투가 없고, 실패해도 아래 안전 통로로 우회한다.
        static void BuildR11(RoomCtx c)
        {
            c.Variant = 2;
            c.Floor(0, 10, 3);
            c.Floor(10, 28, 1);   // 실패 시 우회하는 아래 통로
            c.Floor(22, 28, 4);   // 끝 6유닛 완전 안전지대, 출구 (28,4)

            // 숙련 간격 점프 2회(3.5~4유닛).
            BuildSafePlatform(c.Root.transform, c.P(13.5f, 4f));
            BuildSafePlatform(c.Root.transform, c.P(18f, 4f));

            BuildStoryFragment(c.Root.transform, c.P(6f, 4f), "residue_r11",
                "벽의 홈마다 서로 다른 형상이 놓여 있다.", EmotionId.None, false);

            // 충돌 없는 교수대 실루엣과 임시 Tile 표식은 주 동선의 벽·발판으로 오해되어
            // 제거한다. S3는 조건을 갖춘 뒤 실제 입구와 안내로만 드러낸다.

            // S3로 오르는 서쪽 계단. R12로 가는 길(발판 13.5·18)에서 완전히 떨어진 서쪽 바닥
            // (x 0~10, 표면 y=3) 위에만 세운다 — 되감기 게이트를 R12 동선에 놓지 않으려면
            // 비밀방 가지가 주 동선과 물리적으로 갈라져 있어야 한다.
            // 발판 콜라이더는 3x0.5, 중심 기준이라 윗면 = y+0.25, 아랫면 = y-0.25다.
            //   1단 (3, 5.0)   : 윗면 5.25. 바닥 3 → 2.25 상승(점프 2.72, 여유 0.47).
            //                    아랫면 4.75 > 바닥 위 몸통 상단 4.4라 밑을 걸어 지나갈 수 있다.
            //   2단 (7, 7.2)   : 윗면 7.45. 1단 5.25 → 2.20 상승(여유 0.52).
            //                    가로 간격 1단 오른끝 4.5 → 2단 왼끝 5.5 = 1.0. 2.2 오르는 데
            //                    0.21초, 걷기 6로도 1.27 나아가므로 넉넉하다.
            BuildSafePlatform(c.Root.transform, c.P(3f, 5f));
            BuildSafePlatform(c.Root.transform, c.P(7f, 7.2f));

            // S3 문 앞을 막는 되감기 게이트. 블로커는 1x3 중심 기준이라 x 4.0~5.0, y 5.25~8.25를
            // 차지한다. 1단 윗면(5.25)에 밑을 붙여 세워 아래로 빠져나갈 틈이 없고, 윗면 8.25는
            // 1단에서 뛰어 닿는 최고 발높이 7.97보다 높아 넘어갈 수도 없다. 오른끝 5.0은 2단
            // 왼끝 5.5와 맞물려, 열리기 전에는 2단에 올라설 방법이 아예 없다(2단은 바닥에서
            // 4.45 위라 직접 못 뛴다).
            // R12 동선은 막지 않는다 — 게이트 밑면 5.25는 서쪽 바닥을 걷는 몸통 상단 4.4보다
            // 0.85 높고, 게이트는 x 4~5라 R12로 가는 발판(x 12~15, 16.5~19.5)과 닿지 않는다.
            // 이 게이트가 "균열 클리어 후 잔재 재방문"을 성립시킨다(LEVEL_00_INDEX.md §0).
            BuildGate(c.Root.transform, c.P(4.5f, 6.75f), EmotionId.Rewind, true);

            c.Room("Room11", 28f, 16f);
        }

        // ---------------- S3 감춰진 눈 (D0) ----------------
        // 자각 보유 + 균열 클리어로만 들어온다. 전투·낙하·시간제한 없음.
        static void BuildS3(RoomCtx c)
        {
            c.Variant = 1;
            c.Floor(0, 20, 2);

            BuildDecor(c.Root.transform, "S3_Face", c.P(10f, 7f), new Vector2(8f, 6f),
                "Tile", new Color(0.28f, 0.26f, 0.33f));

            // 조건 게이트는 S3 안이 아니라 "들어오는 길목"(R11의 샤프트 입구)에 있다.
            // 여기 안쪽에 또 두면 최종 파편과 같은 자리에 겹쳐 파편이 지형에 파묻힌다.
            // 파편은 착지 지점(x=14)에서 조금 걸어 들어간 안쪽에 둔다.
            BuildStoryFragment(c.Root.transform, c.P(6f, 3f), "residue_final",
                "벽 뒤에서 거대한 눈이 드러났다.", EmotionId.None, false);

            c.Room("Secret03", 20f, 14f);
        }

        // ---------------- R12 기억의 교수대 (D5, 지역 보스) ----------------
        static void BuildR12(RoomCtx c)
        {
            c.Variant = 1;
            c.Floor(0, 30, 3);
            // 평상시 입구·출구는 열어 둔다. Encounter의 Lock_L/Lock_R이 전투 시작 뒤에만
            // 전장을 잠근다. 상시 외벽까지 겹치면 서쪽에서 방 안으로 진입할 수 없다.

            // 천장 교수대 사슬 3개 — 2단계에서 보스가 이 중 하나에서 떨어진다.
            for (int i = 0; i < 3; i++)
                BuildResidueProp(c.Root.transform, $"R12_Chain_{i}", c.P(8f + i * 7f, 9f), new Vector2(1.5f, 7f), "Prop_r1_c4");

            // 바닥의 복원 가능한 안전 발판 2개. 3단계에서 하나는 다시 부서진다.
            var arenaLeft = ResidueRewindable(c.Root.transform, c.P(10f, 4f));
            var arenaRight = ResidueRewindable(c.Root.transform, c.P(20f, 4f));

            // 기억침을 섞는다. 되감기로 복원한 발판 뒤에 숨어 피하는 것이 공략의 일부다.
            // 전용 시트(MemoryInstructor_*)를 쓴다 — 손목 감시자 그림을 재사용하던 것을
            // 교체했다. 행 의미는 residue-animation-sprites/PROMPTS.md 납품 표 그대로.
            var boss = BuildBoss(c.Root.transform, c.P(15f, 5f), "Enemy_Residue_Professor", 12,
                new[]
                {
                    BossController.Move.GroundSweep, BossController.Move.Projectile,
                    BossController.Move.Slam, BossController.Move.Charge,
                },
                new[] { 0.6f, 0.3f }, new Color(0.5f, 0.42f, 0.55f),
                new[]
                {
                    ("InstructorHalo",     8f,  true),   // 후광 순환 = 대기
                    ("InstructorSweep",    14f, false),
                    ("InstructorHook",     14f, false),
                    ("InstructorSlam",     12f, false),
                    ("InstructorRecover",  10f, false),
                    ("InstructorCore",     10f, false),
                    ("InstructorOverload", 14f, false),
                    ("InstructorHit",      16f, false),
                    ("InstructorPhase",    12f, false),
                    ("InstructorDeath",    10f, false),
                },
                "InstructorHalo_00",
                clipPrefix: "Instructor",
                moveClips: new[] { "InstructorSweep", "InstructorCore",
                                   "InstructorSlam", "InstructorHook" },
                phaseClip: "InstructorPhase");
            SetField(boss.GetComponent<BossController>(), "projectileName", p => p.stringValue = "BossNeedle");
            var professorPresentation = boss.AddComponent<ResidueBossPresentationGuard>();
            professorPresentation.Configure("InstructorHalo");
            professorPresentation.ConfigureArenaFloor(c.P(0f, 3f).y);
            boss.AddComponent<ResidueFinalBossDeathCleanup>();
            // 첫 지역 최종 보스는 패턴을 읽는 시험에 집중한다. 체력과 압박 속도를 함께
            // 낮춰 한 번의 실수로 연속 피격되는 구간을 줄이고, 공격 전 예고를 충분히 준다.
            boss.GetComponent<BossController>().ConfigureDifficulty(
                2.2f, 6.5f, 1.1f, 1.4f, 1.6f, 1.45f, 3.8f);

            // 3단계에서 보스가 다시 부수는 전장 발판. 예전에는 Full 씬 전용 런타임 보정
            // (ResidueLoopRuntime)이 방 경계로 찾아 넣어 줬는데, 그 보정은 방 씬에서 돌지
            // 않는다 — 같은 씬 안의 참조이므로 굽는 시점에 박아 둔다.
            SetField(boss.GetComponent<BossController>(), "arenaRewindables", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = arenaLeft.GetComponent<Rewindable>();
                p.GetArrayElementAtIndex(1).objectReferenceValue = arenaRight.GetComponent<Rewindable>();
            });

            var reward = BuildRewardChest(c.Root.transform, "residue_r12_boss", c.P(15f, 4.5f), 60, true);
            var finalEncounter = BuildEncounter(c.Root.transform, "residue_r12_boss", c.P(15f, 8f), new Vector2(26f, 12f), true,
                new[] { new[] { boss } }, new int[0], reward, null);

            // 지역 보스 승리 뒤 핵심 기억을 드러내고, 해당 완료 상태가 있어야만 응시로 나간다.
            var core = BuildStoryFragment(c.Root.transform, c.P(24f, 4.5f), "residue_core",
                "기억의 교수대에서 발견한 파편.", EmotionId.None, false);
            core.SetActive(false);
            SetField(finalEncounter, "victoryObjects", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = core;
            });

            // 승리 후 열리는 응시 지역 통로.
            var exit = BuildZoneTrigger(c.Root.transform, c.P(28f, 5f), new Vector2(2f, 3f), false);
            SetField(exit.GetComponent<ZoneTrigger>(), "requiredEncounterId",
                p => p.stringValue = "residue_r12_boss");

            c.Room("Room12", 30f, 18f);
            BuildBoundary(c.Root.transform, "Zone_EastBoundary", c.P(30.5f, 0f).x);
        }
    }
}
