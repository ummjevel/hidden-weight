using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.World;

namespace HiddenWeight.EditorTools
{
    // 응시 지역 전체(주 동선 12룸 + 비밀 3룸)를 한 씬에 짓는다.
    // 명세: docs/GAZE_LEVEL_DESIGN.md, docs/WORLD_MAP.md 3장
    //
    // 잔재(ResidueZoneBuilder)와 같은 골격을 그대로 쓴다 — 방마다 로컬 좌표 (0,0)을 쓰고
    // RoomCtx가 전역으로 옮기며, 방 사이는 출구/입구 높이를 맞춘 평평한 통로가 잇는다.
    // 다른 것은 지역이 무엇을 가르치느냐뿐이다: 잔재는 되감기로 공간을 되돌렸고, 응시는
    // 숨죽이기로 몸을 줄였다가 자각으로 정면을 마주 본다.
    //
    // 기존 Zone_Gaze.unity(옛 MVP 4룸)는 건드리지 않는다. 잔재와 같은 순서다 — 검증을
    // 통과한 뒤에 교체한다.
    public static partial class ZoneSceneBuilder
    {
        // ------------------------------------------------------------
        // 방 배치 오프셋
        // 각 방의 "출구 높이"와 다음 방의 "입구 높이"가 세계 좌표에서 정확히 맞도록 y를 계산했다.
        // 예: G03 출구 (28,2) → 세계 y=2, G04 입구 (2,18) → G04.y = 2-18 = -16.
        // x는 이전 방 오른쪽 끝에서 CorridorGap(4)만큼 띄운다.
        // ------------------------------------------------------------
        static readonly Vector2Int G01 = new Vector2Int(0, 0);
        static readonly Vector2Int G02 = new Vector2Int(30, 0);
        static readonly Vector2Int G03 = new Vector2Int(62, 0);
        static readonly Vector2Int G04 = new Vector2Int(96, -16);
        static readonly Vector2Int GS1 = new Vector2Int(96, -30);
        static readonly Vector2Int G05 = new Vector2Int(124, -16);
        static readonly Vector2Int G06 = new Vector2Int(154, -16);
        static readonly Vector2Int GS2 = new Vector2Int(154, -32);
        static readonly Vector2Int G07 = new Vector2Int(190, -16);
        static readonly Vector2Int G08 = new Vector2Int(228, -14);
        static readonly Vector2Int G09 = new Vector2Int(256, 9);
        static readonly Vector2Int G10 = new Vector2Int(292, 10);
        static readonly Vector2Int G11 = new Vector2Int(320, 14);
        static readonly Vector2Int GS3 = new Vector2Int(320, 0);
        static readonly Vector2Int G12 = new Vector2Int(352, 14);

        // 저채도 보라·청록·먹색(2.1절 시각 언어). 전용 아트가 아직 없으므로 지형 색으로 지역을 구분한다.
        static readonly Color GazeTerrain = new Color(0.42f, 0.38f, 0.55f);
        static readonly Color GazeStone = new Color(0.33f, 0.30f, 0.45f);
        static readonly Color GazeMirror = new Color(0.72f, 0.80f, 0.85f);

        // 숨죽이기 게이트 규격. 플레이어 콜라이더는 0.8x1.4, 숨죽이면 0.6배라 0.48x0.84가 된다.
        // 그래서 아래 두 값 사이를 지나갈 수 있는 것은 숨죽인 몸뿐이다.
        const float CrawlClearance = 1.1f;  // 낮은 천장: 0.84 < 1.1 < 1.4
        const float SlotWidth = 0.66f;      // 좁은 세로 틈: 0.48 < 0.66 < 0.8

        // 숏컷은 만든 곳(G03·G07)과 여는 곳(G05·G08·G10)이 다르므로 들고 있는다.
        static Shortcut _gazeShortcutA;
        static Shortcut _gazeShortcutB;
        static Shortcut _gazeShortcutC;

        // 재판관이 읽는 점멸 시선. G09에서 만들고 정예에 물려 준다.
        static GazeHazard[] _g09Gazes;

        // ------------------------------------------------------------
        // 응시 전용 조립 헬퍼
        // ------------------------------------------------------------

        // 타일맵 전체를 지역 색으로 물들인다. 세 지역이 같은 회색 폐허로 보이지 않게 하는
        // 가장 싼 방법이고, 전용 타일 아트가 들어오면 이 한 줄만 지우면 된다.
        static void TintTerrain(Tilemap tilemap, Color color)
        {
            tilemap.color = color;
        }

        // 시선을 막는 엄폐 기둥. GazeHazard의 groundMask가 Ground라 반드시 Ground 레이어여야
        // 라인캐스트를 끊는다 — Wall로 두면 "기둥 뒤에 섰는데 그대로 보이는" 상태가 된다.
        static GameObject BuildCoverPillar(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = BuildSolidBlock(parent, name, center, size, "Ground", GazeStone);
            go.GetComponent<SpriteRenderer>().sortingOrder = 3;
            return go;
        }

        static GameObject BuildPlainWall(Transform parent, string name, Vector2 center, Vector2 size, Color tint)
        {
            var go = BuildSolidBlock(parent, name, center, size, "Wall", tint);
            go.GetComponent<SpriteRenderer>().sortingOrder = 3;
            return go;
        }

        // 시선 하나를 놓고 점멸·휴면·복귀 지점을 설정한다. 프리팹은 그대로 쓰고 값만 덮는다.
        static GazeHazard PlaceGaze(Transform parent, Vector2 pos, float rotationZ, float rotateSpeed,
                                    float onSeconds = 0f, float offSeconds = 0f, float phase = 0f,
                                    bool dormant = false, Transform retreat = null)
        {
            var go = BuildGazeHazard(parent, pos, rotationZ, rotateSpeed);
            var gaze = go.GetComponent<GazeHazard>();

            SetField(gaze, "onSeconds", p => p.floatValue = onSeconds);
            SetField(gaze, "offSeconds", p => p.floatValue = offSeconds);
            SetField(gaze, "phaseOffset", p => p.floatValue = phase);
            SetField(gaze, "dormant", p => p.boolValue = dormant);
            if (retreat != null) SetField(gaze, "retreatPoint", p => p.objectReferenceValue = retreat);

            return gaze;
        }

        // 포착당했을 때 되돌아갈 자리. 방마다 "직전 엄폐물 뒤"에 하나씩 둔다(4.2절).
        static Transform BuildRetreatPoint(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            return go.transform;
        }

        // 안쪽에서 여는 숏컷 장치.
        static ShortcutLever BuildShortcutLever(Transform parent, string name, Vector2 pos, Shortcut target)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.layer = LayerMask.NameToLayer("Interactable");

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(1f, 2f, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Gate");
            sr.color = new Color(0.45f, 0.4f, 0.6f);
            sr.sortingOrder = 4;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 2.4f);

