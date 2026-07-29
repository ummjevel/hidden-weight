using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.World;

namespace HiddenWeight.EditorTools
{
    // 잔재 지역 전체(주 동선 12룸 + 비밀 3룸)를 한 씬에 짓는다.
    // 명세: docs/RESIDUE_ROOM_IMPLEMENTATION.md, docs/RESIDUE_LEVEL_DESIGN.md, docs/WORLD_MAP.md
    //
    // 방마다 좌표계를 왼쪽 아래 (0,0)으로 두고 쓰는 것은 명세 0절의 규칙 그대로다. 여기서는
    // 방마다 오프셋을 하나 정해 두고 Room 헬퍼가 전역 좌표로 옮긴다 — 그래야 방 코드가
    // 문서에 적힌 숫자와 1:1로 읽힌다.
    //
    // 기존 Zone_Residue.unity(옛 MVP 4룸)는 건드리지 않는다. 이 씬이 검증을 통과하면 그때 교체한다.
    public static partial class ZoneSceneBuilder
    {
        // 방 배치 오프셋. 주 동선은 왼쪽에서 오른쪽으로, 하강 구간은 아래로 내린다.
        // 방끼리 겹치지 않게만 두면 되고, 실제 연결감은 각 방의 출입구 높이가 맞물려 만든다.
        // 방 배치 오프셋. 각 방의 "출구 높이"와 다음 방의 "입구 높이"가 세계 좌표에서 정확히
        // 맞도록 y를 계산했다. 예: R03 출구 (27,1) → 세계 y=2, R04 입구 (2,20) → R04.y = 2-20 = -18.
        // x는 이전 방 오른쪽 끝에서 CorridorGap만큼 띄우고, 그 사이를 평평한 연결 통로가 잇는다.
        const int CorridorGap = 4;

        static readonly Vector2Int R01 = new Vector2Int(0, 0);
        static readonly Vector2Int R02 = new Vector2Int(30, 0);
        static readonly Vector2Int R03 = new Vector2Int(62, 1);
        static readonly Vector2Int R04 = new Vector2Int(96, -18);
        static readonly Vector2Int S1 = new Vector2Int(96, -44);
        static readonly Vector2Int R05 = new Vector2Int(124, -18);
        static readonly Vector2Int R06 = new Vector2Int(154, -18);
        static readonly Vector2Int S2 = new Vector2Int(154, -40);
        static readonly Vector2Int R07 = new Vector2Int(190, -16);
        static readonly Vector2Int R08 = new Vector2Int(224, -10);
        static readonly Vector2Int R09 = new Vector2Int(252, 13);
        static readonly Vector2Int R10 = new Vector2Int(288, 14);
        static readonly Vector2Int R11 = new Vector2Int(316, 18);
        static readonly Vector2Int S3 = new Vector2Int(316, 38);
        static readonly Vector2Int R12 = new Vector2Int(348, 19);

        // 잘라 둔 잔재 스프라이트를 이름으로 찾는다(ResidueArtSlicer가 붙인 이름).
        // 없으면 null을 돌려주고, 호출부는 기존 플레이스홀더로 넘어간다 — 아트가 아직 없는
        // 항목 때문에 씬 생성이 실패하면 안 된다.
        static Dictionary<string, Sprite> _residueSprites;

