using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.World;
using HiddenWeight.Enemies;
using HiddenWeight.UI;
using HiddenWeight.Ending;

namespace HiddenWeight.EditorTools
{
    // 씬 7개(Bootstrap/Title/지역 4곳/Ending)를 코드로 조립하고 EditorBuildSettings에 등록한다.
    // ProjectSetup.Run()은 절대 호출하지 않는다 — URP 에셋이 새 GUID로 재생성되어 참조가 깨진다.
    // Volume 프로파일은 항상 기존 ZoneData 에셋의 참조를 그대로 읽어 쓴다.
    public static partial class ZoneSceneBuilder
    {
        const string ScenesFolder = "Assets/Scenes";
        const string PrefabFolder = "Assets/Prefabs";
        const string DataFolder = "Assets/ScriptableObjects";
        const string ArtFolder = "Assets/Art/Placeholder";

        public static void Run()
        {
            EnsureScenesFolder();

            BuildBootstrap();
            BuildTitle();
            BuildZonePrologue();
            // 잔재는 방마다 씬을 굽고 Zone_Residue를 그 셸로 만든다. 옛 4룸 MVP 빌더가 같은
            // 이름에 덮어쓰던 것을 대체한 것이라, 여기서 부르지 않으면 셸이 조용히 지워진다.
            BuildResidueRooms();
            BuildZoneGaze();
            BuildZoneFracture();
            BuildEnding();

            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ZoneSceneBuilder] 씬 7개 생성 완료");
        }

