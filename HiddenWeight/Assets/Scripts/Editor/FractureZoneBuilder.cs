using UnityEditor;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.World;

namespace HiddenWeight.EditorTools
{
    // 균열 지역 전체(주 동선 12룸 + 비밀 3룸)를 한 씬에 짓는다.
    // 명세: docs/FRACTURE_LEVEL_DESIGN.md, docs/WORLD_MAP.md 4장
    //
    // 방 골격과 좌표 규칙은 잔재·응시와 같다. 다른 것은 이 지역이 무엇을 믿게 하느냐다 —
    // 밝고 안전해 보이는 공간의 다음 상태를 예지로만 확인할 수 있고, 자각은 여기서
    // 아무것도 드러내지 않는다(ZoneData.awarenessStable=false가 이미 그렇게 만든다).
    //
    // 그래서 이 파일에는 자각으로 여는 문이 하나도 없다. 명세 1.2절이 "자각을 숨겨진 필수
    // 경로의 열쇠로 사용하지 않는다"를 못박고 있고, 비밀방 3개의 조건도 관찰·예지·반복
    // 관찰이지 자각이 아니다(5절).
    public static partial class ZoneSceneBuilder
    {
        static readonly Vector2Int F01 = new Vector2Int(0, 0);
        static readonly Vector2Int F02 = new Vector2Int(30, 0);
        static readonly Vector2Int F03 = new Vector2Int(62, 0);
        static readonly Vector2Int F04 = new Vector2Int(96, -16);
        static readonly Vector2Int FS1 = new Vector2Int(96, -30);
        static readonly Vector2Int F05 = new Vector2Int(124, -16);
        static readonly Vector2Int F06 = new Vector2Int(154, -16);
        static readonly Vector2Int FS2 = new Vector2Int(154, -32);
        static readonly Vector2Int F07 = new Vector2Int(190, -16);
        static readonly Vector2Int F08 = new Vector2Int(228, -14);
        static readonly Vector2Int F09 = new Vector2Int(256, 9);
        static readonly Vector2Int F10 = new Vector2Int(292, 10);
        static readonly Vector2Int F11 = new Vector2Int(320, 14);
        static readonly Vector2Int FS3 = new Vector2Int(320, 0);
        static readonly Vector2Int F12 = new Vector2Int(352, 14);

        // 파스텔 민트·라벤더·옅은 살구색(2.1절). 이전 두 지역보다 확실히 밝게 둔다 —
        // "밝아서 안심함"이 이 지역의 첫 감정이기 때문이다.
        static readonly Color FractureTerrain = new Color(0.72f, 0.86f, 0.82f);
        static readonly Color FractureStone = new Color(0.78f, 0.75f, 0.88f);
        static readonly Color FractureGhost = new Color(0.95f, 0.97f, 1f);
        static readonly Color FracturePeach = new Color(0.96f, 0.82f, 0.72f);

        // 붕괴 발판은 반드시 스스로 되살아나야 한다. 이 지역에는 되감기가 없어서
        // respawnDelay가 0이면 한 번 무너진 발판이 영영 돌아오지 않고, 그러면 10절의
        // "발판 위상은 사망 후 항상 같은 시작값으로 돌아간다"가 성립하지 않는다.
        const float FractureRespawn = 3f;

        static Shortcut _fractureShortcutA;
        static Shortcut _fractureShortcutB;
        static Shortcut _fractureShortcutC;
        static Shortcut _fractureSecretDoor;   // FS3 입구를 막는 벽. 미래 문이 확정되면 열린다

        // ------------------------------------------------------------
        // 균열 전용 조립 헬퍼
        // ------------------------------------------------------------

        static GameObject BuildFractureCrumbling(Transform parent, Vector2 pos)
        {
            var go = BuildCrumblingPlatform(parent, pos);
            var platform = go.GetComponent<CrumblingPlatform>();
            if (platform != null)
                SetField(platform, "respawnDelay", p => p.floatValue = FractureRespawn);

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            // 겉모습으로는 안전한 발판과 구분되지 않아야 한다(2.1절: "평상시 금이 없거나
            // 매우 약하게 보여 외형만으로 구분할 수 없게 한다"). 그래서 색까지 똑같이 둔다.
            if (sr != null) sr.color = FractureStone;
            return go;
        }

        static GameObject BuildFractureSafePlatform(Transform parent, Vector2 pos)
        {
            var go = BuildSafePlatform(parent, pos);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = FractureStone;
            return go;
        }

        // 회전 발판(시계바늘). pivot은 시작 위치 기준 상대 중심이다.
        static OrbitPlatform BuildOrbitPlatform(Transform parent, string name, Vector2 pos, Vector2 pivot,
                                                float degreesPerSecond, float phase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.layer = LayerMask.NameToLayer("Ground");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Platform");
            sr.color = FractureStone;
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            go.AddComponent<Rigidbody2D>();

            var orbit = go.AddComponent<OrbitPlatform>();
            SetField(orbit, "pivot", p => p.vector2Value = pivot);
            SetField(orbit, "degreesPerSecond", p => p.floatValue = degreesPerSecond);
            SetField(orbit, "phaseOffset", p => p.floatValue = phase);
            return orbit;
        }

        // 예지 안에서만 보이는 미래 구조물. sightingsToFix번 보고 나면 현재에 고정된다.
        static FutureEcho BuildFutureEcho(Transform parent, string name, Vector2 center, Vector2 size,
                                          int sightingsToFix, Shortcut linkedShortcut, bool solidWhenFixed,
                                          Vector2 drift = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            if (solidWhenFixed) go.layer = LayerMask.NameToLayer("Ground");

            var visual = new GameObject("FutureVisual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Tile");
            sr.color = FractureGhost;
            sr.sortingOrder = 3;

            BoxCollider2D col = null;
            if (solidWhenFixed)
            {
                col = go.AddComponent<BoxCollider2D>();
                col.size = size;
            }

            var echo = go.AddComponent<FutureEcho>();
            SetField(echo, "futureVisual", p => p.objectReferenceValue = sr);
            SetField(echo, "sightingsToFix", p => p.intValue = sightingsToFix);
            SetField(echo, "futureDrift", p => p.vector2Value = drift);
            if (col != null) SetField(echo, "solid", p => p.objectReferenceValue = col);
            if (linkedShortcut != null)
                SetField(echo, "linkedShortcut", p => p.objectReferenceValue = linkedShortcut);
            return echo;
        }