        static Sprite Art(string spriteName)
        {
            if (_residueSprites == null)
            {
                _residueSprites = new Dictionary<string, Sprite>();
                foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Residue" }))
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

        // 되감기 대상에 "복원되면 이 숏컷을 연다"를 연결한다.
        static void LinkRewindToShortcut(GameObject rewindable, Shortcut shortcut, params GameObject[] siblings)
        {
            var component = rewindable.GetComponent<Rewindable>();
            if (component == null || shortcut == null) return;

            SetField(component, "linkedShortcut", p => p.objectReferenceValue = shortcut);
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
            public bool FloorArt = true;

            public void Floor(int x0, int x1, int top, int depth = 8)
            {
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
            sr.sprite = Art(healthShard ? "Item_HealthShard" : "Item_Currency") ?? LoadSprite("Fragment");
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
                                        RewardChest reward, Shortcut shortcut)
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

            return encounter;
        }


        // 바닥 표면을 덮는 직선 지형 모듈. 피벗이 Bottom Center라 표면 y에 그대로 세운다.
        static void PlaceFloorArt(Transform parent, Vector2 surfaceCenter, float width, int variant = 1)
        {
            // 1행 = 직선 바닥. 열을 바꿔 방마다 결을 달리한다.
            var sprite = Art($"Terrain_r1_c{Mathf.Clamp(variant, 1, 3)}");
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
        static void BuildImpactVFX(Transform parent)
        {
            var definitions = new (string name, float fps, float height)[]
            {
                ("ImpactMelee", 18f, 1.4f),
                ("ImpactWall", 16f, 1.6f),
                ("ImpactLand", 14f, 1.0f),
                ("ImpactHeavy", 16f, 1.8f),
            };

            var valid = new List<(string name, float fps, float height, Sprite[] frames)>();
            foreach (var definition in definitions)
            {
                var frames = ArtFrames(definition.name);
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

        // 방과 방 사이의 평평한 연결 통로. 양쪽 높이를 맞춰 뒀으므로 바닥 한 줄이면 이어진다.
        // 천장을 두어 통로가 "길"로 읽히게 하고, 위로 빠져나가 엉뚱한 방 지붕에 올라서는 것도 막는다.
        static void BuildCorridor(Transform parent, Tilemap map, string name,
                                  float fromX, float toX, int surfaceY)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(fromX, toX));
            int x1 = Mathf.CeilToInt(Mathf.Max(fromX, toX));
            Floor(map, x0, x1, surfaceY);
            BuildSolidBlock(parent, name + "_Ceiling", new Vector2((x0 + x1) * 0.5f, surfaceY + 5.5f),
                new Vector2(x1 - x0, 1f), "Ground");
        }

        // 비밀방으로 내려가는 수직 통로. 양쪽에 벽점프용 벽을 세워 다시 올라올 수 있게 한다
        // (명세: 비밀방은 "동일 경로 복귀").
        static void BuildShaft(Transform parent, Tilemap map, string name,
                               int centerX, int topY, int bottomY, int width = 4)
        {
            int half = width / 2;
            BuildSolidBlock(parent, name + "_L", new Vector2(centerX - half - 0.5f, (topY + bottomY) * 0.5f),
                new Vector2(1f, topY - bottomY), "Wall");
            BuildSolidBlock(parent, name + "_R", new Vector2(centerX + half + 0.5f, (topY + bottomY) * 0.5f),
                new Vector2(1f, topY - bottomY), "Wall");
        }

        // 주 동선 12룸을 순서대로 잇고, 비밀방 3곳으로 내려가는(올라가는) 수직 통로를 놓는다.
        static void BuildConnections(RoomCtx c)
        {
            var map = c.Map;
            var parent = c.Root.transform;

            // (이전 방 오프셋, 이전 방 출구 로컬, 다음 방 오프셋, 다음 방 입구 로컬)
            var links = new (Vector2Int from, Vector2 exit, Vector2Int to, Vector2 entry, string name)[]
            {
                (R01, new Vector2(26, 2), R02, new Vector2(0, 2), "C_R01_R02"),
                (R02, new Vector2(28, 3), R03, new Vector2(0, 2), "C_R02_R03"),
                (R03, new Vector2(27, 1), R04, new Vector2(2, 20), "C_R03_R04"),
                (R04, new Vector2(22, 2), R05, new Vector2(0, 2), "C_R04_R05"),
                (R05, new Vector2(26, 2), R06, new Vector2(0, 2), "C_R05_R06"),
                (R06, new Vector2(32, 5), R07, new Vector2(0, 3), "C_R06_R07"),
                (R07, new Vector2(30, 8), R08, new Vector2(2, 2), "C_R07_R08"),
                (R08, new Vector2(22, 26), R09, new Vector2(0, 3), "C_R08_R09"),
                (R09, new Vector2(32, 4), R10, new Vector2(0, 3), "C_R09_R10"),
                (R10, new Vector2(24, 7), R11, new Vector2(0, 3), "C_R10_R11"),
                (R11, new Vector2(28, 4), R12, new Vector2(0, 3), "C_R11_R12"),
            };

            foreach (var link in links)
            {
                float exitX = link.from.x + link.exit.x;
                float exitY = link.from.y + link.exit.y;
                float entryX = link.to.x + link.entry.x;
                float entryY = link.to.y + link.entry.y;

                // 오프셋을 그렇게 잡았으므로 두 높이는 같아야 한다. 어긋나면 즉시 알 수 있게 남긴다.
                if (!Mathf.Approximately(exitY, entryY))
                    Debug.LogWarning($"[ResidueZoneBuilder] {link.name} 높이 불일치: 출구 y={exitY}, 입구 y={entryY}");

                BuildCorridor(parent, map, link.name, exitX, entryX, Mathf.RoundToInt(exitY));
            }

            // S1 — R04의 부서진 바닥(로컬 7.5, 6) 아래로 내려간다.
            BuildShaft(parent, map, "Shaft_S1", R04.x + 8, R04.y + 6, S1.y + 14);
            Floor(map, R04.x + 6, R04.x + 11, S1.y + 14);

            // S2 — R06의 선택 대상 아래(로컬 20, 1).
            BuildShaft(parent, map, "Shaft_S2", R06.x + 20, R06.y + 1, S2.y + 18);
            Floor(map, R06.x + 18, R06.x + 23, S2.y + 18);

            // S3 — R11의 상부 벽 뒤(로컬 14, 10). 샤프트 입구 자체를 조건 게이트로 막는다.
            // 게이트가 열리기 전에는 올라갈 수 없어야 "균열 클리어 후 재방문"이 성립한다.
            BuildShaft(parent, map, "Shaft_S3", R11.x + 14, S3.y, R11.y + 10);
            Floor(map, R11.x + 12, R11.x + 17, S3.y);
            BuildGate(parent, new Vector2(R11.x + 14, R11.y + 11), EmotionId.Rewind, true);
        }

        [MenuItem("Hidden Weight/Build Residue Zone (Full)")]
        public static void RunResidueZone()
        {
            EnsureScenesFolder();

            var scene = NewScene();
            var tilemap = BuildZoneRoot("Residue", out var root);
            var rooms = new GameObject("Rooms");
            rooms.transform.SetParent(root.transform, true);

            // 씬 이름이 ZoneData.sceneName과 다르므로 지역을 씬이 직접 선언한다.
            var marker = new GameObject("ZoneMarker");
            marker.transform.SetParent(root.transform, false);
            var zoneMarker = marker.AddComponent<HiddenWeight.Core.ZoneMarker>();
            SetField(zoneMarker, "zone", p => p.enumValueIndex = (int)ZoneId.Residue);

            var ctx = new RoomCtx { Map = tilemap, Root = root, Rooms = rooms.transform };

            BuildR01(ctx);
            BuildR02(ctx);
            BuildR03(ctx);
            BuildR04(ctx);
            BuildS1(ctx);
            BuildR05(ctx);
            BuildR06(ctx);
            BuildS2(ctx);
            BuildR07(ctx);
            BuildR08(ctx);
            BuildR09(ctx);
            BuildR10(ctx);
            BuildR11(ctx);
            BuildS3(ctx);
            BuildR12(ctx);

            BuildConnections(ctx);

            // 충돌 연출은 지역에 하나만 둔다(근접 타격·벽 충돌·착지).
            BuildImpactVFX(root.transform);

            // 숏컷 3곳에 봉쇄·해제 애니메이션을 붙인다. 만든 곳과 여는 곳이 달라서
            // 여기서 한 번에 처리한다.
            AttachSealAnimator(_shortcutA, new Vector2(4f, 2f));
            AttachSealAnimator(_shortcutB, new Vector2(3f, 2.5f));
            AttachSealAnimator(_shortcutC, new Vector2(4f, 2f));

            // 플레이어는 R01 시작점에. 카메라도 같은 자리에서 시작한다.
            ctx.O = R01;
            PlacePlayerAndCamera(root, new Vector3(R01.x + 3f, R01.y + 3f, 0f));

            // 방마다 배경 3층(Far/Mid/FG)을 붙인다. 아직 그림이 없는 방(비밀방 등)은 건너뛴다 —
            // 지형과 배치를 먼저 검증할 수 있어야 하므로 아트가 없다고 씬 생성이 실패하면 안 된다.
            ResidueArtImporter.ConfigureAll();
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                try { ResidueRoomArtBuilder.BuildRoomArt(room); }
                catch (System.Exception e) { Debug.Log($"[ResidueZoneBuilder] {room.name} 배경 생략: {e.Message}"); }
            }

            SaveScene(scene, "Zone_Residue_Full");
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ResidueZoneBuilder] 잔재 전체 지역(15룸) 생성 완료");
        }

        // ---------------- R01 입구 경계 (D0) ----------------
        // 사망 요소도 이동을 막는 요소도 없다. 캐릭터 크기와 배경 규모를 비교하는 방.
        static void BuildR01(RoomCtx c)
        {
            c.O = R01;
            c.Variant = 1;
            c.Floor(0, 26, 2);
            c.Floor(8, 11, 3);   // 둔덕 1유닛
            c.Floor(15, 18, 3);  // 계단 1단
            c.Floor(16, 18, 4);  // 계단 2단

            BuildCheckpoint(c.Root.transform, c.P(5f, 3f));

            BuildCurrencyPickup(c.Root.transform, c.P(15f, 4f));
            BuildCurrencyPickup(c.Root.transform, c.P(16.5f, 4.5f));
            BuildCurrencyPickup(c.Root.transform, c.P(18f, 5f));

            c.Room("Room01", 26f, 14f);
            BuildBoundary(c.Root.transform, "Zone_WestBoundary", c.P(-0.5f, 0f).x);
        }

        // ---------------- R02 애도교 (D1→D2) ----------------
        // "실패해도 다른 길이 된다"를 가르친다. 상부 다리는 보상, 하부 통로는 안전한 우회.
        static void BuildR02(RoomCtx c)
        {
            c.O = R02;
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
            BuildSolidBlock(c.Root.transform, "R02_Bridge_A", c.P(9.25f, 3.75f), new Vector2(2.5f, 0.5f), "Ground");
            BuildSolidBlock(c.Root.transform, "R02_Bridge_B", c.P(13.5f, 3.75f), new Vector2(2f, 0.5f), "Ground");

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
            c.O = R03;
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
            c.O = R04;
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

            // 다시 올라가는 벽점프 굴뚝 — 폭 4, 높이 8.
            // 벽 하단을 바닥(y=2)에서 2유닛 띄운다. 1유닛만 띄우면 키 1.4의 플레이어가 굴뚝
            // 안으로 걸어 들어가지 못해 입구가 막힌 것과 같아진다(봇이 R08에서 그렇게 멈췄다).
            BuildResidueWall(c.Root.transform, "R04_Chimney_L", c.P(18f, 8f), new Vector2(1f, 8f));
            BuildResidueWall(c.Root.transform, "R04_Chimney_R", c.P(22f, 8f), new Vector2(1f, 8f));

            // 명세: "중층 폭 5유닛 발판, 플레이어가 위에서 먼저 관찰". 폭 3 발판 위에 두면
            // 돌아설 자리가 없어 제자리에서 떠는 것처럼 보인다.
            // 하강용 지그재그 발판(폭 3)과 겹치지 않는 자리에 깐다. 겹치면 적이 발판 위에
            // 올라서서 돌아설 폭이 없어진다.
            c.Tiles(12, 18, 13, 14);
            BuildResidueEnemy(c.Root.transform, c.P(15f, 15f), ResidueEnemyKind.Walker);
            BuildResidueEnemy(c.Root.transform, c.P(16f, 3f), ResidueEnemyKind.Walker);  // 하층, S1 반대편

            c.Room("Room04", 24f, 22f);
        }

        // ---------------- S1 납골당 (D3 선택) ----------------
        // 짧은 정밀 이동 퍼즐. 즉사도 체력 피해도 쓰지 않는다.
        static void BuildS1(RoomCtx c)
        {
            c.O = S1;
            c.Variant = 3;
            c.Floor(0, 18, 1);   // 실패 시 떨어지는 안전 바닥

            // 폭 1.5 발판 3개를 2.5 / 3 / 3.5 간격으로. 마지막만 무너진다.
            BuildSafePlatform(c.Root.transform, c.P(4f, 6f));
            BuildSafePlatform(c.Root.transform, c.P(8.5f, 7f));
            BuildSafePlatform(c.Root.transform, c.P(13.5f, 8f));
            ResidueCrumbling(c.Root.transform, c.P(16f, 9f));

            BuildStoryFragment(c.Root.transform, c.P(16f, 10.5f), "residue_s1",
                "이름이 남지 않은 것들도, 여기서는 나란히 누워 있었다.", EmotionId.None, false);
            for (int i = 0; i < 6; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(15f + i * 0.4f, 10f));

            c.Room("Secret01", 18f, 14f);
        }

        // ---------------- R05 되감기 성소 (D0→D2) ----------------
        // 되감기 획득과 3단계 튜토리얼. 실패·적·시간제한이 없다.
        static void BuildR05(RoomCtx c)
        {
            c.O = R05;
            c.Variant = 2;
            c.Floor(0, 12, 2);
            // 1단계 턱을 4유닛으로 세운다. 실측 점프 높이가 2.72라 기본 점프로는 절대 못 오른다 —
            // 복원한 블록(로컬 11,3 → 되감기로 11,6)을 밟고 올라가야 한다. 2유닛이던 시절에는
            // 봇이 되감기를 한 번도 쓰지 않고 R09까지 통과했다.
            c.Floor(12, 16, 6);
            c.Floor(16, 17, 2);
            // 2단계: 폭 6 틈(17~23). 달리기 점프 도달이 6.66이라 아슬아슬해 보이지만, 착지면이
            // 2유닛 높아서 실제로는 넘지 못한다. 다리 조각을 복원해 건넌다.
            c.Floor(23, 26, 4);

            BuildCheckpoint(c.Root.transform, c.P(7f, 3f)); // 체크포인트 2 — 능력 획득 직전

            BuildStoryFragment(c.Root.transform, c.P(10f, 4f), "residue_skill",
                "그때로 돌아갈 수만 있다면, 손끝이라도 붙잡았을 텐데.", EmotionId.Rewind, false);
            BuildTutorialHint(c.Root.transform, c.P(11f, 6f), "K 홀드  —  되감기");

            // 대상 1: 원래 자리는 턱 중간 높이(11,4.5)다. 시작하면 중력으로 바닥까지 굴러떨어져
            // 있고, 되감으면 제자리로 올라가 4유닛 턱을 오르는 디딤돌이 된다.
            ResidueRewindable(c.Root.transform, c.P(11f, 4.5f));
            // 대상 2: 끊어진 다리 조각. 원래 자리는 틈 한가운데 공중이라, 복원해야 발판이 생긴다.
            ResidueRewindable(c.Root.transform, c.P(20f, 6.5f));
            // 대상 3: R03 숏컷 A를 여는 사슬장치.
            var chain = ResidueRewindable(c.Root.transform, c.P(22f, 7f));
            chain.name = "R05_ChainDevice";
            LinkRewindToShortcut(chain, _shortcutA); // 3단계: 복원하면 R03 숏컷 A가 열린다

            c.Room("Room05", 26f, 14f);
        }

        // ---------------- R06 손가락 내부 (D2→D3) ----------------
        // 되감기와 이동을 결합한다. 매복 적(매달린 손가락)을 처음 소개하는 방.
        static void BuildR06(RoomCtx c)
        {
            c.O = R06;
            c.Variant = 3;
            c.Floor(0, 8, 2);
            // 5유닛 상승. 복원 블록을 발판으로 써야만 오른다(기본 점프 2.72).
            c.Floor(8, 20, 7);
            c.Floor(20, 32, 5);   // 출구 (32,5)

            // 필수 대상. 원래 자리(7, 4.5)로 복원하면 5유닛 턱을 오르는 디딤돌이 된다.
            ResidueRewindable(c.Root.transform, c.P(7f, 4.5f));

            // 매복 적. 착지점 좌우 3유닛은 비워 둔다.
            BuildResidueEnemy(c.Root.transform, c.P(18f, 11f), ResidueEnemyKind.Finger);
            // 상승 구간 바닥이 y=7이므로 그 위에 세운다. 예전엔 y=5라 지형에 묻혀 있었다.
            BuildResidueEnemy(c.Root.transform, c.P(12f, 8f), ResidueEnemyKind.Walker);

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
            c.O = S2;
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
            c.O = R07;
            c.Variant = 2;
            c.Floor(0, 8, 3);
            c.Floor(11, 20, 5);   // 넓은 직선교 — 돌진형 구간(폭 9)
            c.Floor(23, 30, 8);   // 출구 (30,8)
            // y=5 → y=8은 한 번에 오르면 +3이라 실측 점프 높이(2.72)를 넘는다. 중간 발판을 둔다.
            BuildSafePlatform(c.Root.transform, c.P(21.5f, 6.5f));

            // 다리 사이 틈 아래의 위험 영역. 복귀 지점은 방 입구가 아니라 직전 안전 발판이다.
            var recovery = new GameObject("R07_Recovery");
            recovery.transform.SetParent(c.Root.transform, false);
            recovery.transform.position = c.P(6f, 4f);
            BuildHazard(c.Root.transform, c.P(15f, -1f), new Vector2(30f, 4f), 1, recovery.transform);

            BuildResidueEnemy(c.Root.transform, c.P(5f, 4f), ResidueEnemyKind.Walker);  // 좁은 발판 전투
            BuildResidueEnemy(c.Root.transform, c.P(16f, 6f), ResidueEnemyKind.Carrier); // 넓은 직선교

            // 돌진을 받아내는 낮은 벽. 여기에 박으면 1.5초 경직.
            BuildResidueWall(c.Root.transform, "R07_CrashWall", c.P(20.5f, 6f), new Vector2(1f, 2f));

            // 숏컷 C는 R07 쪽에 놓인 사슬다리다. 여는 것은 R10의 중간 보스 승리다.
            _shortcutC = BuildShortcut(c.Root.transform, "residue_shortcut_c", c.P(25f, 12f),
                new Vector2(4f, 0.6f), new Color(0.8f, 0.6f, 0.5f));

            c.Room("Room07", 30f, 18f);
        }

        // ---------------- R08 상층 승강축 (D3) ----------------
        // 수직 이동 숙련. 구간마다 안전 발판이 있고 전체 바닥까지 떨어지지 않는다.
        static void BuildR08(RoomCtx c)
        {
            c.O = R08;
            c.Variant = 3;
            c.Floor(0, 24, 2);

            // 하단 8유닛: 폭 4 벽점프 굴뚝
            BuildResidueWall(c.Root.transform, "R08_Chimney_L", c.P(4f, 8f), new Vector2(1f, 8f));
            BuildResidueWall(c.Root.transform, "R08_Chimney_R", c.P(8f, 8f), new Vector2(1f, 8f));
            c.Tiles(2, 11, 10, 11);  // 구간 사이 안전 발판

            // 중단 10유닛: 왕복 이동 발판 2개
            BuildMovingPlatform(c.Root.transform, c.P(13f, 13f), new Vector2(4f, 0f), 4f);
            BuildMovingPlatform(c.Root.transform, c.P(13f, 17f), new Vector2(4f, 0f), 4f);
            c.Tiles(16, 22, 20, 21);

            // 상단 6유닛: 무너지는 발판 2개와 안전 벽면
            ResidueCrumbling(c.Root.transform, c.P(14f, 23f));
            ResidueCrumbling(c.Root.transform, c.P(18f, 25f));
            c.Tiles(20, 24, 26, 27); // 북동 출구 (22,26)

            // 승강기 도르래 2개를 "모두" 되감아야 R03 숏컷 B가 열린다.
            var pulleyA = ResidueRewindable(c.Root.transform, c.P(12f, 21f));
            var pulleyB = ResidueRewindable(c.Root.transform, c.P(19f, 26f));
            LinkRewindToShortcut(pulleyA, _shortcutB, pulleyB);
            LinkRewindToShortcut(pulleyB, _shortcutB, pulleyA);

            c.Room("Room08", 24f, 28f);
        }

        // ---------------- R09 끊어진 상층 고가교 (D4) ----------------
        // 전투 중 무엇을 먼저 복원할지 고르게 한다.
        static void BuildR09(RoomCtx c)
        {
            c.O = R09;
            c.Variant = 2;
            // 초반 관찰 발판과 중앙 전투 구간은 이어 둔다. 사이를 구덩이로 두면 낙하 → 위험 영역
            // 복귀 → 다시 낙하가 반복돼 주 동선이 성립하지 않는다(봇이 여기서 멈췄다).
            c.Floor(0, 24, 3);    // 초반 관찰 발판 + 중앙 전투 구간(14유닛)
            c.Floor(28, 32, 4);   // 출구 (32,4) — 4.5유닛 대시 선택 경로 너머

            var recovery = new GameObject("R09_Recovery");
            recovery.transform.SetParent(c.Root.transform, false);
            recovery.transform.position = c.P(4f, 4f);
            BuildHazard(c.Root.transform, c.P(16f, -1f), new Vector2(32f, 4f), 1, recovery.transform);

            var finger = BuildResidueEnemy(c.Root.transform, c.P(13f, 11f), ResidueEnemyKind.Finger);
            var carrier = BuildResidueEnemy(c.Root.transform, c.P(20f, 4f), ResidueEnemyKind.Carrier);
            var elite = BuildResidueEnemy(c.Root.transform, c.P(23f, 4f), ResidueEnemyKind.Hardened);

            // 복원 대상 둘을 서로 다른 자리에 둔다 — 다리는 안전한 주 동선, 벽은 정예 공략용.
            ResidueRewindable(c.Root.transform, c.P(25f, 4f)); // 다리
            ResidueRewindable(c.Root.transform, c.P(21f, 4f)); // 방어벽

            var reward = BuildRewardChest(c.Root.transform, "residue_r09_elite", c.P(24f, 4.5f), 15, false);
            BuildEncounter(c.Root.transform, "residue_r09", c.P(16f, 6f), new Vector2(20f, 10f), false,
                new[] { new[] { finger }, new[] { carrier }, new[] { elite } },
                new[] { 0, -1 }, reward, null);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(25.5f + i * 0.5f, 5f)); // 대시 선택 경로 보상

            c.Room("Room09", 32f, 16f);
        }

