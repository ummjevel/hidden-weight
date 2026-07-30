using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
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

            // 겉모습으로는 안전한 발판과 구분되지 않아야 한다(2.1절: "평상시 금이 없거나
            // 매우 약하게 보여 외형만으로 구분할 수 없게 한다"). 그래서 BuildSafePlatform과
            // 같은 시트 셀·같은 크기(3x0.8)를 그대로 쓴다 — 프리팹의 플레이스홀더가 남으면
            // 그림만 보고 안전한 발판을 골라낼 수 있어 이 방들의 설계가 사라진다.
            ReplaceArt(go, "FracturePlatform_r1_c1", new Vector2(3f, 0.8f), 2);
            // 밟은 뒤의 균열·붕괴 상태(FracturePlatformStates 3·4행). 밟기 전에는 안전
            // 발판과 같은 그림이어야 하므로 자동 재생은 끈 채로 붙는다(AttachPlatformStates).
            AttachPlatformStates(go);

            var sr = go.GetComponentInChildren<SpriteRenderer>();
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

            // 공용 BuildSafePlatform과 같은 발판 아트를 쓴다. 여기만 플레이스홀더로 두면
            // 같은 방의 고정 발판과 회전 발판이 서로 다른 그림으로 보인다.
            // 스케일 1 자식에 그리는 이유는 ApplyPlatformArt 주석 참고(콜라이더 보호).
            ApplyPlatformArt(go, FractureStone);

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
            // 미래 구조물의 잔상. 사각 타일 대신 예지 기물 셀을 쓴다 — 반투명 톤은 유지한다.
            var ghostArt = Art("FractureForesight_r1_c1");
            sr.sprite = ghostArt != null ? ghostArt : LoadSprite("Tile");
            sr.color = FractureGhost;
            sr.sortingOrder = 3;
            if (ghostArt != null)
            {
                visual.transform.localScale = Vector3.one;
                FitSprite(sr, size.x, size.y);
            }

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
            // 닫힌 문의 겉모습. 열리면 blocker째 꺼지므로 열림 그림은 필요 없다.
            ApplyArtOverlay(blocker, _shortcutClosedArt, size, 5);

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
                ApplyArtOverlay(solid, "FracturePlatform_r1_c2", new Vector2(6f, 0.6f), 2);
                solid.SetActive(false);
                solids[i] = solid;

                var preview = new GameObject($"Preview_{i}");
                preview.transform.SetParent(go.transform, false);
                preview.transform.position = new Vector3(branches[i].platform.x, branches[i].platform.y, 0f);
                preview.transform.localScale = new Vector3(6f, 0.6f, 1f);
                var sr = preview.AddComponent<SpriteRenderer>();
                // 아직 선택되지 않은 갈래의 잔상. 실제 발판과 같은 그림이어야
                // "이 자리에 발판이 올 수 있다"로 읽힌다.
                var previewArt = Art("FracturePlatform_r1_c2");
                sr.sprite = previewArt != null ? previewArt : LoadSprite("Tile");
                sr.color = FractureGhost;
                sr.sortingOrder = 4;
                if (previewArt != null)
                {
                    preview.transform.localScale = Vector3.one;
                    FitSprite(sr, 6f, 0.6f);
                }
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

        // 적 종류별 애니메이션. 시트는 4행(idle / movement / attack / hit·death)이라
        // 공용 EnemyClips와 행 수가 정확히 맞는다. 2행 이름이 Walk인 이유는
        // FractureAnimationArtSlicer 주석에 있다.
        static (string, float, bool)[] FractureEnemyClips(string prefix) => new[]
        {
            ($"{prefix}Idle",   8f,  true),
            ($"{prefix}Walk",   10f, true),
            ($"{prefix}Attack", 14f, false),
            ($"{prefix}Hit",    12f, false),
        };

        // 겉모습을 균열 전용 스프라이트로 바꾼다. 루트 스케일은 건드리지 않는다 —
        // 콜라이더가 함께 줄어 판정이 통째로 어긋난다(잔재·응시에서 겪은 문제 그대로다).
        static void ApplyFractureEnemyArt(GameObject enemy, FractureEnemyKind kind)
        {
            string prefix = kind.ToString();
            var idle = Art($"{prefix}Idle_00");
            if (idle == null)
            {
                // 조용히 넘어가면 그 적만 플레이스홀더 사각형으로 남는다. 아트가 죽는 자리는
                // 늘 여기라서 빠진 것은 반드시 눈에 띄게 남긴다.
                Debug.LogWarning($"[FractureZoneBuilder] {prefix} 아트를 찾지 못했다: {prefix}Idle_00");
                return;
            }

            var rootRenderer = enemy.GetComponent<SpriteRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            var artObject = new GameObject("Art");
            artObject.transform.SetParent(enemy.transform, false);

            var artRenderer = artObject.AddComponent<SpriteRenderer>();
            artRenderer.sprite = idle;
            artRenderer.color = Color.white;
            artRenderer.sortingOrder = 8;

            // 갈라진 자아만 정예라 한 뼘 크게 보인다.
            float displayHeight = kind == FractureEnemyKind.SplitSelf ? 1.8f : 1.3f;
            var size = idle.bounds.size;
            if (size.y > 0f)
            {
                float scale = displayHeight / size.y;
                artObject.transform.localScale = new Vector3(scale, scale, 1f);
            }

            AttachAnimator(artObject, artRenderer, FractureEnemyClips(prefix), displayHeight);
            SetField(enemy.GetComponent<Enemy>(), "clipPrefix", p => p.stringValue = prefix);
        }

        static GameObject BuildFractureEnemy(Transform parent, Vector2 pos, FractureEnemyKind kind)
        {
            var go = BuildEnemy(parent, pos, FractureEnemyData(kind));
            ApplyFractureEnemyArt(go, kind);
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

            // 거울상은 본체와 완전히 같아 보여야 한다 — 본체에 균열 아트를 씌우면서 거울상만
            // 플레이스홀더로 남기면 실루엣만 보고 실체를 골라낼 수 있어 이 전투가 사라진다.
            //
            // 배율은 반드시 자식에 준다. SplitSelfBehavior.UpdateMirror가 매 프레임
            // mirror.localScale을 본체 스케일로 덮어쓰므로 루트에 준 배율은 첫 프레임에 사라진다.
            var mirrorArt = Art("SplitSelfIdle_00");
            if (mirrorArt != null)
            {
                mirrorSr.enabled = false;

                var mirrorArtObject = new GameObject("Art");
                mirrorArtObject.transform.SetParent(mirror.transform, false);

                var mirrorArtRenderer = mirrorArtObject.AddComponent<SpriteRenderer>();
                mirrorArtRenderer.sprite = mirrorArt;
                mirrorArtRenderer.color = Color.white;
                mirrorArtRenderer.sortingOrder = 7;

                const float mirrorHeight = 1.8f;   // ApplyFractureEnemyArt의 정예 높이와 같다
                var size = mirrorArt.bounds.size;
                if (size.y > 0f)
                {
                    float scale = mirrorHeight / size.y;
                    mirrorArtObject.transform.localScale = new Vector3(scale, scale, 1f);
                }

                AttachAnimator(mirrorArtObject, mirrorArtRenderer,
                    FractureEnemyClips("SplitSelf"), mirrorHeight);
            }

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
            BuildShaft(parent, map, "Shaft_FS3", F11.x + 8, F11.y + 1, FS3.y + 2);
        }

        [MenuItem("Hidden Weight/Build Fracture Zone (Full)")]
        public static void RunFractureZone()
        {
            EnsureScenesFolder();

            // 시트 분할을 씬 생성에 묶어 둔다. 메뉴로 따로 돌리게 두면 "잘라 두는 것을 잊고
            // 씬만 다시 지어" 아트가 전부 플레이스홀더로 돌아가는 실패가 반복된다.
            FractureEnvironmentArtSlicer.SliceAll();
            FractureAnimationArtSlicer.SliceAll();

            // 균열 시트는 전부 "Fracture" 접두사를 쓴다(FractureTerrain / FracturePlatform …).
            // 숏컷 겉모습은 문 시트(Fracture_DoorsShortcuts_v1)의 1행을 닫힘/열림으로,
            // 2행을 승강기용으로 쓴다.
            UseArtRoot("Assets/Art/Fracture", "Fracture",
                "FractureDoor_r1_c1", "FractureDoor_r1_c2",
                "FractureDoor_r2_c1", "FractureDoor_r2_c2");

            var scene = NewScene();
            var tilemap = BuildZoneRoot("Fracture", out var root);
            tilemap.GetComponent<TilemapRenderer>().enabled = false;
            TintTerrain(tilemap, FractureTerrain);

            var rooms = new GameObject("Rooms");
            rooms.transform.SetParent(root.transform, true);

            var marker = new GameObject("ZoneMarker");
            marker.transform.SetParent(root.transform, false);
            var zoneMarker = marker.AddComponent<HiddenWeight.Core.ZoneMarker>();
            SetField(zoneMarker, "zone", p => p.enumValueIndex = (int)ZoneId.Fracture);

            // 이제 균열 전용 지형 시트가 있으므로 바닥 표면 아트를 켠다(응시와 같은 전환).
            var ctx = new RoomCtx
            {
                Map = tilemap,
                Root = root,
                Rooms = rooms.transform,
                FloorArt = false
            };

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

            // 충돌 연출과 공격체 발사대는 지역에 하나씩 둔다.
            BuildImpactVFX(root.transform, FractureImpacts);
            BuildProjectileSpawner(root.transform, FractureProjectiles);

            // 숏컷 3곳과 FS3 입구 벽에 봉쇄·해제 애니메이션을 붙인다.
            AttachFractureSealAnimator(_fractureShortcutA, new Vector2(4f, 2f));
            AttachFractureSealAnimator(_fractureShortcutB, new Vector2(3f, 2.5f));
            AttachFractureSealAnimator(_fractureShortcutC, new Vector2(4f, 2f));
            AttachFractureSealAnimator(_fractureSecretDoor, new Vector2(3f, 2.5f));

            // 방마다 원본 콘셉트 한 장만 카메라에 고정한다.
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Fracture");

            HideCollisionPlaceholderRenderers(root);
            SaveScene(scene, "Zone_Fracture_Full");
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FractureZoneBuilder] 균열 전체 지역(15룸) 생성 완료");
        }

        // ------------------------------------------------------------
        // 균열 지역의 연출 목록. 잔재·응시와 같은 모듈을 쓰고 이름과 수치만 다르다.
        // ------------------------------------------------------------

        // 타격 연출 이름에 지역 접두사를 붙이지 않는다 — 런타임이 "ImpactMelee"/"ImpactLand"를
        // 이름 그대로 부르기 때문이다(PlayerAttack, PlayerController, BossController).
        // 지역별 분리는 아트 폴더가 이미 보장한다.
        static readonly (string name, string clip, float fps, float height)[] FractureImpacts =
        {
            ("ImpactMelee", "ImpactMelee", 18f, 1.4f),
            ("ImpactHeavy", "ImpactHeavy", 16f, 1.8f),
            ("ImpactLand",  "ImpactLand",  14f, 1.0f),
            ("ImpactWall",  "ImpactWall",  16f, 1.6f),
            ("BossRing",    "BossRing",    14f, 3.5f),   // 보스 낙하 착지 고리
            ("BossRupture", "BossRupture", 12f, 3.0f),   // 단계 전환 파열
        };

        static readonly (string name, float fps, float speed, float lifetime,
                         float radius, int damage, float height, bool ignoreTerrain)[] FractureProjectiles =
        {
            ("FractureProjShards",    14f, 8f,   2.0f, 0.5f, 1, 1.0f, false),
            ("FractureProjArc",       14f, 6f,   2.2f, 0.7f, 1, 1.2f, false),
            ("FractureProjRing",      12f, 5f,   2.4f, 0.8f, 1, 1.4f, true),
            ("FractureBossShard",     16f, 10f,  2.0f, 0.5f, 1, 1.2f, false),
            ("FractureBossCrystals",  14f, 7f,   2.6f, 0.7f, 1, 1.6f, false),
        };

        static readonly (string clip, float fps)[] FractureBackgroundMotions =
        {
            ("FractureBgArches", 4f), ("FractureBgWater", 5f), ("FractureBgSkyCrack", 3f),
            // 앰비언트 시트의 잔잔한 행을 원경 변형으로 섞는다 — 이웃 방이 같은 움직임을
            // 반복하지 않게 하는 변형 풀을 넓힌다(GAME_DESIGN.md의 랜드마크 규칙).
            ("FractureAmbientPetals", 5f), ("FractureAmbientMist", 4f), ("FractureAmbientGlass", 5f),
        };

        // 전경 모션 표가 없는 것은 의도다. FractureForegroundMotion_v1의 세 행(줄기꽃·꽃·유리
        // 굴절)은 방 높이의 45%로 커져 방 중앙에 놓이는데, 균열 아트 설계 1절이 전경 요소를
        // "화면 가장자리"로 한정하고 중앙 65%를 비우라고 못박고 있다. 실제로 붙여 보니
        // 검은 여백 위에 줄기 세 개가 떠 있는 모양이 됐다. 전경 연출은 FG_Overlay 45장이 맡는다.

        // 숏컷의 봉쇄·해제 애니메이션(DoorShortcutTransitions_v1의 1·2행).
        static void AttachFractureSealAnimator(Shortcut shortcut, Vector2 size)
        {
            if (shortcut == null) return;

            var sealObject = new GameObject("SealAnimation");
            sealObject.transform.SetParent(shortcut.transform, false);

            var renderer = sealObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 6;

            AttachAnimator(sealObject, renderer, new[]
            {
                ("FractureSealClose", 10f, false),
                ("FractureSealOpen", 10f, false),
            }, size.y);

            var animator = sealObject.GetComponent<SpriteAnimator>();
            if (animator == null) { Object.DestroyImmediate(sealObject); return; }

            SetField(animator, "autoPlay", p => p.boolValue = false);
            SetField(shortcut, "transitionAnimator", p => p.objectReferenceValue = animator);
            SetField(shortcut, "closedClip", p => p.stringValue = "FractureSealClose");
            SetField(shortcut, "openClip", p => p.stringValue = "FractureSealOpen");
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
                    "FractureDoor_r1_c3", FractureStone, 0f, -5);
                BuildDecor(c.Root.transform, $"F01_DoubleEdge_{i}_Echo", c.P(x + 0.25f, 4.6f), new Vector2(1.6f, 3f),
                    "FractureDoor_r1_c3", new Color(1f, 1f, 1f, 0.35f), 0f, -6);
            }

            // 하늘의 세로 균열 — 지역 전체의 진행 방향이자 결말(2.3절 랜드마크).
            BuildDecor(c.Root.transform, "F01_SkyFracture", c.P(21f, 12f), new Vector2(0.8f, 16f),
                "Tile", new Color(0.9f, 0.86f, 0.95f), 6f, -8);

            // 방 끝에서, 닿기도 전에 스스로 무너져 있는 발판 조각을 보여준다(4.1절).
            BuildDecor(c.Root.transform, "F01_BrokenPlatform_A", c.P(22f, 6f), new Vector2(1.4f, 0.5f),
                "FracturePlatform_r2_c1", FractureStone, -18f, -3);
            BuildDecor(c.Root.transform, "F01_BrokenPlatform_B", c.P(23.6f, 5.2f), new Vector2(1f, 0.5f),
                "FracturePlatform_r2_c2", FractureStone, 24f, -3);

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
                "FractureDoor_r1_c4", FractureStone, 0f, -4);

            // 랜드마크: 떠 있는 시계탑과 하늘 균열을 중앙에서 동시에 본다(4.3절).
            BuildDecor(c.Root.transform, "F03_ClockTower", c.P(10f, 13f), new Vector2(3.4f, 12f),
                "FractureTransit_r1_c2", new Color(0.86f, 0.83f, 0.94f), -3f, -7);
            BuildDecor(c.Root.transform, "F03_InvertedGreenhouse", c.P(20f, 14f), new Vector2(6f, 5f),
                "FractureProp_r1_c3", new Color(0.8f, 0.92f, 0.88f), 184f, -7);
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

            // 첫 화단만 주 동선에서 주기를 읽는다. 아래 두 단계는 안전 발판이라 첫 방문의
            // 관찰 학습은 남기면서 재방문 하강이 반복 대기로 늘어나지 않는다.
            BuildMovingPlatform(c.Root.transform, c.P(12f, 15f), new Vector2(2f, 0f), 6f);
            BuildFractureSafePlatform(c.Root.transform, c.P(15f, 11f));
            BuildFractureSafePlatform(c.Root.transform, c.P(12f, 7f));
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
                    "FractureDoor_r2_c3", new Color(0.86f, 0.84f, 0.94f, 0.7f), i * 3f - 5f, -6);

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
                "FractureForesight_r1_c2", FracturePeach, 0f, -4);

            // 주 동선에서 읽는 이동 발판은 2개(3초 / 5초)만 남긴다.
            BuildMovingPlatform(c.Root.transform, c.P(9f, 6f), new Vector2(3f, 0f), 3f);
            BuildMovingPlatform(c.Root.transform, c.P(15f, 7f), new Vector2(3f, 0f), 5f);

            // 세 번째 7초 발판과 두 번째 선구는 상단 보상 분기다. 아래 주 동선의 점프
            // 높이에 발판 밑면이 걸리지 않으므로 숙련자는 기다리지 않고 출구로 달린다.
            BuildFractureSafePlatform(c.Root.transform, c.P(18f, 6.5f));
            c.Tiles(19, 27, 9, 10);
            BuildMovingPlatform(c.Root.transform, c.P(22f, 12f), new Vector2(3f, 0f), 7f);

            BuildFractureEnemy(c.Root.transform, c.P(12f, 3f), FractureEnemyKind.Precursor);
            BuildFractureEnemy(c.Root.transform, c.P(23f, 11f), FractureEnemyKind.Precursor);

            for (int i = 0; i < 5; i++)
                BuildCurrencyPickup(c.Root.transform, c.P(20f + i * 1.2f, 11f));

            // 거꾸로 선 온실 — F03에서 보이던 랜드마크에 여기서 도달한다(2.3절).
            BuildDecor(c.Root.transform, "F06_Greenhouse", c.P(16f, 12f), new Vector2(18f, 6f),
                "FractureProp_r1_c3", new Color(0.8f, 0.92f, 0.88f), 182f, -7);

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
                    "FractureProp_r2_c3", new Color(0.92f, 0.88f, 0.8f), 0f, -6);

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
            // 건축물 사이 간격을 전부 3유닛 이하로 둔다. 예전에는 6유닛·8유닛 구덩이를
            // 세로로 움직이는 발판 하나로만 건너게 해서, 타이밍을 놓치면 그대로 추락했다
            // (봇이 x=12에서 떨어졌다). 예지는 "언제 뛸지"를 고르게 하는 능력이지
            // 없으면 못 건너게 만드는 장치가 아니다(명세 1.1절).
            c.Floor(0, 10, 4);
            c.Floor(14, 20, 4);    // 중간의 넓은 건축물 = 복귀 지점
            c.Floor(28, 34, 4);    // 출구 (34,4)

            BuildFractureSafePlatform(c.Root.transform, c.P(12f, 5f));    // 10.5~13.5
            BuildFractureSafePlatform(c.Root.transform, c.P(22f, 5f));    // 20.5~23.5
            BuildFractureCrumbling(c.Root.transform, c.P(25.5f, 5.5f));   // 24~27

            // 수평·수직 경로가 교차하지만 실제 충돌 시점은 겹치지 않는다.
            // 이 둘은 큰 재화로 가는 위쪽 선택 경로다 — 주 동선의 필수 발판이 아니다.
            BuildMovingPlatform(c.Root.transform, c.P(10f, 8f), new Vector2(0f, 4f), 4f);
            BuildOrbitPlatform(c.Root.transform, "F07_Orbit_A", c.P(24f, 9f), new Vector2(0f, 3f), 60f, 0f);

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

            // 응시 G08과 같은 이유로 승강 기둥(x 4.5~7.5) 위는 비워 둔다.
            c.Tiles(10, 16, 12, 13);
            c.Tiles(10, 16, 19, 20);
            c.Tiles(8, 24, 25, 26);    // 북동 출구 선반 (22,26)

            // 첫 웨이포인트가 아래를 향한다 — 이것이 "역행"의 전부다. 그다음은 곧게 위로만.
            BuildLift(c.Root.transform, "F08_Lift", c.P(6f, 2.6f),
                new[] { new Vector2(0f, -0.5f), new Vector2(0f, 22.6f) },
                _fractureShortcutB, new Color(0.7f, 0.9f, 0.85f));

            // 승강기를 놓치고 오른쪽으로 계속 걸어도 허공에 떨어지지 않게 막는다.
            var edge = BuildSolidBlock(c.Root.transform, "F08_RightEdge",
                c.P(24.5f, 12f), new Vector2(1f, 26f), "Ground");
            edge.GetComponent<SpriteRenderer>().enabled = false;

            BuildFractureSafePlatform(c.Root.transform, c.P(12f, 16f));
            BuildFractureSafePlatform(c.Root.transform, c.P(12f, 23f));

            BuildDecor(c.Root.transform, "F08_NoDoorYet", c.P(20f, 30f), new Vector2(3f, 5f),
                "FractureDoor_r2_c4", new Color(0.95f, 0.93f, 1f, 0.5f), 0f, -7);

            BuildHealingPickup(c.Root.transform, c.P(20f, 27f));

            c.Room("FractureRoom08", 24f, 30f);
        }

        // ---------------- F09 거울 가능성실 (D4, 정예) ----------------
        // 일반 전투는 주 동선, 좌우 대칭을 읽는 분열체 정예전은 상단 선택 분기다.
        static void BuildF09(RoomCtx c)
        {
            c.O = F09;
            c.Floor(0, 28, 3);
            c.Floor(28, 32, 4);   // 출구 (32,4)

            // 정예 조우 전에 방 전체를 내려다보는 관찰대.
            c.Tiles(2, 8, 9, 10);
            c.Tiles(18, 29, 9, 10);

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
            var collector = BuildFractureEnemy(c.Root.transform, c.P(19f, 4f), FractureEnemyKind.Collector);

            var split = BuildFractureEnemy(c.Root.transform, c.P(23f, 11f), FractureEnemyKind.SplitSelf);
            ConfigureSplitSelf(split, c.Root.transform, c.P(16f, 0f).x);

            BuildEncounter(c.Root.transform, "fracture_f09_main", c.P(16f, 5f), new Vector2(22f, 6f), false,
                new[] { new[] { sproutA, collector } }, System.Array.Empty<int>(), null, null);

            var reward = BuildRewardChest(c.Root.transform, "fracture_f09_elite", c.P(27f, 11.5f), 40, false);
            BuildEncounter(c.Root.transform, "fracture_f09_elite", c.P(23.5f, 12f), new Vector2(11f, 6f), true,
                new[] { new[] { split } }, System.Array.Empty<int>(), reward, null);

            c.Room("FractureRoom09", 32f, 16f);
        }

        // ---------------- F10 초침 감시탑 (D4, 중간 보스) ----------------
        static void BuildF10(RoomCtx c)
        {
            c.O = F10;
            // 응시 G10과 같은 이유로 출구까지 한 칸씩 오르는 계단을 둔다(+4를 한 번에
            // 오르는 구조는 실측 점프 높이 2.72로 통과 불가).
            c.Floor(0, 16, 3);
            c.Floor(16, 18, 4);
            c.Floor(18, 19, 5);
            c.Floor(19, 20, 6);
            c.Floor(20, 24, 7);   // 출구 (24,7)

            BuildCheckpoint(c.Root.transform, c.P(2f, 4f)); // 체크포인트 3 — 전장 바깥

            // 시계바늘 발판. 일정한 주기라 관찰로 배울 수 있고, 예지 고스트와 정확히 일치한다.
            BuildOrbitPlatform(c.Root.transform, "F10_Hand_Long", c.P(9f, 9f), new Vector2(0f, -3f), 40f, 0f);
            BuildOrbitPlatform(c.Root.transform, "F10_Hand_Short", c.P(13f, 11f), new Vector2(-2f, -4f), 25f, 90f);

            var boss = BuildBoss(c.Root.transform, c.P(9f, 5f), "Enemy_Fracture_SecondHand", 15,
                new[] { BossController.Move.GroundSweep, BossController.Move.TimeSkip, BossController.Move.Charge },
                new[] { 0.5f }, new Color(0.8f, 0.76f, 0.92f),
                new[]
                {
                    ("SecondHandIdle",      8f,  true),
                    ("SecondHandStalk",     10f, true),
                    ("SecondHandSlash",     14f, false),
                    ("SecondHandDelayed",   12f, false),
                    ("SecondHandTimeBolt",  12f, false),
                    ("SecondHandHit",       12f, false),
                    ("SecondHandDeath",     10f, false),
                    ("SecondHandTeleport",  14f, false),
                    ("SecondHandPhase",     10f, false),
                },
                "SecondHandIdle_00",
                clipPrefix: "SecondHand",
                moveClips: new[] { "SecondHandSlash", "SecondHandTeleport", "SecondHandDelayed" },
                phaseClip: "SecondHandPhase");
            SetField(boss.GetComponent<BossController>(), "projectileName",
                p => p.stringValue = "FractureBossShard");

            // 전장을 가두는 벽은 조우가 전투 중에만 세운다(Encounter의 Lock_L/Lock_R).
            var reward = BuildRewardChest(c.Root.transform, "fracture_f10_boss", c.P(6f, 4.5f), 45, false);
            BuildEncounter(c.Root.transform, "fracture_f10_boss", c.P(10f, 7f), new Vector2(14f, 10f), true,
                new[] { new[] { boss } }, new int[0], reward, _fractureShortcutC);

            c.Room("FractureRoom10", 24f, 18f);
        }

        // ---------------- F11 아직 오지 않은 폐허 (D2) ----------------
        // 전투 없는 미래 서사. 고스트는 절대 충돌 지형이 되지 않는다(4.11절).
        static void BuildF11(RoomCtx c)
        {
            c.O = F11;
            // 로컬 8~9를 비워 FS3로 내려가는 자리를 만든다. 그 위를 "아직 없는 문"이 막는다.
            // 폭을 1로 좁혀 둔 이유: 문이 한 번 열리면 이 구멍은 계속 열린 채로 남는데,
            // 주 동선이 바로 위를 지나가므로 넓으면 지날 때마다 빠진다.
            c.Floor(0, 8, 3, 2);
            c.Floor(9, 24, 3, 2);
            c.Floor(24, 28, 4, 2);  // 출구 (28,4)

            BuildStoryFragment(c.Root.transform, c.P(4f, 4f), "fracture_f11",
                "무너진 뒤의 모습만 남아, 아직 무너지지 않았다.", EmotionId.None, false);

            // 현재에는 기초와 문틀만 있다.
            for (int i = 0; i < 4; i++)
                BuildDecor(c.Root.transform, $"F11_Foundation_{i}", c.P(13f + i * 3.5f, 3.6f), new Vector2(2.4f, 0.8f),
                    "FracturePlatform_r3_c1", FractureStone, 0f, -3);

            // 예지 안에서만 완성된 폐허의 윤곽이 나타난다. 밟을 수는 없다(solidWhenFixed=false).
            for (int i = 0; i < 4; i++)
                BuildFutureEcho(c.Root.transform, $"F11_Ruin_{i}", c.P(13f + i * 3.5f, 7f), new Vector2(2.4f, 6f),
                    0, null, false, new Vector2(0.15f * (i % 2 == 0 ? 1f : -1f), 0.2f));

            // FS3 입구를 막는 벽. 세 번의 예지에서 같은 자리에 나타난 문이 현재에 고정되면 열린다.
            _fractureSecretDoor = BuildBlockingDoor(c.Root.transform, "fracture_secret_door", c.P(8.5f, 2.75f),
                new Vector2(1f, 0.5f), FractureStone);
            BuildFutureEcho(c.Root.transform, "F11_UnchosenDoor", c.P(8.5f, 5.5f), new Vector2(2.2f, 4f),
                3, _fractureSecretDoor, false);

            // 방 끝에서 F12 전장이 한 번 무너졌다 돌아오는 모습을 안전하게 본다.
            BuildDecor(c.Root.transform, "F11_DistantArena", c.P(26f, 10f), new Vector2(8f, 6f),
                "FractureTransit_r1_c1", new Color(0.88f, 0.84f, 0.94f, 0.6f), 0f, -7);

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
                    "FractureDoor_r2_c3", new Color(0.93f, 0.91f, 1f, 0.45f), (i - 2) * 4f, -5);

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

            var wallL = BuildSolidBlock(c.Root.transform, "F12_Wall_L", c.P(0.5f, 9f), new Vector2(1.2f, 10f), "Wall", FractureStone);
            var wallR = BuildSolidBlock(c.Root.transform, "F12_Wall_R", c.P(29.5f, 9f), new Vector2(1.2f, 10f), "Wall", FractureStone);
            ApplyBlockArt(wallL, new Vector2(1.2f, 10f));
            ApplyBlockArt(wallR, new Vector2(1.2f, 10f));

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
                new[] { 0.6f, 0.3f }, new Color(0.72f, 0.68f, 0.9f),
                new[]
                {
                    ("NotYetMeIdle",      8f,  true),
                    ("NotYetMeGlide",     10f, true),
                    ("NotYetMeRibbon",    14f, false),
                    ("NotYetMeShards",    12f, false),
                    ("NotYetMeStagger",   10f, false),
                    ("NotYetMeHit",       12f, false),
                    ("NotYetMeDeath",     10f, false),
                    ("NotYetMeDivided",   10f, false),
                    ("NotYetMePhase",     10f, false),
                    ("NotYetMeAcceptance", 8f, false),
                },
                "NotYetMeIdle_00",
                clipPrefix: "NotYetMe",
                moveClips: new[] { "NotYetMeDivided", "NotYetMeShards",
                                   "NotYetMeGlide", "NotYetMeRibbon" },
                phaseClip: "NotYetMePhase");
            SetField(boss.GetComponent<BossController>(), "projectileName",
                p => p.stringValue = "FractureBossCrystals");

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