        // 열리면 "사라지는" 문. 공용 BuildShortcut은 닫힘·열림 양쪽에 solid 블록을 두므로
        // (다리·승강기처럼 열려야 밟히는 구조물이 기본이라) 구멍을 막는 문에는 쓸 수 없다.
        // 여기서는 blocker 하나만 두고 openedVisual을 비워, 열리면 그 자리가 그대로 통로가 된다.
        static Shortcut BuildBlockingDoor(Transform parent, string id, Vector2 center, Vector2 size, Color tint)
        {
            var go = new GameObject($"Door_{id}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var blocker = BuildSolidBlock(go.transform, "Blocker", center, size, "Ground", tint);

            var shortcut = go.AddComponent<Shortcut>();
            SetField(shortcut, "shortcutId", p => p.stringValue = id);
            SetField(shortcut, "blocker", p => p.objectReferenceValue = blocker);
            return shortcut;
        }

        // 갈림길. 플레이어가 처음 들어선 갈래만 실제 발판이 된다(F12 마지막 단계).
        static PathChoice BuildPathChoice(Transform parent, string name, Vector2 origin,
                                          (Vector2 entry, Vector2 platform)[] branches)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(origin.x, origin.y, 0f);

            var choice = go.AddComponent<PathChoice>();
            var entries = new Transform[branches.Length];
            var solids = new GameObject[branches.Length];
            var previews = new SpriteRenderer[branches.Length];

            for (int i = 0; i < branches.Length; i++)
            {
                var entry = new GameObject($"Entry_{i}");
                entry.transform.SetParent(go.transform, false);
                entry.transform.position = new Vector3(branches[i].entry.x, branches[i].entry.y, 0f);
                entries[i] = entry.transform;

                var solid = BuildSolidBlock(go.transform, $"Branch_{i}",
                    branches[i].platform, new Vector2(6f, 0.6f), "Ground", FractureStone);
                solid.SetActive(false);
                solids[i] = solid;

                var preview = new GameObject($"Preview_{i}");
                preview.transform.SetParent(go.transform, false);
                preview.transform.position = new Vector3(branches[i].platform.x, branches[i].platform.y, 0f);
                preview.transform.localScale = new Vector3(6f, 0.6f, 1f);
                var sr = preview.AddComponent<SpriteRenderer>();
                sr.sprite = LoadSprite("Tile");
                sr.color = FractureGhost;
                sr.sortingOrder = 4;
                previews[i] = sr;
            }

            SetField(choice, "branches", p =>
            {
                p.arraySize = branches.Length;
                for (int i = 0; i < branches.Length; i++)
                {
                    var element = p.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("entry").objectReferenceValue = entries[i];
                    element.FindPropertyRelative("solid").objectReferenceValue = solids[i];
                    element.FindPropertyRelative("preview").objectReferenceValue = previews[i];
                }
            });
            return choice;
        }

        // ------------------------------------------------------------
        // 균열 적 구성 (FRACTURE_LEVEL_DESIGN.md 6절)
        // ------------------------------------------------------------

        public enum FractureEnemyKind { Sprout, Precursor, Collector, SplitSelf }

        static EnemyData FractureEnemyData(FractureEnemyKind kind)
        {
            string name = $"Enemy_Fracture_{kind}";
            var existing = LoadData<EnemyData>(name);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<EnemyData>();
            // 이름 붙은 균열 몬스터 4종은 전부 EnemyPatrol을 꺼버리고 시간 함수로 직접 움직이므로
            // wobbleAmplitude(EnemyPatrol 전용 수직 흔들림)는 적용되지 않는다(Enemies/README.md 참고).
            // 다른 지역과 같이 0으로 둔다.

            switch (kind)
            {
                case FractureEnemyKind.Sprout: // 불안 새싹 — 결정론적 왕복 + 가짜 방향 전환
                    data.maxHealth = 2; data.moveSpeed = 1.5f; data.contactDamage = 1;
                    data.tint = new Color(0.62f, 0.86f, 0.72f);
                    data.patrolWidth = 5f; data.patrolPeriod = 5f; data.feintSeconds = 0.6f;
                    break;

                case FractureEnemyKind.Precursor: // 선행 그림자 — 미래 타격 지점이 먼저 나타난다
                    data.maxHealth = 3; data.moveSpeed = 0f; data.contactDamage = 1;
                    data.tint = new Color(0.7f, 0.66f, 0.86f);
                    data.detectRange = 8f;
                    data.leadSeconds = 2f;
                    data.recoverSeconds = 1.2f;
                    break;

                case FractureEnemyKind.Collector: // 가능성 수집자 — 착지 예정 지점에 지연 폭발
                    data.maxHealth = 3; data.moveSpeed = 0f; data.contactDamage = 1;
                    data.tint = new Color(0.95f, 0.78f, 0.66f);
                    data.detectRange = 12f;
                    data.blastFuse = 2f; data.blastRadius = 1.8f;
                    break;

                case FractureEnemyKind.SplitSelf: // 갈라진 자아 — 둘 중 하나만 실체
                    data.maxHealth = 8; data.moveSpeed = 1.6f; data.contactDamage = 1;
                    data.tint = new Color(0.55f, 0.52f, 0.72f);
                    data.detectRange = 9f;
                    data.leadSeconds = 2f;
                    break;
            }

            AssetDatabase.CreateAsset(data, $"{DataFolder}/{name}.asset");
            return data;
        }