        // ---------------- R10 손목 감시탑 (D4, 중간 보스) ----------------
        static void BuildR10(RoomCtx c)
        {
            c.O = R10;
            c.Variant = 1;
            c.Floor(0, 24, 3);
            BuildResidueWall(c.Root.transform, "R10_Wall_L", c.P(1f, 7f), new Vector2(1f, 7f));
            BuildResidueWall(c.Root.transform, "R10_Wall_R", c.P(23f, 7f), new Vector2(1f, 7f));
            BuildSafePlatform(c.Root.transform, c.P(5f, 9f));
            BuildSafePlatform(c.Root.transform, c.P(19f, 9f));

            // 체크포인트 3은 전장 "바깥"이다 — 재진입 30초 이내 목표를 지키려면 문 앞이어야 한다.
            BuildCheckpoint(c.Root.transform, c.P(2f, 4f));

            // 좌우 복원 방어벽 — 돌진을 막는 데 쓴다.
            ResidueRewindable(c.Root.transform, c.P(8f, 4f));
            ResidueRewindable(c.Root.transform, c.P(16f, 4f));

            var boss = BuildBoss(c.Root.transform, c.P(18f, 5f), "Enemy_Residue_Watcher", 12,
                new[] { BossController.Move.GroundSweep, BossController.Move.Charge, BossController.Move.Slam },
                new[] { 0.5f }, new Color(0.66f, 0.5f, 0.45f));

            var reward = BuildRewardChest(c.Root.transform, "residue_r10_boss", c.P(12f, 4.5f), 40, false);
            // 입장 후 2초 관찰 뒤 출구 잠금. 승리하면 잠금 해제 + 숏컷 C + 큰 재화.
            BuildEncounter(c.Root.transform, "residue_r10_boss", c.P(12f, 7f), new Vector2(20f, 10f), true,
                new[] { new[] { boss } }, new int[0], reward, _shortcutC);

            c.Room("Room10", 24f, 18f);
        }