            var lever = go.AddComponent<ShortcutLever>();
            SetField(lever, "target", p => p.objectReferenceValue = target);
            SetField(lever, "visual", p => p.objectReferenceValue = sr);
            return lever;
        }

        // 자각 중에만 드러나는 표식·발판·거울문. invert=true면 반대로, 자각 중에만 사라지는 벽이다.
        static AwarenessRevealed BuildAwarenessPiece(Transform parent, string name, Vector2 center, Vector2 size,
                                                     Color tint, bool solid, bool invert)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            if (solid) go.layer = LayerMask.NameToLayer("Ground");

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Tile");
            sr.color = tint;
            sr.sortingOrder = 3;

            BoxCollider2D col = null;
            if (solid)
            {
                col = go.AddComponent<BoxCollider2D>();
                col.size = size;
            }

            var reveal = go.AddComponent<AwarenessRevealed>();
            SetField(reveal, "visual", p => p.objectReferenceValue = sr);
            SetField(reveal, "invert", p => p.boolValue = invert);
            if (col != null) SetField(reveal, "solid", p => p.objectReferenceValue = col);
            return reveal;
        }

        // 승강기. waypoints는 시작 위치 기준 상대 좌표다.
        static LiftPlatform BuildLift(Transform parent, string name, Vector2 pos, Vector2[] waypoints,
                                      Shortcut shortcut, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.layer = LayerMask.NameToLayer("Ground");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Platform");
            sr.color = tint;
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            go.AddComponent<Rigidbody2D>();

            var lift = go.AddComponent<LiftPlatform>();
            SetField(lift, "waypoints", p =>
            {
                p.arraySize = waypoints.Length;
                for (int i = 0; i < waypoints.Length; i++)
                    p.GetArrayElementAtIndex(i).vector2Value = waypoints[i];
            });
            if (shortcut != null) SetField(lift, "linkedShortcut", p => p.objectReferenceValue = shortcut);
            return lift;
        }

        // 가짜/진짜 예고를 내는 관객 조각상(G12 보스 2단계).
        static DecoyTelegraph BuildStatue(Transform parent, string name, Vector2 basePos, bool isReal, float phase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            body.transform.localScale = new Vector3(1.2f, 2.8f, 1f);
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sprite = LoadSprite("Tile");
            bodySr.color = isReal ? new Color(0.3f, 0.28f, 0.4f) : new Color(0.32f, 0.3f, 0.42f);
            bodySr.sortingOrder = -2;

            var shadow = new GameObject("GroundShadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            shadow.transform.localScale = new Vector3(2f, 0.35f, 1f);
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            shadowSr.sprite = LoadSprite("Tile");
            shadowSr.color = new Color(0f, 0f, 0f, 0.5f);
            shadowSr.sortingOrder = 4;

            var decoy = go.AddComponent<DecoyTelegraph>();
            SetField(decoy, "body", p => p.objectReferenceValue = bodySr);
            SetField(decoy, "groundShadow", p => p.objectReferenceValue = shadowSr);
            SetField(decoy, "isReal", p => p.boolValue = isReal);
            SetField(decoy, "phaseOffset", p => p.floatValue = phase);
            return decoy;
        }

        // ------------------------------------------------------------
        // 응시 적 구성 (GAZE_LEVEL_DESIGN.md 6절)
        // 잔재와 같은 방식이다 — 프리팹을 늘리지 않고 기본 Enemy 프리팹에 데이터와 행동
        // 모듈만 갈아끼운다.
        // ------------------------------------------------------------

        public enum GazeEnemyKind { Pilgrim, Mouth, Audience, Judge }

        static EnemyData GazeEnemyData(GazeEnemyKind kind)
        {
            string name = $"Enemy_Gaze_{kind}";
            var existing = LoadData<EnemyData>(name);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<EnemyData>();
            switch (kind)
            {
                case GazeEnemyKind.Pilgrim: // 눈먼 순례자 — 소리로 쫓는다. 숨죽이면 거의 못 찾는다
                    data.maxHealth = 3; data.moveSpeed = 1.4f; data.contactDamage = 1;
                    data.tint = new Color(0.58f, 0.54f, 0.7f);
                    data.detectRange = 7f;
                    data.hushedDetectMultiplier = 0.25f;
                    break;

                case GazeEnemyKind.Mouth: // 밀고하는 입 — 때리지 않고 시선 장치를 켠다
                    data.maxHealth = 2; data.moveSpeed = 0f; data.contactDamage = 1;
                    data.tint = new Color(0.45f, 0.7f, 0.68f);
                    data.detectRange = 9f;
                    data.telegraphSeconds = 1.0f;
                    data.recoverSeconds = 1.2f;
                    data.alertSeconds = 4f;
                    break;

                case GazeEnemyKind.Audience: // 매달린 관객 — 천장 매복. 숨죽인 아래는 그냥 보낸다
                    data.maxHealth = 2; data.moveSpeed = 1.2f; data.contactDamage = 1;
                    data.tint = new Color(0.4f, 0.36f, 0.52f);
                    data.dropSpeed = 14f;
                    break;

                case GazeEnemyKind.Judge: // 얼굴 없는 재판관 — 정면 방어 정예
                    data.maxHealth = 7; data.moveSpeed = 1.2f; data.contactDamage = 1;
                    data.tint = new Color(0.28f, 0.3f, 0.46f);
                    data.telegraphSeconds = 1.2f;
                    data.recoverSeconds = 1.3f;
                    data.guardArc = 140f;
                    data.attackRange = 2.4f;
                    data.detectRange = 8f;
                    break;
            }

            AssetDatabase.CreateAsset(data, $"{DataFolder}/{name}.asset");
            return data;
        }

        static GameObject BuildGazeEnemy(Transform parent, Vector2 pos, GazeEnemyKind kind)
        {
            var go = BuildEnemy(parent, pos, GazeEnemyData(kind));
            int playerMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");

            switch (kind)
            {
                case GazeEnemyKind.Pilgrim:
                    go.AddComponent<StalkerBehavior>();
                    break;

                case GazeEnemyKind.Mouth:
                    go.AddComponent<ScreamerBehavior>();
                    break;

                case GazeEnemyKind.Audience:
                {
                    var shadowGO = new GameObject("DropShadow");
                    shadowGO.transform.SetParent(go.transform, false);
                    shadowGO.transform.localPosition = new Vector3(0f, -4f, 0f);
                    shadowGO.transform.localScale = new Vector3(1.2f, 0.3f, 1f);
                    var shadowSr = shadowGO.AddComponent<SpriteRenderer>();
                    shadowSr.sprite = LoadSprite("Tile");
                    shadowSr.color = new Color(0f, 0f, 0f, 0.5f);
                    shadowSr.sortingOrder = 4;

                    var ambush = go.AddComponent<AmbusherBehavior>();
                    SetField(ambush, "shadow", p => p.objectReferenceValue = shadowSr);
                    // 잔재의 매달린 손가락과 같은 컴포넌트지만 이 한 줄이 응시의 규칙을 만든다.
                    SetField(ambush, "ignoreHushedPlayer", p => p.boolValue = true);

                    var patrol = go.GetComponent<EnemyPatrol>();
                    if (patrol != null) patrol.enabled = false;
                    break;
                }

                case GazeEnemyKind.Judge:
                {
                    var judge = go.AddComponent<JudgeBehavior>();
                    SetField(judge, "playerMask", p => p.intValue = playerMask);
                    break;
                }
            }

            return go;
        }

        // 재판관에 "이 시선들이 꺼지면 등을 보인다"와 "숨으면 여기를 막는다"를 물려 준다.
        static void ConfigureJudge(GameObject judgeObject, GazeHazard[] watched, Transform exitBlock)
        {
            var judge = judgeObject.GetComponent<JudgeBehavior>();
            if (judge == null) return;

            if (watched != null)
                SetField(judge, "watchedGazes", p =>
                {
                    p.arraySize = watched.Length;
                    for (int i = 0; i < watched.Length; i++)
                        p.GetArrayElementAtIndex(i).objectReferenceValue = watched[i];
                });

            if (exitBlock != null) SetField(judge, "exitBlockPoint", p => p.objectReferenceValue = exitBlock);
        }

        // 시선 공격을 쓰는 보스에 gazeMask(=Player만)와 눈꺼풀 벽을 물려 준다.
        static void ConfigureGazeBoss(GameObject bossObject, GameObject[] closingWalls)
        {
            var boss = bossObject.GetComponent<BossController>();
            if (boss == null) return;

            // PlayerHushed를 넣지 않는다. 이것이 "숨죽이기는 시선 공격만 피한다"의 전부다.
            SetField(boss, "gazeMask", p => p.intValue = 1 << LayerMask.NameToLayer("Player"));

            if (closingWalls == null) return;
            SetField(boss, "closingWalls", p =>
            {
                p.arraySize = closingWalls.Length;
                for (int i = 0; i < closingWalls.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = closingWalls[i].transform;
            });
        }

        // ------------------------------------------------------------
        // 방 연결
        // ------------------------------------------------------------

        static void BuildGazeConnections(RoomCtx c)
        {
            var map = c.Map;
            var parent = c.Root.transform;

            var links = new (Vector2Int from, Vector2 exit, Vector2Int to, Vector2 entry, string name)[]
            {
                (G01, new Vector2(26, 2), G02, new Vector2(0, 2), "GC_G01_G02"),
                (G02, new Vector2(28, 3), G03, new Vector2(0, 3), "GC_G02_G03"),
                (G03, new Vector2(28, 2), G04, new Vector2(2, 18), "GC_G03_G04"),
                (G04, new Vector2(22, 2), G05, new Vector2(0, 2), "GC_G04_G05"),
                (G05, new Vector2(26, 2), G06, new Vector2(0, 2), "GC_G05_G06"),
                (G06, new Vector2(32, 4), G07, new Vector2(0, 4), "GC_G06_G07"),
                (G07, new Vector2(34, 4), G08, new Vector2(2, 2), "GC_G07_G08"),
                (G08, new Vector2(22, 26), G09, new Vector2(0, 3), "GC_G08_G09"),
                (G09, new Vector2(32, 4), G10, new Vector2(0, 3), "GC_G09_G10"),
                (G10, new Vector2(24, 7), G11, new Vector2(0, 3), "GC_G10_G11"),
                (G11, new Vector2(28, 4), G12, new Vector2(0, 4), "GC_G11_G12"),
            };

            foreach (var link in links)
            {
                float exitX = link.from.x + link.exit.x;
                float exitY = link.from.y + link.exit.y;
                float entryX = link.to.x + link.entry.x;
                float entryY = link.to.y + link.entry.y;

                if (!Mathf.Approximately(exitY, entryY))
                    Debug.LogWarning($"[GazeZoneBuilder] {link.name} 높이 불일치: 출구 y={exitY}, 입구 y={entryY}");

                BuildCorridor(parent, map, link.name, exitX, entryX, Mathf.RoundToInt(exitY));
            }

            // GS1 — G04 하층 바닥의 뚫린 자리(로컬 8~9) 아래. 스킬 없이 관찰만으로 찾는다.
            BuildShaft(parent, map, "Shaft_GS1", G04.x + 8, G04.y + 0, GS1.y + 2);

            // GS2 — G06 바닥의 좁은 틈(로컬 20~21) 아래. 틈 폭이 숨죽인 몸만 통과시킨다.
            BuildShaft(parent, map, "Shaft_GS2", G06.x + 20, G06.y + 0, GS2.y + 2);

            // GS3 — G11 바닥의 거울문(로컬 8~10) 아래. 자각 중에만 문이 열린다.
            BuildShaft(parent, map, "Shaft_GS3", G11.x + 9, G11.y + 1, GS3.y + 2);
        }

        [MenuItem("Hidden Weight/Build Gaze Zone (Full)")]
        public static void RunGazeZone()
        {
            EnsureScenesFolder();

            var scene = NewScene();
            var tilemap = BuildZoneRoot("Gaze", out var root);
            TintTerrain(tilemap, GazeTerrain);

            var rooms = new GameObject("Rooms");
            rooms.transform.SetParent(root.transform, true);

            // 씬 이름이 ZoneData.sceneName과 다르므로 지역을 씬이 직접 선언한다.
            var marker = new GameObject("ZoneMarker");
            marker.transform.SetParent(root.transform, false);
            var zoneMarker = marker.AddComponent<HiddenWeight.Core.ZoneMarker>();
            SetField(zoneMarker, "zone", p => p.enumValueIndex = (int)ZoneId.Gaze);

            // 잔재 전용 지형 아트를 응시 방에 씌우지 않는다(RoomCtx.FloorArt 주석 참고).
            var ctx = new RoomCtx { Map = tilemap, Root = root, Rooms = rooms.transform, FloorArt = false };

            BuildG01(ctx);
            BuildG02(ctx);
            BuildG03(ctx);
            BuildG04(ctx);
            BuildGS1(ctx);
            BuildG05(ctx);
            BuildG06(ctx);
            BuildGS2(ctx);
            BuildG07(ctx);
            BuildG08(ctx);
            BuildG09(ctx);
            BuildG10(ctx);
            BuildG11(ctx);
            BuildGS3(ctx);
            BuildG12(ctx);

            BuildGazeConnections(ctx);

            ctx.O = G01;
            PlacePlayerAndCamera(root, new Vector3(G01.x + 3f, G01.y + 3f, 0f));

            SaveScene(scene, "Zone_Gaze_Full");
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GazeZoneBuilder] 응시 전체 지역(15룸) 생성 완료");
        }

        // ---------------- G01 눈꺼풀 경계 (D0) ----------------
        // 공격도 낙사도 없다. "누군가 보고 있다"를 전투 없이 먼저 겪게 하는 방(1.2절).
        static void BuildG01(RoomCtx c)
        {
            c.O = G01;
            c.Floor(0, 26, 2);

            BuildCheckpoint(c.Root.transform, c.P(5f, 3f));

            // 눈꺼풀처럼 닫힌 곡선. 눈 자체는 아직 보여주지 않는다(4.1절).
            BuildDecor(c.Root.transform, "G01_LidUpper", c.P(13f, 11f), new Vector2(22f, 1.6f),
                "Tile", GazeStone, 3f, -4);
            BuildDecor(c.Root.transform, "G01_LidLower", c.P(13f, 0.6f), new Vector2(22f, 1.4f),
                "Tile", GazeStone, -3f, -4);

            // 방 끝에서 지역 후반 목표 둘을 동시에 보여준다(2.3절 랜드마크).
            BuildDecor(c.Root.transform, "G01_ClosedGreatEye", c.P(22f, 9f), new Vector2(5f, 5f),
                "Eye", new Color(0.3f, 0.27f, 0.4f), 0f, -6);
            BuildDecor(c.Root.transform, "G01_TowerSilhouette", c.P(18f, 8f), new Vector2(2.2f, 12f),
                "Tile", new Color(0.22f, 0.2f, 0.3f), 2f, -7);

            // 안전한 거리에서 지나가는 시선 빛만 관찰한다. 시야 반경 밖이라 닿지 않는다.
            PlaceGaze(c.Root.transform, c.P(24f, 10f), 200f, 25f);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(10f + i * 1.5f, 3.2f));

            c.Room("GazeRoom01", 26f, 14f);
            BuildBoundary(c.Root.transform, "Gaze_WestBoundary", c.P(-0.5f, 0f).x);
        }

        // ---------------- G02 고정된 시선교 (D1→D2) ----------------
        // 첫 시선 학습. 기둥 뒤에 서면 완전히 차단되고, 실패해도 하부 우회로로 떨어질 뿐이다.
        static void BuildG02(RoomCtx c)
        {
            c.O = G02;
            c.Floor(0, 8, 2);
            c.Floor(8, 17, 1);   // 하부 안전 우회로
            c.Floor(17, 20, 3);
            c.Floor(20, 22, 1);
            c.Floor(22, 28, 3);  // 출구 (28,3)

            // 상부 다리. 놓쳐도 하부로 떨어질 뿐이다.
            BuildSolidBlock(c.Root.transform, "G02_Bridge_A", c.P(9.5f, 3.75f), new Vector2(3f, 0.5f), "Ground", GazeStone);
            BuildSolidBlock(c.Root.transform, "G02_Bridge_B", c.P(14f, 3.75f), new Vector2(2.5f, 0.5f), "Ground", GazeStone);

            // 첫 시선: 기둥 하나로 완전히 막힌다.
            BuildCoverPillar(c.Root.transform, "G02_Pillar_A", c.P(6.5f, 4f), new Vector2(1.2f, 4f));
            var retreatA = BuildRetreatPoint(c.Root.transform, "G02_Retreat_A", c.P(5f, 3f));
            PlaceGaze(c.Root.transform, c.P(11f, 8f), 270f, 0f, retreat: retreatA);

            // 두 번째 시선: 상부 다리를 훑는다. 하부로 내려가면 아예 닿지 않는다.
            var retreatB = BuildRetreatPoint(c.Root.transform, "G02_Retreat_B", c.P(12f, 2f));
            PlaceGaze(c.Root.transform, c.P(19f, 8f), 270f, 0f, retreat: retreatB);
            BuildCoverPillar(c.Root.transform, "G02_Pillar_B", c.P(16.5f, 5f), new Vector2(1.2f, 4f));

            // 첫 적은 두 시선 사이의 안전 구역에. 시선과 적이 동시에 압박하지 않게 한다(4.2절).
            BuildGazeEnemy(c.Root.transform, c.P(24f, 4f), GazeEnemyKind.Pilgrim);

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(11.5f + i * 0.6f, 5f));
            BuildHealingPickup(c.Root.transform, c.P(26.5f, 4f));

            c.Room("GazeRoom02", 28f, 16f);
        }

        // ---------------- G03 관객 광장 (D1) ----------------
        // 중앙 허브. 첫 방문에는 G04로 내려가는 길만 열려 있고, 숏컷 A·B는 만져지지만 못 지나간다.
        static void BuildG03(RoomCtx c)
        {
            c.O = G03;
            c.Floor(0, 20, 3);
            c.Floor(20, 30, 2);  // 남동쪽 하층으로 가는 낮은 길

            for (int i = 0; i < 4; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(21f + i * 2f, 3.2f));

            BuildGazeEnemy(c.Root.transform, c.P(16f, 4f), GazeEnemyKind.Pilgrim);

            // 랜드마크 둘이 서로 다른 높이에서 보인다(4.3절).
            BuildDecor(c.Root.transform, "G03_IrisTower", c.P(9f, 12f), new Vector2(3f, 14f),
                "Tile", new Color(0.24f, 0.22f, 0.34f), 0f, -7);
            BuildDecor(c.Root.transform, "G03_TheatreDome", c.P(26f, 14f), new Vector2(9f, 5f),
                "Tile", new Color(0.2f, 0.19f, 0.3f), 0f, -8);
            BuildDecor(c.Root.transform, "G03_HangingSeats", c.P(15f, 15f), new Vector2(12f, 2f),
                "Tile", new Color(0.26f, 0.24f, 0.36f), -4f, -6);

            _gazeShortcutA = BuildShortcut(c.Root.transform, "gaze_shortcut_a", c.P(5f, 9f),
                new Vector2(4f, 0.6f), new Color(0.6f, 0.55f, 0.75f));
            _gazeShortcutB = BuildShortcut(c.Root.transform, "gaze_shortcut_b", c.P(24f, 7f),
                new Vector2(3.5f, 0.6f), new Color(0.5f, 0.7f, 0.8f));

            c.Room("GazeRoom03", 30f, 18f);
        }

        // ---------------- G04 하층 새장원 (D2) ----------------
        // 수직 하강과 환경 서사. 철창 안의 존재들은 플레이어가 다가가면 등을 돌린다.
        static void BuildG04(RoomCtx c)
        {
            c.O = G04;
            c.Floor(0, 8, 18);        // 상층 관찰대 (입구 2,18)
            // 하층 안전 바닥. 로컬 8~9를 비워 GS1으로 내려가는 구멍을 만든다.
            // 깊이를 2로 줄인다 — 8칸짜리 깊은 구멍이면 다시 기어 올라올 수 없다.
            c.Floor(0, 8, 2, 2);
            c.Floor(9, 24, 2, 2);

            // 지그재그 하강. 한 번 실패해도 하층 바닥으로 이어져 진행은 가능하다(4.4절).
            BuildSafePlatform(c.Root.transform, c.P(11f, 15f));
            BuildSafePlatform(c.Root.transform, c.P(14.5f, 12f));
            BuildSafePlatform(c.Root.transform, c.P(11f, 9f));
            BuildSafePlatform(c.Root.transform, c.P(14.5f, 6f));

            // 구멍 위를 반쯤 덮는 발판. 주 동선은 끊기지 않고, 왼쪽 끝으로 내려서면 GS1이다.
            BuildSafePlatform(c.Root.transform, c.P(6.5f, 2.6f));
            BuildDecor(c.Root.transform, "G04_GS1_Hint", c.P(8.5f, 2.2f), new Vector2(1.6f, 0.3f),
                "Tile", new Color(0.2f, 0.18f, 0.28f));

            // 철창과 그 안의 존재들. 다가가면 반대편을 본다는 연출은 아트 단계의 몫이고,
            // 여기서는 배치와 실루엣만 세운다.
            for (int i = 0; i < 3; i++)
            {
                float x = 4f + i * 6f;
                BuildDecor(c.Root.transform, $"G04_Cage_{i}", c.P(x, 6.5f), new Vector2(1.8f, 2.6f),
                    "Gate", new Color(0.26f, 0.23f, 0.36f), 0f, -5);
                BuildDecor(c.Root.transform, $"G04_Caged_{i}", c.P(x, 6.2f), new Vector2(0.5f, 1f),
                    "Player", new Color(0.12f, 0.11f, 0.17f), 0f, -6);
            }

            BuildGazeEnemy(c.Root.transform, c.P(4f, 3f), GazeEnemyKind.Pilgrim);
            BuildGazeEnemy(c.Root.transform, c.P(19f, 3f), GazeEnemyKind.Pilgrim);

            BuildStoryFragment(c.Root.transform, c.P(21.5f, 3f), "gaze_g04",
                "보여지고 있었지만, 아무도 나를 보고 있지 않았다.", EmotionId.None, false);

            c.Room("GazeRoom04", 24f, 22f);
        }

        // ---------------- GS1 무대 뒤편 (D2 선택) ----------------
        // 스킬 없이 관찰만으로 찾는 비밀방. 지역 탐색의 기본 규칙을 가르친다(5절).
        static void BuildGS1(RoomCtx c)
        {
            c.O = GS1;
            c.Floor(0, 18, 2);

            BuildSafePlatform(c.Root.transform, c.P(5f, 5f));
            BuildSafePlatform(c.Root.transform, c.P(9.5f, 7f));
            BuildSafePlatform(c.Root.transform, c.P(14f, 8f));

            // 무대를 뒤에서 본 풍경 — 앞면만 칠해진 배경판과 버팀목.
            BuildDecor(c.Root.transform, "GS1_StageBack", c.P(9f, 8f), new Vector2(16f, 10f),
                "Tile", new Color(0.19f, 0.18f, 0.26f), 0f, -7);
            BuildDecor(c.Root.transform, "GS1_Prop", c.P(3f, 4f), new Vector2(1f, 4f),
                "Tile", new Color(0.3f, 0.27f, 0.38f), 8f, -5);

            BuildStoryFragment(c.Root.transform, c.P(14f, 9.5f), "gaze_gs1",
                "앞을 칠할 때, 뒤는 아무도 보지 않는다고 했다.", EmotionId.None, false);
            BuildRewardChest(c.Root.transform, "gaze_gs1_currency", c.P(16f, 3f), 30, false);

            c.Room("GazeSecret01", 18f, 14f);
        }

        // ---------------- G05 숨죽임 성소 (D0→D1) ----------------
        // 핵심 능력 획득. 적도 낙사도 시간제한도 없고, 세 번의 사용을 순서대로 겪는다(4.5절).
        static void BuildG05(RoomCtx c)
        {
            c.O = G05;
            c.Floor(0, 26, 2);

            BuildCheckpoint(c.Root.transform, c.P(3f, 3f)); // 체크포인트 2 — 능력 획득 직전

            BuildStoryFragment(c.Root.transform, c.P(6f, 3f), "gaze_skill",
                "숨을 죽이면, 나를 보던 눈들도 조용해진다.", EmotionId.Hush, false);
            BuildTutorialHint(c.Root.transform, c.P(7f, 5f), "K 홀드  —  숨죽이기");

            // 1) 축소 — 낮은 입구. 능력이 없으면 통과할 수 없다.
            BuildSolidBlock(c.Root.transform, "G05_LowCeiling",
                c.P(11f, 2f + CrawlClearance + 1f), new Vector2(4f, 2f), "Ground", GazeStone);

            // 2) 은신 — 고정 시선 아래를 지나간다. 포착되면 낮은 입구 앞으로 되돌아간다.
            var retreat = BuildRetreatPoint(c.Root.transform, "G05_Retreat", c.P(14f, 3f));
            PlaceGaze(c.Root.transform, c.P(17f, 7f), 270f, 0f, retreat: retreat);

            // 3) 공격 불가 — 적 앞에서 숨죽이면 공격이 막힌다는 것을 안전하게 확인한다.
            BuildGazeEnemy(c.Root.transform, c.P(21f, 3f), GazeEnemyKind.Pilgrim);

            // 마지막에 사슬막 A를 안쪽에서 연다.
            BuildShortcutLever(c.Root.transform, "G05_ChainLever", c.P(24.5f, 3.2f), _gazeShortcutA);

            c.Room("GazeRoom05", 26f, 14f);
        }

        // ---------------- G06 속삭임 통로 (D2) ----------------
        // 능력 연속 응용. 계속 숨어 있는 것이 항상 빠른 선택은 아니게 만든다(4.6절).
        static void BuildG06(RoomCtx c)
        {
            c.O = G06;
            // 로컬 20~21을 비워 GS2로 내려가는 구멍을 만든다. 깊이 2로 얕게 판다.
            c.Floor(0, 20, 2, 2);
            c.Floor(21, 24, 2, 2);
            c.Floor(24, 28, 3, 2);
            c.Floor(28, 32, 4, 2);  // 출구 (32,4)

            // 낮은 천장 두 곳. 그 사이는 열린 공간이라 숨어서만 가면 오히려 느리다.
            BuildSolidBlock(c.Root.transform, "G06_LowCeiling_A",
                c.P(6f, 2f + CrawlClearance + 1f), new Vector2(4f, 2f), "Ground", GazeStone);
            BuildSolidBlock(c.Root.transform, "G06_LowCeiling_B",
                c.P(16f, 2f + CrawlClearance + 1f), new Vector2(4f, 2f), "Ground", GazeStone);

            // 밀고하는 입 2마리. 각자 자기 위의 휴면 시선을 켠다.
            var dormantA = PlaceGaze(c.Root.transform, c.P(11f, 8f), 270f, 0f, dormant: true);
            var dormantB = PlaceGaze(c.Root.transform, c.P(23f, 8f), 270f, 0f, dormant: true);

            var mouthA = BuildGazeEnemy(c.Root.transform, c.P(12f, 3f), GazeEnemyKind.Mouth);
            var mouthB = BuildGazeEnemy(c.Root.transform, c.P(24f, 4f), GazeEnemyKind.Mouth);
            LinkScreamer(mouthA, dormantA);
            LinkScreamer(mouthB, dormantB);

            BuildCoverPillar(c.Root.transform, "G06_Cover_A", c.P(9f, 4f), new Vector2(1.2f, 4f));
            BuildCoverPillar(c.Root.transform, "G06_Cover_B", c.P(19f, 4f), new Vector2(1.2f, 4f));

            // GS2 입구 — 숨죽인 몸만 들어가는 좁은 세로 틈. 바닥 구멍(로컬 20~21) 위에 얹는다.
            // 틈 폭이 0.48(숨죽임) < 0.66 < 0.8(평상시)이라 평소에는 몸이 끼어 들어가지 못한다.
            //
            // 틈을 만드는 벽 두 장은 일부러 낮게(1.5) 세운다. 주 동선이 이 앞을 지나가므로
            // 벽이 높으면 길이 막힌다 — 평상시에는 벽 위를 넘어 지나가고, 숨죽였을 때만
            // 위에서 틈 안으로 몸을 떨어뜨릴 수 있다.
            float slotLeft = 20.5f - SlotWidth * 0.5f;
            float slotRight = 20.5f + SlotWidth * 0.5f;
            BuildPlainWall(c.Root.transform, "G06_Slot_L",
                c.P(slotLeft - 0.6f, 2.75f), new Vector2(1.2f, 1.5f), GazeStone);
            BuildPlainWall(c.Root.transform, "G06_Slot_R",
                c.P(slotRight + 0.6f, 2.75f), new Vector2(1.2f, 1.5f), GazeStone);
            BuildDecor(c.Root.transform, "G06_GS2_Hint", c.P(20.5f, 5.5f), new Vector2(1.4f, 1.4f),
                "Eye", new Color(0.35f, 0.5f, 0.5f), 0f, -4);

            BuildRewardChest(c.Root.transform, "gaze_g06_material", c.P(30f, 5f), 20, false);

            c.Room("GazeRoom06", 32f, 16f);
        }

        static void LinkScreamer(GameObject mouth, GazeHazard device)
        {
            var screamer = mouth.GetComponent<ScreamerBehavior>();
            if (screamer == null || device == null) return;

            SetField(screamer, "linkedDevices", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = device;
            });
        }

        // ---------------- GS2 무언의 우리 (D3 선택) ----------------
        // 공격하지 않고 적 사이를 지나가는 선택 은신. 보상은 최대 체력 조각이다(5절).
        static void BuildGS2(RoomCtx c)
        {
            c.O = GS2;
            c.Floor(0, 24, 2);

            // 통로를 좁히는 우리들. 사이를 지나가되 싸울 필요는 없다.
            for (int i = 0; i < 4; i++)
                BuildDecor(c.Root.transform, $"GS2_Cage_{i}", c.P(4f + i * 5f, 5f), new Vector2(2f, 4f),
                    "Gate", new Color(0.24f, 0.22f, 0.34f), 0f, -5);

            BuildGazeEnemy(c.Root.transform, c.P(8f, 3f), GazeEnemyKind.Pilgrim);
            BuildGazeEnemy(c.Root.transform, c.P(16f, 3f), GazeEnemyKind.Pilgrim);
            PlaceGaze(c.Root.transform, c.P(12f, 8f), 270f, 0f, 3f, 2f, 0f);

            BuildRewardChest(c.Root.transform, "gaze_gs2_shard", c.P(22f, 3f), 0, true);

            c.Room("GazeSecret02", 24f, 16f);
        }

        // ---------------- G07 회전 홍채교 (D3) ----------------
        // 이동과 은신의 결합. 회전 시선 3개가 읽을 수 있는 반복 주기를 만든다(4.7절).
        static void BuildG07(RoomCtx c)
        {
            c.O = G07;
            c.Floor(0, 34, 4);

            // 같은 속도, 다른 시작 각도. 구간마다 겹치는 수를 1 → 2 → 3으로 늘린다.
            PlaceGaze(c.Root.transform, c.P(7f, 9f), 0f, 55f);
            PlaceGaze(c.Root.transform, c.P(16f, 9f), 120f, 55f);
            PlaceGaze(c.Root.transform, c.P(25f, 9f), 240f, 55f);

            // 구간 사이 완전한 안전지대(폭 3 이상). 시선이 닿지 않는 기둥 그림자다.
            BuildCoverPillar(c.Root.transform, "G07_Cover_A", c.P(11.5f, 6f), new Vector2(3f, 2f));
            BuildCoverPillar(c.Root.transform, "G07_Cover_B", c.P(20.5f, 6f), new Vector2(3f, 2f));

            BuildGazeEnemy(c.Root.transform, c.P(30f, 5f), GazeEnemyKind.Pilgrim);
            BuildGazeEnemy(c.Root.transform, c.P(22f, 12f), GazeEnemyKind.Audience);

            BuildRewardChest(c.Root.transform, "gaze_g07_currency", c.P(16f, 5f), 25, false);
            for (int i = 0; i < 5; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(12f + i * 1.2f, 8f));

            // 숏컷 C는 G07 쪽에 놓인 홍채문이다. 여는 것은 G10 중간 보스 승리다.
            _gazeShortcutC = BuildShortcut(c.Root.transform, "gaze_shortcut_c", c.P(32f, 11f),
                new Vector2(4f, 0.6f), new Color(0.8f, 0.6f, 0.7f));

            c.Room("GazeRoom07", 34f, 18f);
        }

        // ---------------- G08 시선 승강정 (D2) ----------------
        // 수직 전환과 휴지. 승강기 몸체가 회전 시선의 엄폐물이 된다(4.8절).
        static void BuildG08(RoomCtx c)
        {
            c.O = G08;
            c.Floor(0, 24, 2);

            // 승강기가 지나갈 기둥(x 4.5~7.5)에는 아무 지형도 두지 않는다. 예전에는 안전
            // 포켓을 그 위에 깔아 두어, 타고 올라가던 플레이어가 천장에 끼여 떨어졌다.
            c.Tiles(10, 16, 12, 13);   // 중간 안전 포켓
            c.Tiles(10, 16, 19, 20);   // 상단 안전 포켓
            c.Tiles(8, 24, 25, 26);    // 북동 출구 선반 (22,26)

            // 승강기는 바닥에 붙여 두고 곧게 위로만 올린다. 1유닛 띄우면 걸어오다 옆면에
            // 부딪혀 지나쳐 버리고, 대각선 구간을 두면 타고 가던 중에 미끄러진다(봇이 둘 다 겪었다).
            var lift = BuildLift(c.Root.transform, "G08_Lift", c.P(6f, 2.6f),
                new[] { new Vector2(0f, 22.6f) },
                _gazeShortcutB, new Color(0.5f, 0.7f, 0.8f));

            // 승강기를 놓치고 오른쪽으로 계속 걸어도 허공으로 떨어지지 않게 막는다.
            // 이 방의 출구는 위(22,26)뿐이라 바닥 오른쪽 바깥에는 아무것도 없다.
            var edge = BuildSolidBlock(c.Root.transform, "G08_RightEdge",
                c.P(24.5f, 12f), new Vector2(1f, 26f), "Ground");
            edge.GetComponent<SpriteRenderer>().enabled = false;

            // 승강 경로를 훑는 회전 시선. 승강기 그림자 안에 서 있으면 지나간다.
            PlaceGaze(c.Root.transform, c.P(12f, 15f), 0f, 45f);
            PlaceGaze(c.Root.transform, c.P(12f, 23f), 180f, 45f);

            // 추락해도 즉사 요소는 두지 않는다. 바닥이 그대로 승강기 시작점이라, 떨어지면
            // 다시 올라타는 것만으로 10초 안에 복귀한다(4.8절). 별도 위험 영역이 필요 없다.

            // 다음 방 방향을 미리 보여준다.
            BuildDecor(c.Root.transform, "G08_TheatreFar", c.P(20f, 30f), new Vector2(10f, 6f),
                "Tile", new Color(0.2f, 0.19f, 0.3f), 0f, -8);

            BuildHealingPickup(c.Root.transform, c.P(20f, 27f));

            c.Room("GazeRoom08", 24f, 28f);
            if (lift == null) Debug.LogWarning("[GazeZoneBuilder] G08 승강기 생성 실패");
        }

        // ---------------- G09 상층 관객석 (D4, 정예) ----------------
        // 숨죽이기만으로는 이길 수 없다는 것을 처음 시험한다(4.9절).
        static void BuildG09(RoomCtx c)
        {
            c.O = G09;
            c.Floor(0, 28, 3);
            c.Floor(28, 32, 4);   // 출구 (32,4)

            // 전투 전 전장을 2초 이상 내려다볼 수 있는 발코니.
            c.Tiles(2, 8, 9, 10);

            // 점멸 시선 2개. 둘이 동시에 꺼지는 순간이 정예의 빈틈이다.
            var gazeA = PlaceGaze(c.Root.transform, c.P(12f, 10f), 270f, 0f, 3f, 2.5f, 0f);
            var gazeB = PlaceGaze(c.Root.transform, c.P(20f, 10f), 270f, 0f, 3f, 2.5f, 0.6f);
            _g09Gazes = new[] { gazeA, gazeB };

            var pilgrim = BuildGazeEnemy(c.Root.transform, c.P(11f, 4f), GazeEnemyKind.Pilgrim);
            var mouth = BuildGazeEnemy(c.Root.transform, c.P(18f, 11f), GazeEnemyKind.Mouth);
            LinkScreamer(mouth, gazeA);

            var judge = BuildGazeEnemy(c.Root.transform, c.P(22f, 4f), GazeEnemyKind.Judge);
            var exitBlock = BuildRetreatPoint(c.Root.transform, "G09_ExitBlock", c.P(27f, 4f));
            ConfigureJudge(judge, _g09Gazes, exitBlock);

            // 실패해도 G08 끝 안전 발판에서 20초 안에 재도전한다(4.9절).
            var reward = BuildRewardChest(c.Root.transform, "gaze_g09_elite", c.P(24f, 4.5f), 40, false);
            BuildEncounter(c.Root.transform, "gaze_g09", c.P(16f, 7f), new Vector2(22f, 12f), false,
                new[] { new[] { pilgrim, mouth }, new[] { judge } },
                new[] { 1, -1 }, reward, null);

            c.Room("GazeRoom09", 32f, 16f);
        }

        // ---------------- G10 홍채 감시탑 (D4, 중간 보스) ----------------
        static void BuildG10(RoomCtx c)
        {
            c.O = G10;
            // 출구 선반(y=7)까지 한 칸씩 오르는 계단으로 만든다. 예전에는 +4를 한 번에
            // 올라야 해서 실측 점프 높이(2.72)로는 아무도 나갈 수 없었고, 떠 있는 발판
            // 두 장으로 고쳤더니 이번에는 선반 옆면에 걸렸다. 지형 계단이 가장 확실하다.
            c.Floor(0, 16, 3);
            c.Floor(16, 18, 4);
            c.Floor(18, 19, 5);
            c.Floor(19, 20, 6);
            c.Floor(20, 24, 7);   // 출구 (24,7)

            // 체크포인트는 전장 바깥이다 — 재도전 20초 목표를 지키려면 문 앞이어야 한다.
            BuildCheckpoint(c.Root.transform, c.P(1.5f, 4f));

            // 눈꺼풀 닫기에 쓰이는 좌우 벽. 천장에서 내려온 셔터라 바닥에 닿지 않는다.
            // 바닥까지 세우면 입구와 출구를 통째로 막아 방을 지나갈 수 없다(봇이 잡아냈다).
            // 이 공격에는 애초에 피해 판정이 없고(명세 7.1절 "기본 이동으로 대응") 압박은
            // 안전지대가 좁아 보이는 것으로만 만들므로, 발밑을 비워도 의도가 살아 있다.
            var lidL = BuildPlainWall(c.Root.transform, "G10_Lid_L", c.P(5f, 9.5f), new Vector2(1.2f, 7f), GazeStone);
            var lidR = BuildPlainWall(c.Root.transform, "G10_Lid_R", c.P(13f, 9.5f), new Vector2(1.2f, 7f), GazeStone);

            // 중앙 엄폐 기둥. 홍채 훑기를 숨죽이기 대신 엄폐로도 넘길 수 있게 한다.
            BuildCoverPillar(c.Root.transform, "G10_Cover_A", c.P(7f, 5f), new Vector2(1.4f, 3f));
            BuildCoverPillar(c.Root.transform, "G10_Cover_B", c.P(12f, 5f), new Vector2(1.4f, 3f));

            var boss = BuildBoss(c.Root.transform, c.P(9f, 5f), "Enemy_Gaze_Gatekeeper", 14,
                new[] { BossController.Move.GazeSweep, BossController.Move.WallClose, BossController.Move.Charge },
                new[] { 0.5f }, new Color(0.55f, 0.45f, 0.7f));
            ConfigureGazeBoss(boss, new[] { lidL, lidR });

            // 전장을 가두는 벽은 조우가 전투 중에만 세운다(Encounter의 Lock_L/Lock_R).
            // 돌진이 벽에 박히는 것도 그것으로 성립하므로 상시 벽을 따로 두지 않는다.
            var reward = BuildRewardChest(c.Root.transform, "gaze_g10_boss", c.P(6f, 4.5f), 45, false);
            BuildEncounter(c.Root.transform, "gaze_g10_boss", c.P(10f, 7f), new Vector2(14f, 10f), true,
                new[] { new[] { boss } }, new int[0], reward, _gazeShortcutC);

            c.Room("GazeRoom10", 24f, 18f);
        }

        // ---------------- G11 자기 초상의 회랑 (D1→D2) ----------------
        // 전투도 낙사도 없다. 자각 해금과 그 직후의 한 화면 퍼즐(4.11절).
        static void BuildG11(RoomCtx c)
        {
            c.O = G11;
            // 로컬 8~10을 비워 GS3로 내려가는 자리를 만든다. 그 위를 거울문이 막는다.
            c.Floor(0, 8, 3, 2);
            c.Floor(10, 24, 3, 2);
            c.Floor(24, 28, 4, 2);  // 출구 (28,4)

            BuildStoryFragment(c.Root.transform, c.P(4f, 4f), "gaze_g11",
                "거울은 나보다 반 박자 늦게 나를 따라왔다.", EmotionId.None, false);

            // 거울들. 거대 눈에 가까워질수록 플레이어를 늦게 따라온다는 연출은 아트의 몫이다.
            for (int i = 0; i < 3; i++)
                BuildDecor(c.Root.transform, $"G11_Mirror_{i}", c.P(12f + i * 4f, 6.5f), new Vector2(1.6f, 5f),
                    "Tile", GazeMirror, 0f, -5);

            // 자각 해금. 연출 중 이동을 빼앗는 시간은 4초를 넘지 않는다(AwarenessUnlockMoment).
            BuildAwarenessUnlock(c.Root.transform, c.P(14f, 4.5f), c.P(14f, 10f),
                "숨는 대신, 처음으로 정면을 마주 보았다.");

            // 해금 직후의 한 화면 퍼즐 — 자각 중에만 밟히는 표식 발판 3개로 위 보상에 닿는다.
            BuildAwarenessPiece(c.Root.transform, "G11_Mark_A", c.P(18f, 5.5f), new Vector2(2.4f, 0.5f),
                GazeMirror, true, false);
            BuildAwarenessPiece(c.Root.transform, "G11_Mark_B", c.P(21f, 7f), new Vector2(2.4f, 0.5f),
                GazeMirror, true, false);
            BuildAwarenessPiece(c.Root.transform, "G11_Mark_C", c.P(24f, 8.5f), new Vector2(2.4f, 0.5f),
                GazeMirror, true, false);
            BuildRewardChest(c.Root.transform, "gaze_g11_mark", c.P(24f, 9.5f), 20, false);

            // GS3 거울문 — 평소에는 바닥이고, 자각 중에만 사라져 길이 된다.
            BuildAwarenessPiece(c.Root.transform, "G11_MirrorDoor", c.P(9f, 2.75f), new Vector2(2f, 0.5f),
                GazeMirror, true, true);

            c.Room("GazeRoom11", 28f, 16f);
        }

        // ---------------- GS3 안쪽 눈 (D0 선택) ----------------
        // 자각으로만 들어온다. 전투도 낙하도 시간제한도 없는 서사 공간이다(5절).
        static void BuildGS3(RoomCtx c)
        {
            c.O = GS3;
            c.Floor(0, 20, 2);

            BuildDecor(c.Root.transform, "GS3_InnerEye", c.P(10f, 7f), new Vector2(7f, 6f),
                "Eye", new Color(0.5f, 0.45f, 0.65f), 0f, -4);

            BuildStoryFragment(c.Root.transform, c.P(6f, 3f), "gaze_final",
                "아무도 보지 않아도, 나는 나를 보고 있었다.", EmotionId.None, false);

            c.Room("GazeSecret03", 20f, 14f);
        }

        // ---------------- G12 만인의 극장 (D5, 지역 보스) ----------------
        static void BuildG12(RoomCtx c)
        {
            c.O = G12;
            c.Floor(0, 30, 4);

            // 전장을 가두는 벽은 조우(Encounter)가 전투 중에만 세운다. 상시 벽을 방 양끝에
            // 두면 입구와 출구를 그대로 막아 버린다 — 봇이 왼쪽 벽을 벽점프로 넘어야만
            // 들어올 수 있는 상태였다.

            // 전장을 관객석·중앙 무대·좌우 엄폐막 세 층으로 읽히게 만든다(4.12절).
            BuildDecor(c.Root.transform, "G12_Gallery", c.P(15f, 14f), new Vector2(26f, 3f),
                "Tile", new Color(0.22f, 0.2f, 0.32f), 0f, -6);
            BuildCoverPillar(c.Root.transform, "G12_Curtain_L", c.P(6f, 6.5f), new Vector2(1.4f, 4f));
            BuildCoverPillar(c.Root.transform, "G12_Curtain_R", c.P(24f, 6.5f), new Vector2(1.4f, 4f));

            // 관객 조각상 5개. 자각이 없으면 다섯이 같은 예고를 보내고, 자각 중에는 하나만 남는다.
            for (int i = 0; i < 5; i++)
                BuildStatue(c.Root.transform, $"G12_Statue_{i}", c.P(4f + i * 5.5f, 4f), i == 2, i * 0.35f);

            var boss = BuildBoss(c.Root.transform, c.P(15f, 6f), "Enemy_Gaze_AllEyes", 20,
                new[]
                {
                    BossController.Move.GazeSweep,
                    BossController.Move.Slam,
                    BossController.Move.Charge,
                    BossController.Move.GazeSweep,
                },
                new[] { 0.6f, 0.3f }, new Color(0.45f, 0.4f, 0.62f));
            ConfigureGazeBoss(boss, null);

            var reward = BuildRewardChest(c.Root.transform, "gaze_g12_boss", c.P(15f, 5.5f), 70, true);
            BuildEncounter(c.Root.transform, "gaze_g12_boss", c.P(15f, 9f), new Vector2(26f, 12f), true,
                new[] { new[] { boss } }, new int[0], reward, null);

            // 핵심 파편과 균열로 이어지는 통로.
            BuildStoryFragment(c.Root.transform, c.P(26f, 5f), "gaze_core",
                "관객들이 서로를 보기 시작하자, 무대는 그냥 바닥이 되었다.", EmotionId.None, false);
            BuildZoneTrigger(c.Root.transform, c.P(28f, 6f), new Vector2(2f, 4f), false);

            c.Room("GazeRoom12", 30f, 18f);
            BuildBoundary(c.Root.transform, "Gaze_EastBoundary", c.P(30.5f, 0f).x);
        }
    }
}