        static GameObject BuildFractureEnemy(Transform parent, Vector2 pos, FractureEnemyKind kind)
        {
            var go = BuildEnemy(parent, pos, FractureEnemyData(kind));
            int playerMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");

            switch (kind)
            {
                case FractureEnemyKind.Sprout:
                    go.AddComponent<FeintPatrol>();
                    break;

                case FractureEnemyKind.Precursor:
                {
                    var precursor = go.AddComponent<PrecursorBehavior>();
                    SetField(precursor, "playerMask", p => p.intValue = playerMask);
                    SetField(precursor, "markerSprite", p => p.objectReferenceValue = LoadSprite("Tile"));
                    break;
                }

                case FractureEnemyKind.Collector:
                {
                    var collector = go.AddComponent<CollectorBehavior>();
                    SetField(collector, "playerMask", p => p.intValue = playerMask);
                    SetField(collector, "blastSprite", p => p.objectReferenceValue = LoadSprite("Tile"));
                    break;
                }

                case FractureEnemyKind.SplitSelf:
                    // 거울상·그림자·대칭축은 방마다 달라서 ConfigureSplitSelf가 따로 물려 준다.
                    go.AddComponent<SplitSelfBehavior>();
                    break;
            }

            return go;
        }

        // 갈라진 자아에 거울상과 두 그림자를 붙인다. 거울상은 콜라이더가 없는 그림이라
        // 때려도 아무 일이 일어나지 않는다 — 판별 자체가 이 전투다.
        static void ConfigureSplitSelf(GameObject enemy, Transform parent, float mirrorAxisX)
        {
            var split = enemy.GetComponent<SplitSelfBehavior>();
            if (split == null) return;

            var mirror = new GameObject("SplitSelf_Mirror");
            mirror.transform.SetParent(parent, false);
            mirror.transform.position = enemy.transform.position;

            var mirrorSr = mirror.AddComponent<SpriteRenderer>();
            mirrorSr.sprite = LoadSprite("Enemy");
            mirrorSr.color = FractureEnemyData(FractureEnemyKind.SplitSelf).tint;
            mirrorSr.sortingOrder = 7;

            var mirrorShadow = new GameObject("Shadow");
            mirrorShadow.transform.SetParent(mirror.transform, false);
            mirrorShadow.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            mirrorShadow.transform.localScale = new Vector3(1.4f, 0.3f, 1f);
            var mirrorShadowSr = mirrorShadow.AddComponent<SpriteRenderer>();
            mirrorShadowSr.sprite = LoadSprite("Tile");
            mirrorShadowSr.sortingOrder = 4;

            var bodyShadow = new GameObject("Shadow");
            bodyShadow.transform.SetParent(enemy.transform, false);
            bodyShadow.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            bodyShadow.transform.localScale = new Vector3(1.4f, 0.3f, 1f);
            var bodyShadowSr = bodyShadow.AddComponent<SpriteRenderer>();
            bodyShadowSr.sprite = LoadSprite("Tile");
            bodyShadowSr.sortingOrder = 4;

            SetField(split, "mirror", p => p.objectReferenceValue = mirror.transform);
            SetField(split, "bodyShadow", p => p.objectReferenceValue = bodyShadowSr);
            SetField(split, "mirrorShadow", p => p.objectReferenceValue = mirrorShadowSr);
            SetField(split, "mirrorAxisX", p => p.floatValue = mirrorAxisX);
        }

        // ------------------------------------------------------------
        // 방 연결
        // ------------------------------------------------------------

        static void BuildFractureConnections(RoomCtx c)
        {
            var map = c.Map;
            var parent = c.Root.transform;

            var links = new (Vector2Int from, Vector2 exit, Vector2Int to, Vector2 entry, string name)[]
            {
                (F01, new Vector2(26, 2), F02, new Vector2(0, 2), "FC_F01_F02"),
                (F02, new Vector2(28, 3), F03, new Vector2(0, 3), "FC_F02_F03"),
                (F03, new Vector2(28, 2), F04, new Vector2(2, 18), "FC_F03_F04"),
                (F04, new Vector2(22, 2), F05, new Vector2(0, 2), "FC_F04_F05"),
                (F05, new Vector2(26, 2), F06, new Vector2(0, 2), "FC_F05_F06"),
                (F06, new Vector2(32, 4), F07, new Vector2(0, 4), "FC_F06_F07"),
                (F07, new Vector2(34, 4), F08, new Vector2(2, 2), "FC_F07_F08"),
                (F08, new Vector2(22, 26), F09, new Vector2(0, 3), "FC_F08_F09"),
                (F09, new Vector2(32, 4), F10, new Vector2(0, 3), "FC_F09_F10"),
                (F10, new Vector2(24, 7), F11, new Vector2(0, 3), "FC_F10_F11"),
                (F11, new Vector2(28, 4), F12, new Vector2(0, 4), "FC_F11_F12"),
            };

            foreach (var link in links)
            {
                float exitX = link.from.x + link.exit.x;
                float exitY = link.from.y + link.exit.y;
                float entryX = link.to.x + link.entry.x;
                float entryY = link.to.y + link.entry.y;

                if (!Mathf.Approximately(exitY, entryY))
                    Debug.LogWarning($"[FractureZoneBuilder] {link.name} 높이 불일치: 출구 y={exitY}, 입구 y={entryY}");

                BuildCorridor(parent, map, link.name, exitX, entryX, Mathf.RoundToInt(exitY));
            }

            // FS1 — F04 하층 바닥의 뚫린 자리 아래. 흔들리는 화단이 주기적으로 비켜야 보인다.
            BuildShaft(parent, map, "Shaft_FS1", F04.x + 8, F04.y + 0, FS1.y + 2);

            // FS2 — F06 바닥의 뚫린 자리 아래. 역방향으로 가는 발판을 따라가야 닿는다.
            BuildShaft(parent, map, "Shaft_FS2", F06.x + 4, F06.y + 0, FS2.y + 2);

            // FS3 — F11 바닥의 뚫린 자리 아래. 세 번 본 미래 문이 현재에 고정되면 열린다.
            BuildShaft(parent, map, "Shaft_FS3", F11.x + 9, F11.y + 1, FS3.y + 2);
        }