        // ---------------- R11 후회의 회랑 (D1→D3) ----------------
        // 보스 전 정적. 전투가 없고, 실패해도 아래 안전 통로로 우회한다.
        static void BuildR11(RoomCtx c)
        {
            c.O = R11;
            c.Variant = 2;
            c.Floor(0, 10, 3);
            c.Floor(10, 28, 1);   // 실패 시 우회하는 아래 통로
            c.Floor(22, 28, 4);   // 끝 6유닛 완전 안전지대, 출구 (28,4)

            // 숙련 간격 점프 2회(3.5~4유닛).
            BuildSafePlatform(c.Root.transform, c.P(13.5f, 4f));
            BuildSafePlatform(c.Root.transform, c.P(18f, 4f));

            BuildStoryFragment(c.Root.transform, c.P(6f, 4f), "residue_r11",
                "돌이킬 수 없다는 말은, 돌아보지 말라는 뜻이 아니었다.", EmotionId.None, false);

            // 보스 방향을 가리키는 교수대 실루엣.
            BuildResidueProp(c.Root.transform, "R11_GallowsSilhouette", c.P(26f, 4f), new Vector2(4f, 7f), "Prop_r2_c1");

            // S3 암시. 자각 + 균열 클리어 전에는 윤곽만 보인다.
            BuildDecor(c.Root.transform, "R11_S3_Hint", c.P(14f, 10f), new Vector2(2f, 2f),
                "Tile", new Color(0.32f, 0.3f, 0.4f));

            c.Room("Room11", 28f, 16f);
        }