        // 튜토리얼 작업 중 다른 지역 씬을 다시 저장하지 않기 위한 제한된 진입점.
        // 오늘 제작 범위인 Zone_Prologue만 생성하고 나머지 씬은 절대 건드리지 않는다.
        public static void BuildPrologueOnly()
        {
            EnsureScenesFolder();
            BuildZonePrologue();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ZoneSceneBuilder] Zone_Prologue 전용 생성 완료");
        }

        // ============================================================
        // 공통 헬퍼
        // ============================================================

        static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static void SaveScene(Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{name}.unity");
        }

        // 잔재 전용 아트를 이름으로 집는다. 잔재 이외 지역(프롤로그·응시·균열)에서는 없을 수도
        // 있으므로 null이면 호출부가 기존 플레이스홀더로 넘어간다.
        static Sprite ResidueArt(string spriteName) => Art(spriteName);

        // 스프라이트를 원하는 월드 크기에 맞춰 늘린다.
        static void FitRenderer(SpriteRenderer renderer, float width, float height)
            => FitSprite(renderer, width, height);

        static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
        static T LoadData<T>(string name) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{name}.asset");

        static GameObject Spawn(string prefabName, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        static void SetField(Object target, string propertyName, System.Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            apply(so.FindProperty(propertyName));
            so.ApplyModifiedProperties();
        }

        // Tile 에셋(1회 생성, 이후 재사용). Tile.png 스프라이트를 그대로 얹는다.
        static Tile _groundTile;
        static Tile GroundTile()
        {
            if (_groundTile != null) return _groundTile;

            const string path = "Assets/Art/Placeholder/GroundTile.asset";
            _groundTile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (_groundTile != null) return _groundTile;

            _groundTile = ScriptableObject.CreateInstance<Tile>();
            _groundTile.sprite = LoadSprite("Tile");
            AssetDatabase.CreateAsset(_groundTile, path);
            return _groundTile;
        }

        // 셀 사각형 [xMin,xMax) x [yMin,yMax)를 타일로 채운다.
        static void PlaceTiles(Tilemap tilemap, TileBase tile, int xMin, int xMax, int yMin, int yMax)
        {
            for (int x = xMin; x < xMax; x++)
                for (int y = yMin; y < yMax; y++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        // 표면 높이 topY, 깊이 depth(기본 6)의 solid 바닥 한 구간을 채운다.
        static void Floor(Tilemap tilemap, int xMin, int xMax, int topY, int depth = 6)
            => PlaceTiles(tilemap, GroundTile(), xMin, xMax, topY - depth, topY);

        // Grid + Tilemap(Ground, TilemapCollider2D) 생성.
        //
        // CompositeCollider2D는 쓰지 않는다. 붙여 보면 씬에 저장된 컴포짓이 런타임에 형상을
        // 하나도 만들지 못해(pathCount=0) 지역 전체에 바닥 충돌이 사라지고, 플레이어가 시작과
        // 동시에 무한 낙하한다. 부착 순서를 바꿔도, 저장 직전 GenerateGeometry()를 불러도,
        // 로드 후 다시 불러도 0이었다 — 런타임에 컴포짓을 새로 붙였을 때만 살아난다.
        // TilemapCollider2D 단독은 타일 444개에 대해 형상을 정상 생성하므로(레이캐스트가 바닥
        // 표면 y를 정확히 맞춘다) 컴포짓 없이 간다. 대가는 타일 경계가 병합되지 않는다는 것뿐이고,
        // 플레이어가 CapsuleCollider2D라 이음새에 걸릴 위험은 낮다.
        // 검증: Assets/Tests/PlayMode/ZonePlayableTests.cs
        static Tilemap BuildGroundGrid(out GameObject gridGO)
        {
            gridGO = new GameObject("Grid");
            gridGO.AddComponent<Grid>();

            var tilemapGO = new GameObject("Tilemap");
            tilemapGO.transform.SetParent(gridGO.transform, false);
            tilemapGO.layer = LayerMask.NameToLayer("Ground");

            var tilemap = tilemapGO.AddComponent<Tilemap>();
            tilemapGO.AddComponent<TilemapRenderer>();

            tilemapGO.AddComponent<TilemapCollider2D>();

            var rb = tilemapGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            return tilemap;
        }

        static GameObject BuildRoom(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            var room = go.AddComponent<Room>();
            SetField(room, "size", p => p.vector2Value = size);

            col.isTrigger = true;
            col.size = size;
            col.offset = Vector2.zero;

            return go;
        }

        static void BuildZoneVolume(Transform parent, string zoneAssetName)
        {
            var zoneData = LoadData<ZoneData>($"Zone_{zoneAssetName}");

            var go = new GameObject("ZoneVolume");
            go.transform.SetParent(parent, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.weight = 1f;
            vol.sharedProfile = zoneData != null ? zoneData.volumeProfile : null;
        }

        // 1x1 Tile 스프라이트를 늘려 쓰는 단순 블록(벽·안전 발판 등). localScale로 크기를 낸다.
        static GameObject BuildSolidBlock(Transform parent, string name, Vector2 center, Vector2 size, string layerName, Color? tint = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.layer = LayerMask.NameToLayer(layerName);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Tile");
            if (tint.HasValue) sr.color = tint.Value;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            return go;
        }

        // 충돌 지오메트리는 반드시 눈에 보여야 한다. 처음에는 4K 배경이 지형 외형을
        // 담당한다는 가정으로 단색 Tile을 전부 숨겼는데, 배경은 카메라에 고정된 벽지라
        // (CameraLockedRoomBackground) 월드에 고정된 벽·천장을 그려 줄 수 없다 — 결과가
        // "안 보이는데 부딪히는 벽"이었다. 그래서 숨기는 대신 지역 지형 아트를 입힌다.
        // 잠금벽(비활성)·트리거·이미 아트가 붙은 블록(루트 렌더러 꺼짐)은 건너뛴다.
        static void ClotheCollisionPlaceholderRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || renderer.sprite.name != "Tile") continue;
                if (!renderer.enabled) continue;                       // 이미 아트로 대체됨
                var col = renderer.GetComponent<Collider2D>();
                if (col == null || col.isTrigger) continue;            // 장식·트리거는 그대로

                var size = Vector2.Scale(col is BoxCollider2D box ? box.size : Vector2.one,
                                         renderer.transform.lossyScale);
                ApplyBlockArt(renderer.gameObject, size,
                              renderer.sortingOrder == 0 ? 1 : renderer.sortingOrder);
            }
        }

        // 균열 지역의 "안전한" 발판. CrumblingPlatform과 같은 Platform 스프라이트(96x16px, 이미
        // 3x0.5 유닛 네이티브 크기)를 그대로 써서 시각적으로 구분되지 않게 한다 — 결코 무너지지
        // 않는다는 점만 다르다.
        static GameObject BuildSafePlatform(Transform parent, Vector2 pos)
        {
            var go = new GameObject("SafePlatform");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.layer = LayerMask.NameToLayer("Ground");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Platform");
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            // 지역 아트는 루트가 아니라 스케일 1 자식에 그린다. 루트 스케일로 맞추면
            // 콜라이더가 같이 줄어 발판이 슬리버가 된다(ApplyPlatformArt 주석 참고).
            ApplyPlatformArt(go);

            return go;
        }

        static GameObject BuildZoneTrigger(Transform parent, Vector2 center, Vector2 size, bool marksFractureCleared)
        {
            var go = new GameObject("ZoneTrigger");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var trigger = go.AddComponent<ZoneTrigger>();
            if (marksFractureCleared) SetField(trigger, "marksFractureCleared", p => p.boolValue = true);

            return go;
        }

        static GameObject BuildStoryFragment(Transform parent, Vector2 pos, string fragmentId, string text, EmotionId grantsSkill, bool grantsAwareness)
        {
            var go = Spawn("StoryFragment", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            var frag = go.GetComponent<HiddenWeight.World.StoryFragment>();
            SetField(frag, "fragmentId", p => p.stringValue = fragmentId);
            SetField(frag, "text", p => p.stringValue = text);
            SetField(frag, "grantsSkill", p => p.intValue = (int)grantsSkill);
            SetField(frag, "grantsAwareness", p => p.boolValue = grantsAwareness);

            // 기억 파편은 이 게임에서 서사적으로 가장 중요한 수집물인데, 프리팹의 공용
            // 플레이스홀더(작은 흰 점)를 그대로 쓰고 있었다 — 여섯 방에서 배경에 묻혔다.
            // 재화와 같은 방식으로 지역 수집물 시트를 쓰고, 재화(Shard)와는 다른 행을 골라
            // 둘이 구분되게 한다.
            var fragSprite = go.GetComponentInChildren<SpriteRenderer>();
            if (fragSprite != null)
            {
                var art = ZoneArt("ItemToken_00") ?? ZoneArt("ItemShard_00");
                if (art != null)
                {
                    fragSprite.sprite = art;
                    fragSprite.color = Color.white;
                    AttachAnimator(go, fragSprite, new[] { (_artPrefix + "ItemToken", 7f, true) }, 0f);
                    FitRenderer(fragSprite, 1.1f, 1.1f);
                }
            }
            return go;
        }

        static GameObject BuildHiddenFragment(Transform parent, Vector2 pos, string fragmentId, string text)
        {
            var go = Spawn("HiddenFragment", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            var frag = go.GetComponent<HiddenFragment>();
            SetField(frag, "fragmentId", p => p.stringValue = fragmentId);
            SetField(frag, "text", p => p.stringValue = text);
            return go;
        }

        static GameObject BuildGate(Transform parent, Vector2 pos, EmotionId requiredSkill, bool requiresFinalCondition)
        {
            var go = Spawn("Gate", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            var gate = go.GetComponent<HiddenWeight.World.Gate>();
            SetField(gate, "requiredSkill", p => p.intValue = (int)requiredSkill);
            SetField(gate, "requiresFinalCondition", p => p.boolValue = requiresFinalCondition);
            return go;
        }

        static GameObject BuildEnemy(Transform parent, Vector2 pos, EnemyData overrideData)
        {
            var go = Spawn("Enemy", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            if (overrideData != null)
            {
                var enemy = go.GetComponent<Enemy>();
                SetField(enemy, "data", p => p.objectReferenceValue = overrideData);
            }
            return go;
        }

        // rotateSpeed가 0이 아니면 회전형(GazeRotator) — 기획서 EMOTION_SYSTEM 2.3절의
        // 고정형/회전형 두 종류 구분.
        static GameObject BuildGazeHazard(Transform parent, Vector2 pos, float rotationZ = 0f, float rotateSpeed = 0f)
        {
            var go = Spawn("GazeHazard", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            if (rotateSpeed != 0f)
            {
                var rot = go.AddComponent<GazeRotator>();
                SetField(rot, "degreesPerSecond", p => p.floatValue = rotateSpeed);
            }
            return go;
        }

        static GameObject BuildMovingPlatform(Transform parent, Vector2 pos, Vector2 offset, float period)
        {
            var go = Spawn("MovingPlatform", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            var mp = go.GetComponent<HiddenWeight.World.MovingPlatform>();
            SetField(mp, "offset", p => p.vector2Value = offset);
            SetField(mp, "period", p => p.floatValue = period);
            ApplyPlatformArt(go);
            return go;
        }

        static GameObject BuildCrumblingPlatform(Transform parent, Vector2 pos)
        {
            var go = Spawn("CrumblingPlatform", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            return go;
        }

        static GameObject BuildRewindableBlock(Transform parent, Vector2 pos)
        {
            var go = Spawn("RewindableBlock", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            return go;
        }

        static GameObject BuildCheckpoint(Transform parent, Vector2 pos)
        {
            var go = Spawn("Checkpoint", new Vector3(pos.x, pos.y, 0f));
            go.transform.SetParent(parent, true);
            return go;
        }

        // 소형 획득물(일반 재화). 점프 경로를 따라 놓아 이동 유도선으로 쓴다.
        static GameObject BuildCurrencyPickup(Transform parent, Vector2 pos, int amount = 1)
        {
            var go = new GameObject("CurrencyPickup");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.5f;
            go.layer = LayerMask.NameToLayer("Interactable");

            var sr = go.AddComponent<SpriteRenderer>();
            // 잔재는 전용 아틀라스, 응시·균열은 수집물 시트의 첫 프레임(GazeItemShard_00 /
            // FractureItemShard_00)을 쓴다. 둘 다 없을 때만 플레이스홀더로 떨어진다.
            sr.sprite = ResidueArt("Item_Currency") ?? ZoneArt("ItemShard_00") ?? LoadSprite("Fragment");
            if (sr.sprite.name == "Fragment") sr.color = new Color(0.95f, 0.86f, 0.6f);
            // 수집물 시트가 있으면 반짝임 순환을 튼다 — 정지 그림이면 배경 기물과 구분되지 않는다.
            AttachAnimator(go, sr, new[] { (_artPrefix + "ItemShard", 8f, true) }, 0f);
            sr.sortingOrder = 5; // Art/Residue/README.md의 Interactables 레이어 기준
            FitRenderer(sr, 1.6f, 1.6f); // localScale 0.5가 곱해져 실제 0.8유닛

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f; // localScale 0.5가 곱해져 실제 반경 0.4

            var pickup = go.AddComponent<CurrencyPickup>();
            SetField(pickup, "amount", p => p.intValue = amount);

            return go;
        }



        // ============================================================
        // 잔재 적 구성 (CONTENT_SYSTEM.md 4절). 프리팹을 새로 만들지 않고 기본 Enemy 프리팹에
        // 데이터와 행동 모듈만 갈아끼운다 — PrefabBuilder.Run()을 다시 돌리면 프리팹 내부
        // fileID가 바뀌어 기존 씬의 인스턴스 오버라이드가 끊기기 때문이다.
        // ============================================================

        public enum ResidueEnemyKind { Walker, Carrier, Finger, Hardened }

        // EnemyData 에셋을 없을 때만 만든다(GroundTile과 같은 방식).
        static EnemyData ResidueEnemyData(ResidueEnemyKind kind)
        {
            string name = $"Enemy_Residue_{kind}";
            var existing = LoadData<EnemyData>(name);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<EnemyData>();
            switch (kind)
            {
                case ResidueEnemyKind.Walker: // 잔재 보행자 — 기본 순찰
                    data.maxHealth = 2; data.moveSpeed = 1.5f; data.contactDamage = 1;
                    data.tint = new Color(0.55f, 0.5f, 0.6f);
                    data.turnHesitationSeconds = 0.35f; // 죄책감처럼 발이 무겁게, 방향 전환 전 잠깐 멈칫
                    break;
                case ResidueEnemyKind.Carrier: // 애도 운반자 — 직선 돌진
                    data.maxHealth = 3; data.moveSpeed = 2f; data.contactDamage = 1;
                    data.tint = new Color(0.7f, 0.45f, 0.4f);
                    data.telegraphSeconds = 0.8f;  // 명세 R07: 돌진 전 0.8초 예고
                    data.stunSeconds = 1.5f;       // 벽 충돌 시 1.5초 경직
                    data.chargeSpeed = 11f; data.detectRange = 9f;
                    break;
                case ResidueEnemyKind.Finger: // 매달린 손가락 — 천장 매복
                    data.maxHealth = 2; data.moveSpeed = 1.2f; data.contactDamage = 1;
                    data.tint = new Color(0.62f, 0.6f, 0.68f);
                    data.dropSpeed = 14f;
                    break;
                case ResidueEnemyKind.Hardened: // 굳은 잔재 — 방어형 정예
                    data.maxHealth = 6; data.moveSpeed = 1.2f; data.contactDamage = 1;
                    data.tint = new Color(0.42f, 0.44f, 0.5f);
                    data.telegraphSeconds = 1.1f;  // 느린 강공격
                    data.recoverSeconds = 1.2f;    // 그만큼 빈틈도 크다
                    data.guardArc = 120f; data.attackRange = 2.2f; data.detectRange = 7f;
                    break;
            }

            AssetDatabase.CreateAsset(data, $"{DataFolder}/{name}.asset");
            return data;
        }

        static GameObject BuildResidueEnemy(Transform parent, Vector2 pos, ResidueEnemyKind kind,
                                            string animationPrefix = null)
        {
            var go = BuildEnemy(parent, pos, ResidueEnemyData(kind));

            // 종류별 실루엣으로 바꾼다. CONTENT_SYSTEM.md가 "실루엣과 행동 목적이 분명한 적"을
            // 요구하므로, 네 종류가 같은 사각형으로 보이면 안 된다.
            string artName = kind == ResidueEnemyKind.Walker ? "Enemy_Walker"
                           : kind == ResidueEnemyKind.Carrier ? "Enemy_Carrier"
                           : kind == ResidueEnemyKind.Finger ? "Enemy_Finger"
                           : "Enemy_Hardened";
            var enemyArt = ResidueArt(artName);
            if (enemyArt != null)
            {
                // 루트 스케일은 건드리지 않는다. 예전에는 여기서 0.076배까지 줄여 콜라이더까지
                // 같이 작아졌다 — 겉모습만 줄이려던 것이 판정을 통째로 망가뜨렸다.
                var rootRenderer = go.GetComponent<SpriteRenderer>();
                if (rootRenderer != null) rootRenderer.enabled = false;

                var artObject = new GameObject("Art");
                artObject.transform.SetParent(go.transform, false);

                var artRenderer = artObject.AddComponent<SpriteRenderer>();
                artRenderer.sprite = enemyArt;
                artRenderer.color = Color.white;
                artRenderer.sortingOrder = 8;
                // 잔재 적 그림이 전부 어두운 앰버 톤이라 배경과 실루엣이 잘 안 갈린다 —
                // 외곽선 셰이더로 가장자리만 밝힌다(PrefabBuilder.ApplyEnemyOutline 참고).
                artRenderer.material = PrefabBuilder.EnemyOutlineMaterial();

                // 종류별 화면 크기. 애니메이터가 프레임마다 이 높이로 맞춘다.
                float displayHeight = kind == ResidueEnemyKind.Hardened ? 1.7f : 1.2f;
                var artSize = enemyArt.bounds.size;
                if (artSize.y > 0f)
                {
                    float scale = displayHeight / artSize.y;
                    artObject.transform.localScale = new Vector3(scale, scale, 1f);
                }

                string prefix = animationPrefix ?? (kind == ResidueEnemyKind.Walker ? "Walker"
                              : kind == ResidueEnemyKind.Carrier ? "Carrier"
                              : kind == ResidueEnemyKind.Finger ? "Finger"
                              : "Hardened");
                AttachAnimator(artObject, artRenderer, EnemyClips(prefix), displayHeight);
                SetField(go.GetComponent<HiddenWeight.Enemies.Enemy>(), "clipPrefix",
                    p => p.stringValue = prefix);
            }

            int groundMask = 1 << LayerMask.NameToLayer("Ground");
            int playerMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");

            switch (kind)
            {
                case ResidueEnemyKind.Walker:
                    // 순찰은 기본 프리팹의 EnemyPatrol이 그대로 맡고, 거리를 둔 플레이어에게는
                    // 석재 파편을 던진다(ResidueEnemyProjectiles_v1 1행). 붙어 있을 때는
                    // 던지지 않으므로 근접 전투의 성격은 그대로다.
                    go.AddComponent<RangedAttackBehavior>();
                    break;

                case ResidueEnemyKind.Carrier:
                {
                    var charger = go.AddComponent<ChargerBehavior>();
                    SetField(charger, "obstacleMask", p => p.intValue = groundMask | (1 << LayerMask.NameToLayer("Wall")));
                    break;
                }

                case ResidueEnemyKind.Finger:
                {
                    // 착지 예정 지점에 그리는 그림자. 이 예고가 이 적의 전부다.
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

                    // 매복 중에는 순찰하지 않는다. 착지 후 AmbusherBehavior가 다시 켠다.
                    var patrol = go.GetComponent<HiddenWeight.Enemies.EnemyPatrol>();
                    if (patrol != null) patrol.enabled = false;
                    break;
                }

                case ResidueEnemyKind.Hardened:
                {
                    var guard = go.AddComponent<GuardBehavior>();
                    SetField(guard, "playerMask", p => p.intValue = playerMask);
                    var patrol = go.GetComponent<HiddenWeight.Enemies.EnemyPatrol>();
                    if (patrol != null) patrol.enabled = false;
                    break;
                }
            }

            return go;
        }

        // 보스. 체력·패턴만 다르고 나머지는 같은 BossController를 쓴다.
        // clips/idleSprite를 주면 그 지역 보스 아트를 쓰고, 비우면 잔재의 감시자 시트를 쓴다.
        // 지역마다 보스 빌더를 따로 만들지 않기 위한 선택적 인자다.
        //
        // clipPrefix는 Enemy.PlayClip("Hit"/"Death")의 접두사다 — 시트의 피격 행이
        // "{접두사}Hit"로 잘려 있어야 맞아 들어간다(각 슬라이서 주석 참고).
        // moveClips는 moves와 나란한 배열로, 그 공격을 시작할 때 트는 클립이다.
        static GameObject BuildBoss(Transform parent, Vector2 pos, string assetName, int health,
                                    BossController.Move[] moves, float[] phases, Color tint,
                                    (string clip, float fps, bool loop)[] clips = null,
                                    string idleSprite = null,
                                    string clipPrefix = null,
                                    string[] moveClips = null,
                                    string phaseClip = null)
        {
            var data = LoadData<EnemyData>(assetName);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                data.maxHealth = health;
                data.moveSpeed = 2f;
                data.contactDamage = 1;
                data.tint = tint;
                AssetDatabase.CreateAsset(data, $"{DataFolder}/{assetName}.asset");
            }

            var go = BuildEnemy(parent, pos, data);
            go.transform.localScale = Vector3.one * 2f;

            var bossArt = idleSprite != null ? Art(idleSprite) : ResidueArt("Watcher_Idle");
            if (bossArt != null)
            {
                var rootRenderer = go.GetComponent<SpriteRenderer>();
                if (rootRenderer != null) rootRenderer.enabled = false;

                var artObject = new GameObject("Art");
                artObject.transform.SetParent(go.transform, false);

                var artRenderer = artObject.AddComponent<SpriteRenderer>();
                artRenderer.sprite = bossArt;
                artRenderer.color = Color.white;
                artRenderer.sortingOrder = 9;

                const float bossHeight = 3.2f;
                var bossSize = bossArt.bounds.size;
                if (bossSize.y > 0f)
                {
                    float scale = bossHeight / bossSize.y;
                    artObject.transform.localScale = new Vector3(scale, scale, 1f);
                }

                AttachAnimator(artObject, artRenderer, clips ?? new[]
                {
                    ("WatcherAnimIdle",  8f,  true),
                    ("WatcherAnimSweep", 12f, false),
                    ("WatcherAnimCharge",14f, false),
                    ("WatcherAnimStun",  10f, false),
                    ("WatcherAnimDrop",  14f, false),
                    ("WatcherAnimHit",   12f, false),
                    ("WatcherAnimDeath", 10f, false),
                }, bossHeight);
            }

            // 피격·사망 클립을 Enemy가 스스로 틀 수 있게 접두사를 준다. 기본은 잔재 감시자.
            SetField(go.GetComponent<HiddenWeight.Enemies.Enemy>(), "clipPrefix",
                p => p.stringValue = clipPrefix ?? "WatcherAnim");

            var patrol = go.GetComponent<HiddenWeight.Enemies.EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;

            var boss = go.AddComponent<BossController>();

            // 무브별 전투 클립. 첫 클립(대기)으로 돌아오는 것까지 BossController가 맡는다.
            var effectiveClips = clips;
            string idleClip = effectiveClips != null && effectiveClips.Length > 0
                ? effectiveClips[0].clip : "WatcherAnimIdle";
            SetField(boss, "idleClipName", p => p.stringValue = idleClip);
            if (phaseClip != null)
                SetField(boss, "phaseClipName", p => p.stringValue = phaseClip);
            if (moveClips != null)
                SetField(boss, "moveClipNames", p =>
                {
                    p.arraySize = moveClips.Length;
                    for (int i = 0; i < moveClips.Length; i++)
                        p.GetArrayElementAtIndex(i).stringValue = moveClips[i];
                });
            SetField(boss, "playerMask", p => p.intValue =
                1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed"));
            SetField(boss, "shadowSprite", p => p.objectReferenceValue = LoadSprite("Tile"));
            SetField(boss, "obstacleMask", p => p.intValue =
                1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Wall"));
            SetField(boss, "moves", p =>
            {
                p.arraySize = moves.Length;
                for (int i = 0; i < moves.Length; i++)
                    p.GetArrayElementAtIndex(i).enumValueIndex = (int)moves[i];
            });
            SetField(boss, "phaseThresholds", p =>
            {
                p.arraySize = phases.Length;
                for (int i = 0; i < phases.Length; i++)
                    p.GetArrayElementAtIndex(i).floatValue = phases[i];
            });

            return go;
        }

        // 소형 회복물.
        static GameObject BuildHealingPickup(Transform parent, Vector2 pos, int amount = 1)
        {
            var go = new GameObject("HealingPickup");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.7f;
            go.layer = LayerMask.NameToLayer("Interactable");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ResidueArt("Item_Healing") ?? ZoneArt("ItemHealing_00") ?? LoadSprite("Fragment");
            if (sr.sprite.name == "Fragment") sr.color = new Color(0.6f, 0.9f, 0.75f);
            AttachAnimator(go, sr, new[] { (_artPrefix + "ItemHealing", 8f, true) }, 0f);
            sr.sortingOrder = 5;
            FitRenderer(sr, 1.4f, 1.4f);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.7f;

            var pickup = go.AddComponent<HealingPickup>();
            SetField(pickup, "amount", p => p.intValue = amount);
            return go;
        }

        // 물리적으로 열리는 숏컷. 닫혀 있는 동안은 blocker가 길을 막고, 열리면 다리·승강기 본체가
        // 나타난다. 첫 방문에는 "만져볼 수 있지만 통과할 수 없는" 상태로 보여야 한다(R03 명세).
        static Shortcut BuildShortcut(Transform parent, string id, Vector2 center, Vector2 size, Color tint)
        {
            var go = new GameObject($"Shortcut_{id}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            // Color를 스칼라로 곱하면 알파까지 반으로 줄어든다 — "닫힘"을 어둡게만 표시하려던
            // 의도와 달리 blocker가 반투명해진다. 지금은 ApplyArtOverlay가 성공하면 이 렌더러
            // 자체를 꺼서 안 보이니 티가 안 나지만, 그 아트가 하나라도 없으면(에셋 이름이
            // 바뀌거나 슬라이싱이 안 된 경우) 밝기 대신 반투명한 "바닥"이 그대로 드러난다.
            var dimTint = new Color(tint.r * 0.5f, tint.g * 0.5f, tint.b * 0.5f, tint.a);
            var blocker = BuildSolidBlock(go.transform, "Blocker", center, size, "Ground", dimTint);
            var opened = BuildSolidBlock(go.transform, "Opened", center, size, "Ground", tint);
            opened.SetActive(false);

            // 끊어진 상태와 복원된 상태를 각각 다른 그림으로 보여준다. 승강기(숏컷 B)는 별도 세트다.
            bool isLift = id.EndsWith("_b");
            ApplyArtOverlay(blocker, isLift ? _shortcutLiftClosedArt : _shortcutClosedArt, size, 5);
            ApplyArtOverlay(opened, isLift ? _shortcutLiftOpenArt : _shortcutOpenArt, size, 5);

            var shortcut = go.AddComponent<Shortcut>();
            SetField(shortcut, "shortcutId", p => p.stringValue = id);
            SetField(shortcut, "blocker", p => p.objectReferenceValue = blocker);
            SetField(shortcut, "openedVisual", p => p.objectReferenceValue = opened);
            return shortcut;
        }

        // 깊은 "안전 바닥" 바로 위를 덮는 추락 복귀 트리거. 안전 바닥은 본 바닥보다 6 낮은데
        // 실측 최대 점프 높이가 2.72라 한 번 떨어지면 다시 못 올라온다 — 그대로 두면 재시도가
        // 아니라 진행 불가다. 피해 없이 마지막 체크포인트로만 돌려보낸다.
        static GameObject BuildHazardFloor(Transform parent, int xMin, int xMax, int topY)
            => BuildHazard(parent, new Vector2((xMin + xMax) * 0.5f, topY + 1.5f),
                           new Vector2(xMax - xMin, 3f), 0, null);

        // 위험 영역 공통 빌더. damage>0이면 체력을 깎고, recoveryPoint가 있으면 방 안의 그 지점으로
        // 되돌린다(없으면 마지막 체크포인트). 명세 R07·R09가 요구하는 "직전 안전 발판 복귀"가 이것이다.
        static GameObject BuildHazard(Transform parent, Vector2 center, Vector2 size, int damage, Transform recoveryPoint)
        {
            var go = new GameObject(damage > 0 ? "Hazard" : "FallRecovery");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var hazard = go.AddComponent<Hazard>();
            SetField(hazard, "damage", p => p.intValue = damage);
            if (recoveryPoint != null)
                SetField(hazard, "recoveryPoint", p => p.objectReferenceValue = recoveryPoint);

            return go;
        }

        // 충돌 없는 배경 연출용 스프라이트 (새장·무너진 탑·거울 기둥 등).
        //
        // spriteName이 지역 시트의 스프라이트(예: "FractureDoor_r1_c3")면 그것을 쓰고,
        // 아니면 플레이스홀더로 떨어진다. 두 경로의 크기 의미를 같게 유지한다 — 플레이스홀더
        // Tile은 1x1 유닛이라 localScale이 곧 월드 크기지만, 시트 셀은 8유닛쯤 되므로
        // FitSprite로 같은 월드 크기에 맞춘다.
        static SpriteRenderer BuildDecor(Transform parent, string name, Vector2 pos, Vector2 scale,
            string spriteName, Color tint, float rotationZ = 0f, int sortingOrder = -5)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            var sr = go.AddComponent<SpriteRenderer>();
            var zoneSprite = Art(spriteName);
            if (zoneSprite != null)
            {
                sr.sprite = zoneSprite;
                FitSprite(sr, scale.x, scale.y);
            }
            else
            {
                sr.sprite = LoadSprite(spriteName);
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            }
            sr.color = tint;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        // 조작 안내 텍스트. 플레이어가 다가오면 TutorialHint가 스스로 페이드 인한다.
        static GameObject BuildTutorialHint(Transform parent, Vector2 pos, string message)
        {
            var go = new GameObject("TutorialHint");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var hint = go.AddComponent<TutorialHint>();
            SetField(hint, "message", p => p.stringValue = message);
            return go;
        }

        static GameObject BuildPrologueActionHint(Transform parent, Vector2 pos,
            PrologueActionHint.RequiredAction action, string message, float delaySeconds = 0f)
        {
            var go = new GameObject($"PrologueHint_{action}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var hint = go.AddComponent<PrologueActionHint>();
            SetField(hint, "action", p => p.intValue = (int)action);
            SetField(hint, "message", p => p.stringValue = message);
            SetField(hint, "delaySeconds", p => p.floatValue = delaySeconds);
            return go;
        }

        static GameObject BuildPrologueConceptHint(Transform parent, Vector2 pos, string message,
            float showRadius = 3f)
        {
            var go = new GameObject("PrologueConceptHint");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var hint = go.AddComponent<PrologueConceptHint>();
            SetField(hint, "message", p => p.stringValue = message);
            SetField(hint, "showRadius", p => p.floatValue = showRadius);
            return go;
        }

        static SpriteRenderer BuildPrologueDecor(Transform parent, string assetName,
            Vector2 pos, Vector2 size, float alpha = 0.6f, float rotationZ = 0f)
        {
            var sprite = LoadPrologueSprite(assetName);

            var go = new GameObject(assetName);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.82f, 0.84f, 1f, alpha);
            renderer.sortingOrder = -12;
            FitSprite(renderer, size.x, size.y);
            return renderer;
        }

        static Sprite LoadPrologueSprite(string assetName)
        {
            string path = $"Assets/Art/Prologue/Environment/{assetName}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new System.InvalidOperationException(
                    $"튜토리얼 장식 스프라이트를 찾지 못했다: {path}");
            return sprite;
        }

        static GameObject BuildPrologueWall(Transform parent, string name, Vector2 center,
            Vector2 size)
        {
            var wall = BuildSolidBlock(parent, name, center, size, "Wall");
            var placeholder = wall.GetComponent<SpriteRenderer>();
            if (placeholder != null) placeholder.enabled = false;

            var art = new GameObject("GeneratedWallArt");
            art.transform.SetParent(wall.transform, false);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadPrologueSprite("Prologue_TraversalWall_v2");
            renderer.color = new Color(0.86f, 0.88f, 1f, 0.92f);
            renderer.sortingOrder = 3;
            Vector2 spriteSize = renderer.sprite.bounds.size;
            art.transform.localScale = new Vector3(
                size.x / (spriteSize.x * Mathf.Abs(wall.transform.localScale.x)),
                size.y / (spriteSize.y * Mathf.Abs(wall.transform.localScale.y)), 1f);
            return wall;
        }

        static GameObject BuildPrologueEnemy(Transform parent, Vector2 position,
            EnemyData data, string name, string spriteName, float displayHeight)
        {
            var enemy = BuildEnemy(parent, position, data);
            enemy.name = name;

            var placeholder = enemy.GetComponent<SpriteRenderer>();
            if (placeholder != null) Object.DestroyImmediate(placeholder);

            var art = new GameObject("GeneratedEnemyArt");
            art.transform.SetParent(enemy.transform, false);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadPrologueSprite(spriteName);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;
            float scale = displayHeight / renderer.sprite.bounds.size.y;
            art.transform.localScale = Vector3.one * scale;
            return enemy;
        }

        static EnemyData PrologueEnemyData()
        {
            const string path = DataFolder + "/Enemy_Prologue.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) return data;

            data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHealth = 2;
            data.moveSpeed = 1.2f;
            data.contactDamage = 1;
            data.tint = Color.white;
            data.turnHesitationSeconds = 0.35f;
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        // 자각 해금 지점: 거대 눈 오브제 + 트리거. 응시 지역 후반부에 1곳만 배치한다.
        static GameObject BuildAwarenessUnlock(Transform parent, Vector2 triggerPos, Vector2 eyePos, string text)
        {
            var eye = BuildDecor(parent, "GreatEye", eyePos, new Vector2(4f, 4f), "Eye",
                new Color(0.72f, 0.64f, 0.85f), 0f, -3);

            var go = new GameObject("AwarenessUnlock");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(triggerPos.x, triggerPos.y, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 4f);

            var moment = go.AddComponent<AwarenessUnlockMoment>();
            SetField(moment, "eyeVisual", p => p.objectReferenceValue = eye);
            SetField(moment, "fragmentText", p => p.stringValue = text);
            return go;
        }

        static GameObject BuildEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            return go;
        }

        static GameObject BuildPauseMenu(Transform parent)
        {
            var go = new GameObject("PauseMenu");
            go.transform.SetParent(parent, false);
            go.AddComponent<PauseMenu>();
            return go;
        }

        // 공통 지역 킷 1단계: GameManager(가장 먼저 - Awake 순서 보장) + Grid/Tilemap(Ground) +
        // 지역 Volume. Player/Camera는 아직 두지 않는다 — Room1 바닥 타일을 놓아 실제 표면
        // 높이를 알아낸 다음에야(PlacePlayerAndCamera) 스폰 위치를 그로부터 계산할 수 있다.
        //
        // GameManager는 Zone 루트의 자식으로 붙이지 않고 씬 루트 오브젝트로 남긴다(Bootstrap/Title과
        // 동일하게). GameManager 프리팹에는 ScreenFader/AudioManager도 함께 있고 셋 다 Awake에서
        // DontDestroyOnLoad(gameObject)를 호출하는데, 이 호출은 루트 오브젝트에서만 동작한다 — Zone
        // 루트의 자식으로 붙이면 (예전 HUD/FragmentLog와 같은 이유로) "DontDestroyOnLoad only works
        // for root GameObjects" 에러가 난다. 평소 플레이 흐름에서는 Bootstrap의 인스턴스가 이미
        // 살아있어 지역 씬의 중복 인스턴스가 Awake 즉시 자기 자신을 Destroy하고 DontDestroyOnLoad까지
        // 가지 않아 조용히 넘어가지만, Play Mode 테스트처럼 지역 씬을 단독으로 로드하면(Instance가
        // 아직 없으면) 바로 이 에러가 재현된다 — 뿌리 원인은 지역 씬마다 GameManager가 씬 루트가
        // 아니었다는 것이므로 여기서 고친다.
        static Tilemap BuildZoneRoot(string zoneAssetName, out GameObject root)
        {
            root = new GameObject($"Zone_{zoneAssetName}");

            // Awake 순서는 씬 루트 순서로 보장되지 않는다(SetAsFirstSibling으로 루트를 맨 앞에
            // 옮겨도 Player 쪽 Awake가 먼저 돌았다). GameManager 클래스 쪽의
            // [DefaultExecutionOrder]로 보장한다 — GameManager.cs 참고.
            Spawn("GameManager", Vector3.zero);

            var tilemap = BuildGroundGrid(out var gridGO);
            gridGO.transform.SetParent(root.transform, true);

            BuildZoneVolume(root.transform, zoneAssetName);

            // 모든 지역의 바닥은 x=-1에서 시작한다. 왼쪽(이전 지역 방향)으로 되돌아가면
            // 허공으로 떨어져 소프트락되므로 보이지 않는 경계벽으로 막는다.
            // (오른쪽 경계는 지역마다 폭이 달라 각 빌더가 세운다.)
            BuildBoundary(root.transform, "LeftBoundary", -2f);

            return tilemap;
        }

        // 보이지 않는 지역 경계벽. Ground 레이어라 벽잡기(Wall 레이어 전용)는 되지 않는다.
        // 잔재 지역의 보이는 벽(굴뚝·전장 벽). 충돌은 BuildSolidBlock 그대로 두고 겉모습만 덮는다.
        static GameObject BuildResidueWall(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = BuildSolidBlock(parent, name, center, size, "Wall");
            ApplyArtOverlay(go, _artPrefix + "Terrain_r3_c1", size, 3); // 3행 = 세로 벽
            return go;
        }

        static void BuildBoundary(Transform parent, string name, float x)
        {
            var go = BuildSolidBlock(parent, name, new Vector2(x, 6f), new Vector2(1f, 26f), "Ground");
            go.GetComponent<SpriteRenderer>().enabled = false;
        }

        // 공통 지역 킷 2단계: MainCamera + Player + HUD + PauseMenu + EventSystem.
        // spawn은 항상 "Room1 바닥의 실제 topY + 여유값"에서 호출부가 직접 계산해 넘긴다 —
        // 별도로 하드코딩한 스폰 좌표가 바닥 배치와 따로 놀다 어긋나는 것을 막기 위함이다
        // (Task 13 최초 버전의 버그: 플레이어 스프라이트 정렬 순서가 타일맵과 동률이라 안 보이던 문제 +
        // 스폰 좌표가 바닥 레이아웃과 독립적으로 하드코딩되어 있던 문제, 둘 다 여기서 고친다).
        // 카메라는 스폰과 정확히 같은 X/Y(Z만 -10)에 둬서 첫 프레임에 엉뚱한 위치에서 보간해오지 않게 한다.
        static void PlacePlayerAndCamera(GameObject root, Vector3 spawn, bool lockCameraToPlayer = false)
        {
            var camGO = Spawn("MainCamera", new Vector3(spawn.x, spawn.y, -10f));
            camGO.transform.SetParent(root.transform, true);
            if (lockCameraToPlayer)
                SetField(camGO.GetComponent<RoomCamera>(), "lockToPlayer", p => p.boolValue = true);

            Spawn("Player", spawn).transform.SetParent(root.transform, true);

            // HUD(FragmentLog 포함)는 의도적으로 Zone 루트 아래에 넣지 않고 씬 루트 오브젝트로
            // 남겨둔다. 이전에는 Zone 루트의 자식으로 붙였는데, FragmentLog.Awake()가 호출하는
            // DontDestroyOnLoad는 루트 오브젝트에서만 동작해서 씬 로드마다("DontDestroyOnLoad only
            // works on root GameObjects") 에러가 찍혔다(플레이 1회당 6번). HUD는 지역 씬마다 새로
            // 만들어지는 것으로 이미 충분하므로(각 지역 씬이 자기 HUD를 들고 있다) FragmentLog의
            // DontDestroyOnLoad 자체를 제거했다 — 씬 루트로 남겨두는 것은 그 호출이 더 이상 없어도
            // 계층을 불필요하게 깊게 만들지 않기 위한 선택이다.
            Spawn("HUD", Vector3.zero);

            BuildPauseMenu(root.transform);
            BuildEventSystem(root.transform);
        }

        // ============================================================
        // Step 2: Bootstrap / Title
        // ============================================================

        static void BuildBootstrap()
        {
            var scene = NewScene();

            var gm = Spawn("GameManager", Vector3.zero);
            SetField(gm.GetComponent<GameManager>(), "autoLoadTitle", p => p.boolValue = true);

            SaveScene(scene, "Bootstrap");
        }

        static void BuildTitle()
        {
            var scene = NewScene();

            Spawn("GameManager", Vector3.zero);

            var titleGO = new GameObject("TitleScreen");
            titleGO.AddComponent<TitleScreen>();

            BuildEventSystem(null);

            SaveScene(scene, "Title");
        }

        // ============================================================
        // Step 3: Zone_Prologue — 몽환의 우주. 튜토리얼 T01~T04.
        // ============================================================

        static void BuildZonePrologue()
        {
            var scene = NewScene();
            var tilemap = BuildZoneRoot("Prologue", out var root);
            // 런타임 배경 컴포넌트가 Awake하기 전 에디터 첫 프레임에서도 회색 플레이스홀더가
            // 번쩍이지 않게 씬 자체에 튜토리얼 충돌 타일 투명도를 저장한다.
            tilemap.color = new Color(0.1f, 0.09f, 0.2f, 0.05f);
            var rooms = new GameObject("Rooms"); rooms.transform.SetParent(root.transform, true);

            var prologueEnemy = PrologueEnemyData();

            // T01 [0,24] — 이동과 점프. 구덩이·적·피해 없이 입력에만 집중한다.
            const int t01XMin = -1, t01XMax = 24, t01FloorTop = 0;
            Floor(tilemap, t01XMin, t01XMax, t01FloorTop);
            PlaceTiles(tilemap, GroundTile(), 9, 11, 0, 1);
            PlaceTiles(tilemap, GroundTile(), 16, 18, 0, 1);
            PlaceTiles(tilemap, GroundTile(), 18, 20, 0, 2);
            PlacePlayerAndCamera(root, new Vector3(3f, 1f, 0f));
            BuildCheckpoint(root.transform, new Vector2(3f, 1f));
            BuildPrologueActionHint(root.transform, new Vector2(5f, 3.2f),
                PrologueActionHint.RequiredAction.Move, "{Move}  ·  이동", 0.35f);
            BuildPrologueActionHint(root.transform, new Vector2(9.5f, 3.2f),
                PrologueActionHint.RequiredAction.Jump, "{Jump}  ·  점프");
            BuildPrologueConceptHint(root.transform, new Vector2(15f, 4.5f),
                "이곳은 꿈속입니다.\n기억과 감정은 공간의 형태로 나타납니다.");
            var t01Room = BuildRoom(rooms.transform, "T01", new Vector2(12f, 7f),
                new Vector2(24f, 14f)).GetComponent<Room>();
            SingleRoomBackgroundBuilder.Build(t01Room, "Assets/Art/Prologue");
            BuildPrologueDecor(t01Room.transform.Find("Art"), "Prologue_OrbitRing",
                new Vector2(19f, 8.5f), new Vector2(6f, 5.3f), 0.48f, -8f);

            // T02 [24,52] — 실패가 하부 안전길이 되는 첫 점프와 폭 4 벽점프 굴뚝.
            Floor(tilemap, 24, 30, 0);
            Floor(tilemap, 30, 35, -2);
            Floor(tilemap, 35, 37, -1);
            Floor(tilemap, 37, 42, 0);
            Floor(tilemap, 42, 46, 0);
            BuildPrologueWall(root.transform, "T02_Wall_Left", new Vector2(42f, 5.5f),
                new Vector2(1f, 7f));
            BuildPrologueWall(root.transform, "T02_Wall_Right", new Vector2(46f, 5.5f),
                new Vector2(1f, 7f));
            Floor(tilemap, 46, 52, 9);
            BuildPrologueActionHint(root.transform, new Vector2(40f, 3f),
                PrologueActionHint.RequiredAction.WallJump,
                "{Jump}  ·  벽점프\n벽에 붙은 상태에서 누르기");
            BuildPrologueConceptHint(root.transform, new Vector2(33f, 2.5f),
                "꿈은 세 구역으로 나뉩니다.\n잔재 · 응시 · 균열");
            var t02Room = BuildRoom(rooms.transform, "T02", new Vector2(38f, 7f),
                new Vector2(28f, 18f)).GetComponent<Room>();
            SingleRoomBackgroundBuilder.Build(t02Room, "Assets/Art/Prologue");
            BuildPrologueDecor(t02Room.transform.Find("Art"), "Prologue_ConstellationHand",
                new Vector2(28f, 10.5f), new Vector2(3.2f, 4.2f), 0.42f, 8f);

            // T03 [52,82] — 점프+대시로 틈을 넘고, 넓은 바닥에서 첫 공격을 성공한다.
            Floor(tilemap, 52, 60, 9);
            Floor(tilemap, 60, 64, 5);
            Floor(tilemap, 64, 66, 6);
            Floor(tilemap, 66, 68, 7);
            Floor(tilemap, 68, 70, 8);
            Floor(tilemap, 70, 82, 9);
            BuildPrologueActionHint(root.transform, new Vector2(57.5f, 12f),
                PrologueActionHint.RequiredAction.Dash,
                "{Dash}  ·  대시\n점프 중에도 사용 가능");
            BuildPrologueEnemy(root.transform, new Vector2(75f, 10f), prologueEnemy,
                "NamelessEcho_T03", "Prologue_FragmentShard", 1.55f);
            BuildPrologueActionHint(root.transform, new Vector2(73f, 12f),
                PrologueActionHint.RequiredAction.Attack, "{Attack}  ·  공격");
            BuildPrologueConceptHint(root.transform, new Vector2(80f, 12f),
                "각 구역에서 기억 파편을 찾으면\n다음 구역으로 이동할 수 있습니다.", 2.5f);
            var t03Room = BuildRoom(rooms.transform, "T03", new Vector2(67f, 13f),
                new Vector2(30f, 16f)).GetComponent<Room>();
            SingleRoomBackgroundBuilder.Build(t03Room, "Assets/Art/Prologue");
            BuildPrologueDecor(t03Room.transform.Find("Art"), "Prologue_NebulaMist",
                new Vector2(67f, 16f), new Vector2(5.5f, 3.2f), 0.38f);

            // T04 [82,114] — 공격, 벽점프, 점프+대시를 짧게 결합해 잔재의 경계를 넘는다.
            Floor(tilemap, 82, 90, 9);
            BuildPrologueEnemy(root.transform, new Vector2(85f, 10f), prologueEnemy,
                "NamelessEcho_T04", "Prologue_ConstellationHand", 1.7f);
            // 벽 하단을 바닥보다 2.2유닛 띄워 플레이어가 굴뚝 안으로 걸어 들어갈 수 있게 한다.
            BuildPrologueWall(root.transform, "T04_Wall_Left", new Vector2(90f, 14.6f),
                new Vector2(1f, 6.8f));
            BuildPrologueWall(root.transform, "T04_Wall_Right", new Vector2(94f, 14.6f),
                new Vector2(1f, 6.8f));
            Floor(tilemap, 94, 101, 18);
            Floor(tilemap, 101, 106, 14);
            // 마지막 대시 실패는 사망이나 소프트락이 아니라 짧은 계단 우회로 연결한다.
            PlaceTiles(tilemap, GroundTile(), 103, 104, 14, 15);
            PlaceTiles(tilemap, GroundTile(), 104, 105, 14, 16);
            PlaceTiles(tilemap, GroundTile(), 105, 106, 14, 17);
            Floor(tilemap, 106, 114, 18);
            BuildCheckpoint(root.transform, new Vector2(110f, 19f));
            BuildPrologueConceptHint(root.transform, new Vector2(109f, 22f),
                "첫 구역: 잔재\n지나간 기억이 남아 있는 곳입니다.", 2.5f);
            var t04Room = BuildRoom(rooms.transform, "T04", new Vector2(98f, 14f),
                new Vector2(32f, 18f)).GetComponent<Room>();
            SingleRoomBackgroundBuilder.Build(t04Room, "Assets/Art/Prologue");
            BuildPrologueDecor(t04Room.transform.Find("Art"), "Prologue_FragmentShard",
                new Vector2(85.5f, 20.5f), new Vector2(3.2f, 4.6f), 0.46f, -10f);

            BuildZoneTrigger(root.transform, new Vector2(113f, 19f), new Vector2(2f, 3f), false);
            BuildBoundary(root.transform, "RightBoundary", 115f);

            SaveScene(scene, "Zone_Prologue");
        }

        // ============================================================
        // Step 5: Zone_Gaze — 응시(현재·수치심). 4룸.
        // ============================================================

        static void BuildZoneGaze()
        {
            var scene = NewScene();
            var tilemap = BuildZoneRoot("Gaze", out var root);
            var rooms = new GameObject("Rooms"); rooms.transform.SetParent(root.transform, true);

            // Room1 [0,24]: 입구 + Checkpoint. GazeHazard 1개를 멀리 배치해 위험을 미리 보여준다.
            const int room1XMin = -1, room1XMax = 25, room1FloorTop = 0;
            Floor(tilemap, room1XMin, room1XMax, room1FloorTop);
            PlacePlayerAndCamera(root, new Vector3(room1XMin + 3f, room1FloorTop + 1f, 0f));
            BuildCheckpoint(root.transform, new Vector2(4, 1));
            BuildGazeHazard(root.transform, new Vector2(20, 1.5f), 180f);
            BuildRoom(rooms.transform, "Room1", new Vector2(12, 4), new Vector2(26, 14));

            // Room2 [24,48]: StoryFragment(grantsSkill=Hush). 자각은 여기서 주지 않는다 —
            // 기획서 EMOTION_SYSTEM 2.4절대로 지역 후반부의 거대 눈 앞(Room3 끝)에서 해금한다.
            Floor(tilemap, 24, 48, 0);
            BuildStoryFragment(root.transform, new Vector2(36, 1), "gaze_skill",
                "숨을 죽이면, 나를 보던 눈들도 조용해진다.", EmotionId.Hush, false);
            BuildTutorialHint(root.transform, new Vector2(40, 3), "K 홀드  —  숨죽이기");
            BuildRoom(rooms.transform, "Room2", new Vector2(36, 4), new Vector2(24, 14));

            // Room3 [48,72]: 감시자의 회랑(WORLD_MAP 3.2절 E) — 회전형 눈 3개가 위상차를 두고
            // 통로를 훑는다 + 높이1.2 좁은 틈. 숨죽이기로만 통과. 끝에서 자각 해금.
            Floor(tilemap, 48, 72, 0);
            BuildSolidBlock(root.transform, "LowCeiling", new Vector2(60, 3.2f), new Vector2(12, 4), "Ground");
            BuildGazeHazard(root.transform, new Vector2(55, 0.6f), 0f, 60f);
            BuildGazeHazard(root.transform, new Vector2(60, 0.6f), 120f, 60f);
            BuildGazeHazard(root.transform, new Vector2(65, 0.6f), 240f, 60f);
            BuildAwarenessUnlock(root.transform, new Vector2(70, 2), new Vector2(70, 6),
                "숨는 대신, 처음으로 정면을 마주 보았다.");
            BuildRoom(rooms.transform, "Room3", new Vector2(60, 4), new Vector2(24, 14));

            // Room4 [72,96]: 우리(새장) 연출(WORLD_MAP 3.2절 F) + Enemy(Gaze) 2 +
            // HiddenFragment(자각으로만 보임) 2 = 해금 직후 자각 튜토리얼 퍼즐 + 출구.
            Floor(tilemap, 72, 96, 0);
            BuildDecor(root.transform, "Cage_1", new Vector2(76, 6.5f), new Vector2(1.6f, 2.2f),
                "Gate", new Color(0.28f, 0.24f, 0.38f));
            BuildDecor(root.transform, "Cage_1_Figure", new Vector2(76, 6.2f), new Vector2(0.5f, 1f),
                "Player", new Color(0.1f, 0.09f, 0.14f), 0f, -6);
            BuildDecor(root.transform, "Cage_2", new Vector2(83, 7.5f), new Vector2(1.6f, 2.2f),
                "Gate", new Color(0.24f, 0.2f, 0.34f));
            BuildDecor(root.transform, "Cage_2_Figure", new Vector2(83, 7.2f), new Vector2(0.5f, 1f),
                "Player", new Color(0.1f, 0.09f, 0.14f), 0f, -6);
            BuildDecor(root.transform, "Cage_3", new Vector2(90, 6f), new Vector2(1.6f, 2.2f),
                "Gate", new Color(0.26f, 0.22f, 0.36f));
            var gazeEnemyData = LoadData<EnemyData>("Enemy_Gaze");
            BuildEnemy(root.transform, new Vector2(78, 1), gazeEnemyData);
            BuildEnemy(root.transform, new Vector2(86, 1), gazeEnemyData);
            BuildTutorialHint(root.transform, new Vector2(79, 3), "L 홀드  —  자각");
            BuildHiddenFragment(root.transform, new Vector2(82, 1), "gaze_hidden_01",
                "부끄러움은 언제나 나보다 먼저 도착해 있었다.");
            BuildHiddenFragment(root.transform, new Vector2(90, 1), "gaze_hidden_02",
                "아무도 보지 않아도, 나는 나를 보고 있었다.");
            BuildZoneTrigger(root.transform, new Vector2(94, 1), new Vector2(2, 3), false);
            BuildRoom(rooms.transform, "Room4", new Vector2(84, 4), new Vector2(24, 14));

            BuildBoundary(root.transform, "RightBoundary", 97f);

            SaveScene(scene, "Zone_Gaze");
        }

        // ============================================================
        // Step 6: Zone_Fracture — 균열(미래·불안). 4룸. awarenessStable=false는 ZoneData에서
        // 이미 처리됨(DataAssetBuilder) — 씬은 손대지 않는다.
        // ============================================================

        static void BuildZoneFracture()
        {
            var scene = NewScene();
            var tilemap = BuildZoneRoot("Fracture", out var root);
            var rooms = new GameObject("Rooms"); rooms.transform.SetParent(root.transform, true);

            // Room1 [0,24]: 입구 + Checkpoint + StoryFragment(grantsSkill=Foresight).
            const int room1XMin = -1, room1XMax = 25, room1FloorTop = 0;
            Floor(tilemap, room1XMin, room1XMax, room1FloorTop);
            PlacePlayerAndCamera(root, new Vector3(room1XMin + 3f, room1FloorTop + 1f, 0f));
            BuildCheckpoint(root.transform, new Vector2(4, 1));
            BuildStoryFragment(root.transform, new Vector2(12, 1), "fracture_skill",
                "아직 오지 않은 것들이, 이미 나를 흔든다.", EmotionId.Foresight, false);
            // 자각이 이 지역에서 무력하다는 것은 설명하지 않는다 — 직접 겪게 한다 (WORLD_MAP 4.1절).
            BuildTutorialHint(root.transform, new Vector2(16, 3), "K 탭  —  예지");
            BuildRoom(rooms.transform, "Room1", new Vector2(12, 4), new Vector2(26, 14));

            // Room2 [24,48]: MovingPlatform 3개, 서로 다른 주기로 왕복. 예지로 도착 위치를 본다.
            Floor(tilemap, 24, 32, 0);
            Floor(tilemap, 44, 48, 0);
            PlaceTiles(tilemap, GroundTile(), 32, 44, -8, -6); // 추락 시 안전 바닥
            BuildHazardFloor(root.transform, 32, 44, -6);
            BuildMovingPlatform(root.transform, new Vector2(34, 0), new Vector2(3, 0), 3f);
            BuildMovingPlatform(root.transform, new Vector2(38, 0), new Vector2(3, 0), 5f);
            BuildMovingPlatform(root.transform, new Vector2(42, 0), new Vector2(3, 0), 7f);
            BuildRoom(rooms.transform, "Room2", new Vector2(36, 4), new Vector2(24, 14));

            // Room3 [48,84]: 거울 방(WORLD_MAP 4.2절 I) — 좌우가 완벽히 대칭인 기둥들 사이에
            // CrumblingPlatform 6개 중 3개만 안전. 겉보기(대칭·동일 스프라이트)로는 구분 불가,
            // 예지로만 구분된다 = 자각 무력화를 가장 극적으로 보여주는 구간.
            Floor(tilemap, 48, 60, 0);
            Floor(tilemap, 78, 84, 0);
            PlaceTiles(tilemap, GroundTile(), 60, 78, -8, -6); // 추락 시 안전 바닥
            BuildHazardFloor(root.transform, 60, 78, -6);
            var mirrorTint = new Color(0.85f, 0.92f, 0.88f);
            BuildDecor(root.transform, "MirrorPillar_L1", new Vector2(60, 3.5f), new Vector2(0.8f, 7f), "Tile", mirrorTint);
            BuildDecor(root.transform, "MirrorPillar_R1", new Vector2(78, 3.5f), new Vector2(0.8f, 7f), "Tile", mirrorTint);
            BuildDecor(root.transform, "MirrorPillar_L2", new Vector2(64.5f, 3f), new Vector2(0.6f, 6f), "Tile", mirrorTint);
            BuildDecor(root.transform, "MirrorPillar_R2", new Vector2(73.5f, 3f), new Vector2(0.6f, 6f), "Tile", mirrorTint);
            BuildDecor(root.transform, "MirrorArch", new Vector2(69, 6.5f), new Vector2(10f, 0.5f), "Tile", mirrorTint);
            BuildCrumblingPlatform(root.transform, new Vector2(61.5f, 0f));   // 위험
            BuildSafePlatform(root.transform, new Vector2(64.5f, 0f));       // 안전
            BuildCrumblingPlatform(root.transform, new Vector2(67.5f, 0f));   // 위험
            BuildSafePlatform(root.transform, new Vector2(70.5f, 0f));       // 안전
            BuildCrumblingPlatform(root.transform, new Vector2(73.5f, 0f));   // 위험
            BuildSafePlatform(root.transform, new Vector2(76.5f, 0f));       // 안전
            BuildRoom(rooms.transform, "Room3", new Vector2(66, 4), new Vector2(36, 14));

            // Room4 [84,108]: Enemy(Fracture) 2 + StoryFragment 2 + 출구(marksFractureCleared=true).
            // 이 지역의 파편은 숨기지 않는다 — 자각이 완전히 무력화되어(EMOTION_SYSTEM 3.3절)
            // 숨김 파편은 영원히 찾을 수 없기 때문이다. 눈에 보이는 파편으로 클라이맥스에 배치한다.
            Floor(tilemap, 84, 108, 0);
            var fractureEnemyData = LoadData<EnemyData>("Enemy_Fracture");
            BuildEnemy(root.transform, new Vector2(90, 1), fractureEnemyData);
            BuildEnemy(root.transform, new Vector2(98, 1), fractureEnemyData);
            BuildStoryFragment(root.transform, new Vector2(94, 1), "fracture_hidden_01",
                "무너질 걸 알면서도, 발을 뗄 수밖에 없었다.", EmotionId.None, false);
            BuildStoryFragment(root.transform, new Vector2(102, 1), "fracture_hidden_02",
                "불안은 미래가 아니라, 지금의 다른 이름이었다.", EmotionId.None, false);
            BuildZoneTrigger(root.transform, new Vector2(106, 1), new Vector2(2, 3), true);
            BuildRoom(rooms.transform, "Room4", new Vector2(96, 4), new Vector2(24, 14));

            BuildBoundary(root.transform, "RightBoundary", 109f);

            SaveScene(scene, "Zone_Fracture");
        }

        // ============================================================
        // Step 7: Ending — 횡스크롤 없음. 정적 침실 레이어 + 이상 오브젝트 3개 + 몽타주.
        // ============================================================

        static void BuildEnding()
        {
            var scene = NewScene();
            var root = new GameObject("Ending");

            // GameManager는 여기서도 Ending 루트의 자식으로 붙이지 않는다 — 위 BuildZoneRoot의
            // DontDestroyOnLoad 관련 주석과 동일한 이유.
            Spawn("GameManager", Vector3.zero);

            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(root.transform, true);
            camGO.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            // 배경 레이어: Wall(z0) 뒤에 Bed(z-1, 카메라에 더 가깝게 = 앞).
            var wallGO = new GameObject("Wall");
            wallGO.transform.SetParent(root.transform, true);
            wallGO.transform.position = new Vector3(0f, 0f, 0f);
            wallGO.transform.localScale = new Vector3(3f, 2.2f, 1f);
            var wallSr = wallGO.AddComponent<SpriteRenderer>();
            wallSr.sprite = LoadSprite("Wall");
            wallSr.sortingOrder = 0;

            var bedGO = new GameObject("Bed");
            bedGO.transform.SetParent(root.transform, true);
            bedGO.transform.position = new Vector3(0f, -3f, -1f);
            bedGO.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var bedSr = bedGO.AddComponent<SpriteRenderer>();
            bedSr.sprite = LoadSprite("Bed");
            bedSr.sortingOrder = 1;

            // AnomalyObject 3개.
            var candle = BuildAnomaly(root.transform, "Anomaly_Candle", AnomalyObject.Kind.InvertedCandle,
                new Vector3(2.5f, -1.6f, -1.2f), "Candle", Color.white, new Vector2(1f, 1f));
            var shadow = BuildAnomaly(root.transform, "Anomaly_Shadow", AnomalyObject.Kind.MismatchedShadow,
                new Vector3(-2.5f, -3.6f, -1.2f), "Tile", Color.black, new Vector2(2f, 0.4f));
            var wallPatch = BuildAnomaly(root.transform, "Anomaly_TremblingWall", AnomalyObject.Kind.TremblingWall,
                new Vector3(3.5f, 1.5f, -0.2f), "Tile", new Color(0.18f, 0.16f, 0.22f), new Vector2(1.2f, 1.2f));

            // 몽타주용 전체화면 Canvas + Image.
            var canvasGO = new GameObject("MontageCanvas");
            canvasGO.transform.SetParent(root.transform, true);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGO.AddComponent<CanvasScaler>();

            var imageGO = new GameObject("MontageImage");
            imageGO.transform.SetParent(canvasGO.transform, false);
            var image = imageGO.AddComponent<Image>();
            var imgRt = image.rectTransform;
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;

            // EndingSequence.
            var sequenceGO = new GameObject("EndingSequence");
            sequenceGO.transform.SetParent(root.transform, true);
            var sequence = sequenceGO.AddComponent<EndingSequence>();

            var so = new SerializedObject(sequence);
            var anomaliesProp = so.FindProperty("anomalies");
            anomaliesProp.arraySize = 3;
            anomaliesProp.GetArrayElementAtIndex(0).objectReferenceValue = candle;
            anomaliesProp.GetArrayElementAtIndex(1).objectReferenceValue = shadow;
            anomaliesProp.GetArrayElementAtIndex(2).objectReferenceValue = wallPatch;

            so.FindProperty("montageImage").objectReferenceValue = image;

            var framesProp = so.FindProperty("montageFrames");
            framesProp.arraySize = 3;
            framesProp.GetArrayElementAtIndex(0).objectReferenceValue = LoadSprite("Tile");   // 잔재
            framesProp.GetArrayElementAtIndex(1).objectReferenceValue = LoadSprite("Eye");    // 응시
            framesProp.GetArrayElementAtIndex(2).objectReferenceValue = LoadSprite("Gate");   // 균열
            so.ApplyModifiedProperties();

            SaveScene(scene, "Ending");
        }

        static AnomalyObject BuildAnomaly(Transform parent, string name, AnomalyObject.Kind kind, Vector3 pos, string spriteName, Color tint, Vector2 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;

            var visualGO = new GameObject("Visual");
            visualGO.transform.SetParent(go.transform, false);
            visualGO.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = visualGO.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spriteName);
            sr.color = tint;
            sr.sortingOrder = 2;

            var anomaly = go.AddComponent<AnomalyObject>();
            var so = new SerializedObject(anomaly);
            so.FindProperty("type").intValue = (int)kind;
            so.FindProperty("visual").objectReferenceValue = sr;
            so.ApplyModifiedProperties();

            return anomaly;
        }

        // ============================================================
        // Step 8: EditorBuildSettings 등록
        // ============================================================

        static void RegisterBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/Bootstrap.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Title.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Prologue.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Residue.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Gaze.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Fracture.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Ending.unity", true),
                // 재설계 전체 지역(각 15룸). 아직 게임 흐름에 연결하지 않았지만 Play Mode
                // 테스트가 이름으로 로드해야 하므로 등록해 둔다.
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Residue_Full.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Gaze_Full.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Fracture_Full.unity", true),
            };

            // 방 씬은 additive로만 로드되지만 빌드 세팅에 없으면 CanStreamedLevelBeLoaded가
            // false를 돌려주고 RoomLoader가 전환을 취소한다 — 등록이 곧 통행 허가다.
            foreach (var room in ResidueRoomLinks.RoomNames)
                scenes.Add(new EditorBuildSettingsScene($"{ScenesFolder}/Room_Residue_{room}.unity", true));
            foreach (var room in GazeRoomLinks.RoomNames)
                scenes.Add(new EditorBuildSettingsScene($"{ScenesFolder}/Room_Gaze_{room}.unity", true));
            foreach (var room in FractureRoomLinks.RoomNames)
                scenes.Add(new EditorBuildSettingsScene($"{ScenesFolder}/Room_Fracture_{room}.unity", true));

            // 아직 굽지 않은 지역의 방 씬은 파일이 없다. 등록만 해 두면 Unity가 빌드 때
            // 조용히 건너뛰므로, 지역을 하나씩 옮기는 동안에도 이 목록은 그대로 둘 수 있다.
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