        [MenuItem("Hidden Weight/Build Fracture Zone (Full)")]
        public static void RunFractureZone()
        {
            EnsureScenesFolder();

            var scene = NewScene();
            var tilemap = BuildZoneRoot("Fracture", out var root);
            TintTerrain(tilemap, FractureTerrain);

            var rooms = new GameObject("Rooms");
            rooms.transform.SetParent(root.transform, true);

            var marker = new GameObject("ZoneMarker");
            marker.transform.SetParent(root.transform, false);
            var zoneMarker = marker.AddComponent<HiddenWeight.Core.ZoneMarker>();
            SetField(zoneMarker, "zone", p => p.enumValueIndex = (int)ZoneId.Fracture);

            var ctx = new RoomCtx { Map = tilemap, Root = root, Rooms = rooms.transform, FloorArt = false };

            BuildF01(ctx);
            BuildF02(ctx);
            BuildF03(ctx);
            BuildF04(ctx);
            BuildFS1(ctx);
            BuildF05(ctx);
            BuildF06(ctx);
            BuildFS2(ctx);
            BuildF07(ctx);
            BuildF08(ctx);
            BuildF09(ctx);
            BuildF10(ctx);
            BuildF11(ctx);
            BuildFS3(ctx);
            BuildF12(ctx);

            BuildFractureConnections(ctx);

            ctx.O = F01;
            PlacePlayerAndCamera(root, new Vector3(F01.x + 3f, F01.y + 3f, 0f));

            SaveScene(scene, "Zone_Fracture_Full");
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FractureZoneBuilder] 균열 전체 지역(15룸) 생성 완료");
        }

        // ---------------- F01 유리 정원 (D0) ----------------
        // 밝기 전환과 불신의 씨앗. 필수 진행에 붕괴도 낙사도 없다(4.1절).
        static void BuildF01(RoomCtx c)
        {
            c.O = F01;
            c.Floor(0, 26, 2);

            BuildCheckpoint(c.Root.transform, c.P(5f, 3f));

            // 자각으로 반응할 것 같은 이중 윤곽. 자각을 켜도 아무 변화가 없다 —
            // AwarenessRevealed를 일부러 쓰지 않는다. 여기 있는 것은 그냥 장식이다.
            for (int i = 0; i < 3; i++)
            {
                float x = 9f + i * 4f;
                BuildDecor(c.Root.transform, $"F01_DoubleEdge_{i}", c.P(x, 4.5f), new Vector2(1.6f, 3f),
                    "Gate", FractureStone, 0f, -5);
                BuildDecor(c.Root.transform, $"F01_DoubleEdge_{i}_Echo", c.P(x + 0.25f, 4.6f), new Vector2(1.6f, 3f),
                    "Gate", new Color(1f, 1f, 1f, 0.35f), 0f, -6);
            }

            // 하늘의 세로 균열 — 지역 전체의 진행 방향이자 결말(2.3절 랜드마크).
            BuildDecor(c.Root.transform, "F01_SkyFracture", c.P(21f, 12f), new Vector2(0.8f, 16f),
                "Tile", new Color(0.9f, 0.86f, 0.95f), 6f, -8);

            // 방 끝에서, 닿기도 전에 스스로 무너져 있는 발판 조각을 보여준다(4.1절).
            BuildDecor(c.Root.transform, "F01_BrokenPlatform_A", c.P(22f, 6f), new Vector2(1.4f, 0.5f),
                "Platform", FractureStone, -18f, -3);
            BuildDecor(c.Root.transform, "F01_BrokenPlatform_B", c.P(23.6f, 5.2f), new Vector2(1f, 0.5f),
                "Platform", FractureStone, 24f, -3);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(10f + i * 1.5f, 3.2f));