        // ---------------- S3 감춰진 눈 (D0) ----------------
        // 자각 보유 + 균열 클리어로만 들어온다. 전투·낙하·시간제한 없음.
        static void BuildS3(RoomCtx c)
        {
            c.O = S3;
            c.Variant = 1;
            c.Floor(0, 20, 2);

            BuildDecor(c.Root.transform, "S3_Face", c.P(10f, 7f), new Vector2(8f, 6f),
                "Tile", new Color(0.28f, 0.26f, 0.33f));

            // 조건 게이트는 S3 안이 아니라 "들어오는 길목"(R11의 샤프트 입구)에 있다.
            // 여기 안쪽에 또 두면 최종 파편과 같은 자리에 겹쳐 파편이 지형에 파묻힌다.
            // 파편은 착지 지점(x=14)에서 조금 걸어 들어간 안쪽에 둔다.
            BuildStoryFragment(c.Root.transform, c.P(6f, 3f), "residue_final",
                "처음부터, 그것은 나를 보고 있었다.", EmotionId.None, false);

            c.Room("Secret03", 20f, 14f);
        }

        // ---------------- R12 기억의 교수대 (D5, 지역 보스) ----------------
        static void BuildR12(RoomCtx c)
        {
            c.O = R12;
            c.Variant = 1;
            c.Floor(0, 30, 3);
            BuildResidueWall(c.Root.transform, "R12_Wall_L", c.P(1f, 8f), new Vector2(1f, 8f));
            BuildResidueWall(c.Root.transform, "R12_Wall_R", c.P(29f, 8f), new Vector2(1f, 8f));

            // 천장 교수대 사슬 3개 — 2단계에서 보스가 이 중 하나에서 떨어진다.
            for (int i = 0; i < 3; i++)
                BuildResidueProp(c.Root.transform, $"R12_Chain_{i}", c.P(8f + i * 7f, 9f), new Vector2(1.5f, 7f), "Prop_r1_c4");

            // 바닥의 복원 가능한 안전 발판 2개. 3단계에서 하나는 다시 부서진다.
            ResidueRewindable(c.Root.transform, c.P(10f, 4f));
            ResidueRewindable(c.Root.transform, c.P(20f, 4f));

            var boss = BuildBoss(c.Root.transform, c.P(15f, 5f), "Enemy_Residue_Professor", 18,
                new[] { BossController.Move.GroundSweep, BossController.Move.Slam, BossController.Move.Charge },
                new[] { 0.6f, 0.3f }, new Color(0.5f, 0.42f, 0.55f));

            var reward = BuildRewardChest(c.Root.transform, "residue_r12_boss", c.P(15f, 4.5f), 60, true);
            BuildEncounter(c.Root.transform, "residue_r12_boss", c.P(15f, 8f), new Vector2(26f, 12f), true,
                new[] { new[] { boss } }, new int[0], reward, null);

            // 승리 후 열리는 응시 지역 통로.
            BuildZoneTrigger(c.Root.transform, c.P(28f, 5f), new Vector2(2f, 3f), false);

            c.Room("Room12", 30f, 18f);
            BuildBoundary(c.Root.transform, "Zone_EastBoundary", c.P(30.5f, 0f).x);
        }
    }
}