            c.Room("FractureRoom01", 26f, 14f);
            BuildBoundary(c.Root.transform, "Fracture_WestBoundary", c.P(-0.5f, 0f).x);
        }

        // ---------------- F02 어긋난 산책로 (D1→D2) ----------------
        // 첫 거짓 안전. 상부 길은 완전히 안전해 보이지만 차례로 무너진다(4.2절).
        static void BuildF02(RoomCtx c)
        {
            c.O = F02;
            c.Floor(0, 8, 2);
            c.Floor(8, 17, 1);   // 하부 안전 우회로 — 15초 안에 주 동선으로 돌아온다
            c.Floor(17, 20, 3);
            c.Floor(20, 22, 1);
            c.Floor(22, 28, 3);  // 출구 (28,3)

            // 상부 길 발판 3개. 안전 발판과 같은 그림·같은 색이라 외형으로는 구분되지 않는다.
            BuildFractureCrumbling(c.Root.transform, c.P(9.5f, 4f));
            BuildFractureCrumbling(c.Root.transform, c.P(13f, 4f));
            BuildFractureCrumbling(c.Root.transform, c.P(16.5f, 4f));

            BuildFractureEnemy(c.Root.transform, c.P(5f, 3f), FractureEnemyKind.Sprout);
            BuildFractureEnemy(c.Root.transform, c.P(24f, 4f), FractureEnemyKind.Sprout);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(12f + i * 0.6f, 5.5f));
            BuildHealingPickup(c.Root.transform, c.P(26.5f, 4f));

            c.Room("FractureRoom02", 28f, 16f);
        }

        // ---------------- F03 가능성 광장 (D1) ----------------
        // 중앙 허브. 숏컷 A는 문틀만 있고 문짝이 미래에 있으며, 숏컷 B의 승강기는 거꾸로 움직인다.
        static void BuildF03(RoomCtx c)
        {
            c.O = F03;
            c.Floor(0, 20, 3);
            c.Floor(20, 30, 2);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(21f + i * 2f, 3.2f));

            BuildFractureEnemy(c.Root.transform, c.P(15f, 4f), FractureEnemyKind.Sprout);

            _fractureShortcutA = BuildShortcut(c.Root.transform, "fracture_shortcut_a", c.P(5f, 9f),
                new Vector2(4f, 0.6f), new Color(0.85f, 0.8f, 0.95f));
            _fractureShortcutB = BuildShortcut(c.Root.transform, "fracture_shortcut_b", c.P(24f, 7f),
                new Vector2(3.5f, 0.6f), new Color(0.7f, 0.9f, 0.85f));

            // 숏컷 A의 문틀. 문짝은 미래 위치에서 깜빡이고, F05에서 예지로 확정된다.
            BuildDecor(c.Root.transform, "F03_DoorFrame", c.P(5f, 10.5f), new Vector2(4.4f, 3f),
                "Gate", FractureStone, 0f, -4);

            // 랜드마크: 떠 있는 시계탑과 하늘 균열을 중앙에서 동시에 본다(4.3절).
            BuildDecor(c.Root.transform, "F03_ClockTower", c.P(10f, 13f), new Vector2(3.4f, 12f),
                "Tile", new Color(0.86f, 0.83f, 0.94f), -3f, -7);
            BuildDecor(c.Root.transform, "F03_InvertedGreenhouse", c.P(20f, 14f), new Vector2(6f, 5f),
                "Tile", new Color(0.8f, 0.92f, 0.88f), 184f, -7);
            BuildDecor(c.Root.transform, "F03_SkyFracture", c.P(27f, 14f), new Vector2(0.8f, 14f),
                "Tile", new Color(0.9f, 0.86f, 0.95f), 5f, -8);

            c.Room("FractureRoom03", 30f, 18f);
        }

        // ---------------- F04 흔들리는 하층정원 (D2) ----------------
        // 예지가 아직 없다. 필수 점프는 두 주기만 관찰하면 통과할 수 있게 단순화한다(4.4절).
        static void BuildF04(RoomCtx c)
        {
            c.O = F04;
            c.Floor(0, 8, 18);        // 상층 관찰대 (입구 2,18)
            c.Floor(0, 8, 2, 2);      // 하층 안전 바닥 — 로컬 8~9를 비워 FS1 통로를 만든다
            c.Floor(9, 24, 2, 2);

            // 좌우로 천천히 흔들리는 화단. 느린 주기라 눈으로 읽을 수 있다.
            BuildMovingPlatform(c.Root.transform, c.P(12f, 15f), new Vector2(2f, 0f), 6f);
            BuildMovingPlatform(c.Root.transform, c.P(15f, 11f), new Vector2(-2f, 0f), 6f);
            BuildMovingPlatform(c.Root.transform, c.P(12f, 7f), new Vector2(2f, 0f), 6f);
            BuildFractureSafePlatform(c.Root.transform, c.P(18f, 5f));

            // FS1 입구는 "배경 기둥과 실제 통로가 한 번 정렬되는" 순간에만 열린다(4.4절).
            // 왕복 화단이 구멍 위를 지날 때는 막히고, 비켜났을 때만 내려갈 수 있다 —
            // 능력이 아니라 반복 주기 관찰로 찾는 비밀방이다.
            BuildMovingPlatform(c.Root.transform, c.P(8.5f, 2.4f), new Vector2(3.5f, 0f), 7f);
            BuildDecor(c.Root.transform, "F04_FS1_Pillar", c.P(8.5f, 6f), new Vector2(1f, 7f),
                "Tile", new Color(0.8f, 0.78f, 0.9f), 0f, -6);

            BuildFractureEnemy(c.Root.transform, c.P(4f, 3f), FractureEnemyKind.Sprout);
            BuildFractureEnemy(c.Root.transform, c.P(19f, 3f), FractureEnemyKind.Sprout);

            BuildStoryFragment(c.Root.transform, c.P(21.5f, 3f), "fracture_f04",
                "흔들리는 것은 정원이 아니라, 그것을 보는 쪽이었다.", EmotionId.None, false);

            c.Room("FractureRoom04", 24f, 22f);
        }

        // ---------------- FS1 버려진 가능성 (D2 선택) ----------------
        static void BuildFS1(RoomCtx c)
        {
            c.O = FS1;
            c.Floor(0, 18, 2);

            BuildFractureSafePlatform(c.Root.transform, c.P(5f, 5f));
            BuildFractureSafePlatform(c.Root.transform, c.P(9.5f, 7f));
            BuildFractureSafePlatform(c.Root.transform, c.P(14f, 8f));

            // 완성되지 못한 미래들의 잔해.
            for (int i = 0; i < 4; i++)
                BuildDecor(c.Root.transform, $"FS1_Unbuilt_{i}", c.P(3f + i * 4f, 4f), new Vector2(1.2f, 4f),
                    "Gate", new Color(0.86f, 0.84f, 0.94f, 0.7f), i * 3f - 5f, -6);

            BuildStoryFragment(c.Root.transform, c.P(14f, 9.5f), "fracture_fs1",
                "고르지 않은 쪽에도 문은 있었다.", EmotionId.None, false);
            BuildRewardChest(c.Root.transform, "fracture_fs1_currency", c.P(16f, 3f), 30, false);

            c.Room("FractureSecret01", 18f, 14f);
        }

        // ---------------- F05 예지 성소 (D0→D1) ----------------
        // 핵심 능력 획득. 세 대상을 순서대로 확인하고, 마지막에 숏컷 A가 현재에 고정된다(4.5절).
        static void BuildF05(RoomCtx c)
        {
            c.O = F05;
            c.Floor(0, 26, 2);

            BuildCheckpoint(c.Root.transform, c.P(3f, 3f)); // 체크포인트 2

            BuildStoryFragment(c.Root.transform, c.P(6f, 3f), "fracture_skill",
                "아직 오지 않은 것들이, 이미 나를 흔든다.", EmotionId.Foresight, false);
            BuildTutorialHint(c.Root.transform, c.P(7f, 5f), "K 탭  —  예지");

            // 1) 움직이는 발판의 2초 뒤 위치.
            BuildMovingPlatform(c.Root.transform, c.P(11f, 4f), new Vector2(4f, 0f), 4f);

            // 2) 곧 사라질 붕괴 발판 — 고스트가 나타나지 않는 것이 경고임을 배운다.
            BuildFractureCrumbling(c.Root.transform, c.P(16f, 4f));
            BuildFractureSafePlatform(c.Root.transform, c.P(19.5f, 4f));

            // 3) 적이 아니라 안전한 기계 장치로 미래 활성 위치를 본다.
            BuildFutureEcho(c.Root.transform, "F05_Machine", c.P(22f, 5f), new Vector2(2f, 2f),
                0, null, false, new Vector2(0.6f, 0.4f));

            // 마지막: 미래 문짝이 현재 문틀과 맞물려 숏컷 A가 실제 문이 된다.
            // 예지를 한 번 쓰면 확정되므로, 능력을 쓴 것 자체가 문을 여는 행동이 된다.
            BuildFutureEcho(c.Root.transform, "F05_FutureDoor", c.P(24.5f, 4f), new Vector2(2.4f, 4f),
                1, _fractureShortcutA, false);

            c.Room("FractureRoom05", 26f, 14f);
        }

        // ---------------- F06 시차 온실 (D2) ----------------
        // 서로 다른 주기의 이동 발판과 미래 타격. 필수 예측 대상은 화면에 최대 3개(4.6절).
        static void BuildF06(RoomCtx c)
        {
            c.O = F06;
            // 로컬 4~5를 비워 FS2로 내려가는 자리를 만든다. 얕게(깊이 2) 판다.
            c.Floor(0, 4, 2, 2);
            c.Floor(5, 24, 2, 2);
            c.Floor(24, 28, 3, 2);
            c.Floor(28, 32, 4, 2);  // 출구 (32,4)

            // FS2 — 주 동선(오른쪽)과 반대로 움직이는 발판의 미래 위치를 따라가야 닿는다.
            // 발판이 왼쪽 끝에 있을 때만 구멍 위를 비켜 준다.
            BuildMovingPlatform(c.Root.transform, c.P(4.5f, 2.4f), new Vector2(-3f, 0f), 6f);
            BuildDecor(c.Root.transform, "F06_FS2_Marker", c.P(4.5f, 6f), new Vector2(1.2f, 1.2f),
                "Gate", FracturePeach, 0f, -4);

            // 서로 다른 주기의 이동 발판 3개(3초 / 5초 / 7초).
            BuildMovingPlatform(c.Root.transform, c.P(9f, 6f), new Vector2(3f, 0f), 3f);
            BuildMovingPlatform(c.Root.transform, c.P(15f, 7f), new Vector2(3f, 0f), 5f);
            BuildMovingPlatform(c.Root.transform, c.P(21f, 6f), new Vector2(3f, 0f), 7f);

            BuildFractureEnemy(c.Root.transform, c.P(12f, 3f), FractureEnemyKind.Precursor);
            BuildFractureEnemy(c.Root.transform, c.P(22f, 3f), FractureEnemyKind.Precursor);

            // 거꾸로 선 온실 — F03에서 보이던 랜드마크에 여기서 도달한다(2.3절).
            BuildDecor(c.Root.transform, "F06_Greenhouse", c.P(16f, 12f), new Vector2(18f, 6f),
                "Tile", new Color(0.8f, 0.92f, 0.88f), 182f, -7);

            BuildRewardChest(c.Root.transform, "fracture_f06_material", c.P(30f, 5f), 20, false);

            c.Room("FractureRoom06", 32f, 16f);
        }

        // ---------------- FS2 멈춘 오후 (D3 선택) ----------------
        static void BuildFS2(RoomCtx c)
        {
            c.O = FS2;
            c.Floor(0, 24, 2);

            // 멈춰 있는 오후 — 아무것도 움직이지 않는 유일한 방.
            for (int i = 0; i < 5; i++)
                BuildDecor(c.Root.transform, $"FS2_StoppedThing_{i}", c.P(3f + i * 4.5f, 5f), new Vector2(1.4f, 3f),
                    "Tile", new Color(0.92f, 0.88f, 0.8f), 0f, -6);

            BuildFractureSafePlatform(c.Root.transform, c.P(8f, 6f));
            BuildFractureSafePlatform(c.Root.transform, c.P(14f, 8f));

            BuildRewardChest(c.Root.transform, "fracture_fs2_shard", c.P(20f, 3f), 0, true);

            c.Room("FractureSecret02", 24f, 16f);
        }

        // ---------------- F07 부유 건축군 (D3) ----------------
        // 이동과 전투의 결합. 실패하면 시작점이 아니라 가장 최근의 넓은 건축물로 복귀한다(4.7절).
        static void BuildF07(RoomCtx c)
        {
            c.O = F07;
            c.Floor(0, 8, 4);
            c.Floor(14, 20, 4);    // 중간의 넓은 건축물 = 복귀 지점
            c.Floor(28, 34, 4);    // 출구 (34,4)

            // 수평·수직 경로가 교차하지만 실제 충돌 시점은 겹치지 않는다.
            BuildMovingPlatform(c.Root.transform, c.P(10f, 5f), new Vector2(0f, 4f), 4f);
            BuildOrbitPlatform(c.Root.transform, "F07_Orbit_A", c.P(24f, 5f), new Vector2(0f, 3f), 60f, 0f);
            BuildMovingPlatform(c.Root.transform, c.P(24f, 10f), new Vector2(3f, 0f), 5f);

            // 곧 사라지는 착지면. 예지로 보면 고스트가 없다.
            BuildFractureCrumbling(c.Root.transform, c.P(12f, 6f));
            BuildFractureCrumbling(c.Root.transform, c.P(26f, 6f));

            BuildFractureEnemy(c.Root.transform, c.P(17f, 5f), FractureEnemyKind.Sprout);
            BuildFractureEnemy(c.Root.transform, c.P(30f, 5f), FractureEnemyKind.Collector);

            // 아래로 떨어지면 체력 1을 잃고 중간 건축물로 되돌아간다.
            var recovery = BuildRetreatPoint(c.Root.transform, "F07_Recovery", c.P(17f, 5f));
            BuildHazard(c.Root.transform, c.P(17f, -3f), new Vector2(34f, 4f), 1, recovery);

            BuildRewardChest(c.Root.transform, "fracture_f07_currency", c.P(24f, 13f), 30, false);

            // 숏컷 C는 F07 쪽에 놓인 시계탑 긴 바늘 다리다. 여는 것은 F10 중간 보스 승리다.
            _fractureShortcutC = BuildShortcut(c.Root.transform, "fracture_shortcut_c", c.P(32f, 11f),
                new Vector2(4f, 0.6f), new Color(0.9f, 0.85f, 0.7f));

            c.Room("FractureRoom07", 34f, 18f);
        }

        // ---------------- F08 역행 승강축 (D2) ----------------
        // 승강기가 먼저 아래로 내려갔다가 상층으로 오른다. 층마다 안전 포켓을 둔다(4.8절).
        static void BuildF08(RoomCtx c)
        {
            c.O = F08;
            c.Floor(0, 24, 2);

            c.Tiles(2, 8, 12, 13);
            c.Tiles(16, 22, 20, 21);
            c.Tiles(18, 24, 25, 26);   // 북동 출구 (22,26)

            // 첫 웨이포인트가 아래를 향한다 — 이것이 "역행"의 전부다.
            BuildLift(c.Root.transform, "F08_Lift", c.P(6f, 4f),
                new[] { new Vector2(0f, -1f), new Vector2(0f, 10f), new Vector2(9f, 22f) },
                _fractureShortcutB, new Color(0.7f, 0.9f, 0.85f));

            BuildFractureSafePlatform(c.Root.transform, c.P(12f, 16f));
            BuildFractureSafePlatform(c.Root.transform, c.P(12f, 23f));

            BuildDecor(c.Root.transform, "F08_NoDoorYet", c.P(20f, 30f), new Vector2(3f, 5f),
                "Gate", new Color(0.95f, 0.93f, 1f, 0.5f), 0f, -7);

            BuildHealingPickup(c.Root.transform, c.P(20f, 27f));

            c.Room("FractureRoom08", 24f, 28f);
        }

        // ---------------- F09 거울 가능성실 (D4, 정예) ----------------
        // 좌우 대칭 중 한쪽만 유지된다. 예지로 확인할 수 있지만 선택은 플레이어의 몫이다(4.9절).
        static void BuildF09(RoomCtx c)
        {
            c.O = F09;
            c.Floor(0, 28, 3);
            c.Floor(28, 32, 4);   // 출구 (32,4)

            // 정예 조우 전에 방 전체를 내려다보는 관찰대.
            c.Tiles(2, 8, 9, 10);

            // 좌우 대칭 경로. 위쪽 줄은 유지되고 아래쪽 줄은 같은 시점에 사라진다 —
            // 겉모습이 같으므로 예지로만 미리 구분된다.
            for (int i = 0; i < 3; i++)
            {
                BuildFractureSafePlatform(c.Root.transform, c.P(11f + i * 4f, 8f));
                BuildFractureCrumbling(c.Root.transform, c.P(11f + i * 4f, 5f));
            }

            BuildDecor(c.Root.transform, "F09_MirrorAxis", c.P(16f, 8f), new Vector2(0.4f, 12f),
                "Tile", FractureGhost, 0f, -4);

            var sproutA = BuildFractureEnemy(c.Root.transform, c.P(10f, 4f), FractureEnemyKind.Sprout);
            var collector = BuildFractureEnemy(c.Root.transform, c.P(21f, 9f), FractureEnemyKind.Collector);

            var split = BuildFractureEnemy(c.Root.transform, c.P(20f, 4f), FractureEnemyKind.SplitSelf);
            ConfigureSplitSelf(split, c.Root.transform, c.P(16f, 0f).x);

            var reward = BuildRewardChest(c.Root.transform, "fracture_f09_elite", c.P(25f, 4.5f), 40, false);
            BuildEncounter(c.Root.transform, "fracture_f09", c.P(16f, 7f), new Vector2(22f, 12f), false,
                new[] { new[] { sproutA, collector }, new[] { split } },
                new[] { 1, -1 }, reward, null);

            c.Room("FractureRoom09", 32f, 16f);
        }

        // ---------------- F10 초침 감시탑 (D4, 중간 보스) ----------------
        static void BuildF10(RoomCtx c)
        {
            c.O = F10;
            c.Floor(0, 20, 3);
            c.Floor(20, 24, 7);   // 출구 (24,7)
            BuildFractureSafePlatform(c.Root.transform, c.P(18.5f, 5.5f));

            BuildCheckpoint(c.Root.transform, c.P(2f, 4f)); // 체크포인트 3 — 전장 바깥

            // 시계바늘 발판. 일정한 주기라 관찰로 배울 수 있고, 예지 고스트와 정확히 일치한다.
            BuildOrbitPlatform(c.Root.transform, "F10_Hand_Long", c.P(10f, 9f), new Vector2(0f, -3f), 40f, 0f);
            BuildOrbitPlatform(c.Root.transform, "F10_Hand_Short", c.P(14f, 11f), new Vector2(-2f, -4f), 25f, 90f);

            var boss = BuildBoss(c.Root.transform, c.P(14f, 5f), "Enemy_Fracture_SecondHand", 15,
                new[] { BossController.Move.GroundSweep, BossController.Move.TimeSkip, BossController.Move.Charge },
                new[] { 0.5f }, new Color(0.8f, 0.76f, 0.92f));

            var reward = BuildRewardChest(c.Root.transform, "fracture_f10_boss", c.P(10f, 4.5f), 45, false);
            BuildEncounter(c.Root.transform, "fracture_f10_boss", c.P(10f, 8f), new Vector2(18f, 12f), true,
                new[] { new[] { boss } }, new int[0], reward, _fractureShortcutC);

            c.Room("FractureRoom10", 24f, 18f);
        }

        // ---------------- F11 아직 오지 않은 폐허 (D2) ----------------
        // 전투 없는 미래 서사. 고스트는 절대 충돌 지형이 되지 않는다(4.11절).
        static void BuildF11(RoomCtx c)
        {
            c.O = F11;
            // 로컬 8~10을 비워 FS3로 내려가는 자리를 만든다. 그 위를 "아직 없는 문"이 막는다.
            c.Floor(0, 8, 3, 2);
            c.Floor(10, 24, 3, 2);
            c.Floor(24, 28, 4, 2);  // 출구 (28,4)

            BuildStoryFragment(c.Root.transform, c.P(4f, 4f), "fracture_f11",
                "무너진 뒤의 모습만 남아, 아직 무너지지 않았다.", EmotionId.None, false);

            // 현재에는 기초와 문틀만 있다.
            for (int i = 0; i < 4; i++)
                BuildDecor(c.Root.transform, $"F11_Foundation_{i}", c.P(13f + i * 3.5f, 3.6f), new Vector2(2.4f, 0.8f),
                    "Tile", FractureStone, 0f, -3);

            // 예지 안에서만 완성된 폐허의 윤곽이 나타난다. 밟을 수는 없다(solidWhenFixed=false).
            for (int i = 0; i < 4; i++)
                BuildFutureEcho(c.Root.transform, $"F11_Ruin_{i}", c.P(13f + i * 3.5f, 7f), new Vector2(2.4f, 6f),
                    0, null, false, new Vector2(0.15f * (i % 2 == 0 ? 1f : -1f), 0.2f));

            // FS3 입구를 막는 벽. 세 번의 예지에서 같은 자리에 나타난 문이 현재에 고정되면 열린다.
            _fractureSecretDoor = BuildBlockingDoor(c.Root.transform, "fracture_secret_door", c.P(9f, 2.75f),
                new Vector2(2f, 0.5f), FractureStone);
            BuildFutureEcho(c.Root.transform, "F11_UnchosenDoor", c.P(9f, 5.5f), new Vector2(2.2f, 4f),
                3, _fractureSecretDoor, false);

            // 방 끝에서 F12 전장이 한 번 무너졌다 돌아오는 모습을 안전하게 본다.
            BuildDecor(c.Root.transform, "F11_DistantArena", c.P(26f, 10f), new Vector2(8f, 6f),
                "Tile", new Color(0.88f, 0.84f, 0.94f, 0.6f), 0f, -7);

            c.Room("FractureRoom11", 28f, 16f);
        }

        // ---------------- FS3 선택되지 않은 문 (D0 선택) ----------------
        static void BuildFS3(RoomCtx c)
        {
            c.O = FS3;
            c.Floor(0, 20, 2);

            // 선택되지 않은 미래들이 흔적으로만 남아 있다.
            for (int i = 0; i < 5; i++)
                BuildDecor(c.Root.transform, $"FS3_UnchosenDoor_{i}", c.P(3f + i * 3.5f, 4.5f), new Vector2(1.6f, 3.4f),
                    "Gate", new Color(0.93f, 0.91f, 1f, 0.45f), (i - 2) * 4f, -5);

            BuildStoryFragment(c.Root.transform, c.P(16f, 3f), "fracture_final",
                "고르지 않은 문들도, 끝까지 나를 따라왔다.", EmotionId.None, false);

            c.Room("FractureSecret03", 20f, 14f);
        }

        // ---------------- F12 내일의 균열 (D5, 지역 보스) ----------------
        static void BuildF12(RoomCtx c)
        {
            c.O = F12;
            // 전장을 좌측 안정 지대 / 중앙 변화 지대 / 우측 공격 지대로 나눈다(4.12절).
            c.Floor(0, 10, 4);     // 좌측 안정 지대 — 항상 남는다
            c.Floor(10, 20, 4);    // 중앙 변화 지대
            c.Floor(20, 30, 4);    // 우측 공격 지대

            BuildSolidBlock(c.Root.transform, "F12_Wall_L", c.P(0.5f, 9f), new Vector2(1.2f, 10f), "Wall", FractureStone);
            BuildSolidBlock(c.Root.transform, "F12_Wall_R", c.P(29.5f, 9f), new Vector2(1.2f, 10f), "Wall", FractureStone);

            // 단계마다 일부 발판이 사라지지만 좌측 안정 지대는 언제나 반응 가능한 안전 경로다.
            BuildFractureCrumbling(c.Root.transform, c.P(13f, 7f));
            BuildFractureCrumbling(c.Root.transform, c.P(17f, 9f));
            BuildFractureSafePlatform(c.Root.transform, c.P(6f, 8f));

            var boss = BuildBoss(c.Root.transform, c.P(16f, 6f), "Enemy_Fracture_NotYetMe", 22,
                new[]
                {
                    BossController.Move.TimeSkip,
                    BossController.Move.Slam,
                    BossController.Move.Charge,
                    BossController.Move.GroundSweep,
                },
                new[] { 0.6f, 0.3f }, new Color(0.72f, 0.68f, 0.9f));

            var reward = BuildRewardChest(c.Root.transform, "fracture_f12_boss", c.P(16f, 5.5f), 70, true);
            BuildEncounter(c.Root.transform, "fracture_f12_boss", c.P(15f, 9f), new Vector2(26f, 12f), true,
                new[] { new[] { boss } }, new int[0], reward, null);

            // 마지막 단계: 하늘 균열이 세 갈래로 보이다가, 플레이어가 향한 하나만 실제가 된다.
            BuildPathChoice(c.Root.transform, "F12_SkyChoice", c.P(22f, 5f), new[]
            {
                (c.P(21f, 6f), c.P(24f, 7f)),
                (c.P(21f, 9f), c.P(24f, 10f)),
                (c.P(21f, 12f), c.P(24f, 13f)),
            });

            BuildStoryFragment(c.Root.transform, c.P(27f, 5f), "fracture_core",
                "알아도 고를 수는 없었다. 그래서 골랐다.", EmotionId.None, false);

            // 클리어 허브로 이어지는 통로. 이 트리거가 균열 클리어를 진행도에 기록한다.
            BuildZoneTrigger(c.Root.transform, c.P(28f, 6f), new Vector2(2f, 4f), true);

            c.Room("FractureRoom12", 30f, 18f);
            BuildBoundary(c.Root.transform, "Fracture_EastBoundary", c.P(30.5f, 0f).x);
        }
    }
}
