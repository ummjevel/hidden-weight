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
    public static class ZoneSceneBuilder
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
            BuildZoneResidue();
            BuildZoneGaze();
            BuildZoneFracture();
            BuildEnding();

            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ZoneSceneBuilder] 씬 7개 생성 완료");
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

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

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

        // 충돌 없는 배경 연출용 스프라이트 (새장·무너진 탑·거울 기둥 등).
        static SpriteRenderer BuildDecor(Transform parent, string name, Vector2 pos, Vector2 scale,
            string spriteName, Color tint, float rotationZ = 0f, int sortingOrder = -5)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spriteName);
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
        static void PlacePlayerAndCamera(GameObject root, Vector3 spawn)
        {
            Spawn("MainCamera", new Vector3(spawn.x, spawn.y, -10f)).transform.SetParent(root.transform, true);
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
        // Step 3: Zone_Prologue — 몽환의 우주. 튜토리얼 3룸.
        // ============================================================

        static void BuildZonePrologue()
        {
            var scene = NewScene();
            var tilemap = BuildZoneRoot("Prologue", out var root);
            var rooms = new GameObject("Rooms"); rooms.transform.SetParent(root.transform, true);

            // Room1 [0,24]: 평평한 바닥, 좌우 이동만.
            const int room1XMin = -1, room1XMax = 25, room1FloorTop = 0;
            Floor(tilemap, room1XMin, room1XMax, room1FloorTop);
            // 스폰은 항상 이 바닥의 실제 topY에서 계산한다 — 하드코딩된 좌표가 레이아웃과
            // 따로 놀다 바닥 밑/뒤에 파묻히는 일(Task 13 버그)을 막는다.
            PlacePlayerAndCamera(root, new Vector3(room1XMin + 3f, room1FloorTop + 1f, 0f));
            BuildTutorialHint(root.transform, new Vector2(10, 3), "← →  또는  A / D  이동");
            BuildRoom(rooms.transform, "Room1", new Vector2(12, 4), new Vector2(26, 14));

            // Room2 [24,48]: 3단 계단 + 폭4 구덩이(점프·대시). 착지 실패 시 낮은 통로로 떨어져
            // 안전하게 재시도할 수 있다(진짜 무저갱이 아니다).
            Floor(tilemap, 24, 30, 0);
            Floor(tilemap, 30, 32, 1);
            Floor(tilemap, 32, 34, 2);
            Floor(tilemap, 34, 36, 3);
            Floor(tilemap, 36, 40, 3);
            Floor(tilemap, 40, 44, 0); // 착지 실패 시 떨어지는 낮은 안전 통로
            Floor(tilemap, 44, 48, 3);
            BuildTutorialHint(root.transform, new Vector2(29, 3.5f), "Space  점프  ·  LeftCtrl  대시");
            BuildRoom(rooms.transform, "Room2", new Vector2(36, 4), new Vector2(24, 14));

            // Room3 [48,72]: 높이8 수직 벽 2개 사이 굴뚝을 좌우 벽점프 핑퐁으로 올라간다.
            // 굴뚝 위는 뚫려 있고 출구 트리거가 그 바로 위에 떠 있다 — 꼭대기까지 오르면 닿는다.
            // 예전 버전은 굴뚝 위가 착지 발판(타일)으로 막혀 있어서, 바깥 벽면을 같은 벽
            // 반복 홉으로 오르는 고난도 루트만 남아 사실상 진행 불가에 가까웠다. 튜토리얼
            // 난이도에 맞게 천장을 열고 안쪽 핑퐁 루트를 정식 경로로 만든다.
            Floor(tilemap, 48, 72, 0);
            // 벽 2개를 공중에 띄운다(y 2.2~8). 바닥에서 굴뚝 아래로 걸어 들어간 뒤 점프해서
            // 벽면에 붙는 것이 입구다 — 점프 정점(속도14·중력3.5 기준 약 2.85)이 벽 하단(2.2)에
            // 확실히 닿는다. 이전 버전은 벽이 바닥부터 서 있어 굴뚝에 들어갈 방법이 없었다.
            BuildSolidBlock(root.transform, "Wall_Left", new Vector2(58, 5.1f), new Vector2(1, 5.8f), "Wall");
            BuildSolidBlock(root.transform, "Wall_Right", new Vector2(61, 5.1f), new Vector2(1, 5.8f), "Wall");
            BuildTutorialHint(root.transform, new Vector2(55, 3),
                "두 벽 사이에서 점프해  벽 쪽 방향키로 붙고\nSpace  로 반대 벽에 옮겨 붙으며 오르기");
            BuildRoom(rooms.transform, "Room3", new Vector2(60, 8), new Vector2(24, 20));

            // 굴뚝 입구(x 58.5~60.5) 바로 위를 넉넉히 덮는다 — 착지 없이 닿기만 하면 클리어.
            BuildZoneTrigger(root.transform, new Vector2(59.5f, 9.5f), new Vector2(3, 3), false);

            BuildBoundary(root.transform, "RightBoundary", 73f);

            SaveScene(scene, "Zone_Prologue");
        }

        // ============================================================
        // Step 4: Zone_Residue — 잔재(과거·죄책감). 4룸.
        // ============================================================

        static void BuildZoneResidue()
        {
            var scene = NewScene();
            var tilemap = BuildZoneRoot("Residue", out var root);
            var rooms = new GameObject("Rooms"); rooms.transform.SetParent(root.transform, true);

            // Room1 [0,24]: 입구. Checkpoint. StoryFragment(grantsSkill=Rewind).
            const int room1XMin = -1, room1XMax = 25, room1FloorTop = 0;
            Floor(tilemap, room1XMin, room1XMax, room1FloorTop);
            PlacePlayerAndCamera(root, new Vector3(room1XMin + 3f, room1FloorTop + 1f, 0f));
            BuildCheckpoint(root.transform, new Vector2(4, 1));
            BuildStoryFragment(root.transform, new Vector2(12, 1), "residue_skill",
                "그때로 돌아갈 수만 있다면, 손끝이라도 붙잡았을 텐데.", EmotionId.Rewind, false);
            BuildTutorialHint(root.transform, new Vector2(16, 3), "K 홀드  —  되감기");
            BuildRoom(rooms.transform, "Room1", new Vector2(12, 4), new Vector2(26, 14));

            // Room2 [24,48]: 무너진 다리. RewindableBlock 3개가 떨어져 있다(중력으로 낙하 후
            // 되감기로 원위치 복원). 실패 시 낮은 안전 바닥으로 떨어진다.
            Floor(tilemap, 24, 35, 0);
            Floor(tilemap, 38, 48, 0);
            PlaceTiles(tilemap, GroundTile(), 35, 38, -8, -6); // 다리 아래 안전 바닥
            BuildRewindableBlock(root.transform, new Vector2(35.5f, -0.5f));
            BuildRewindableBlock(root.transform, new Vector2(36.5f, -0.5f));
            BuildRewindableBlock(root.transform, new Vector2(37.5f, -0.5f));
            BuildRoom(rooms.transform, "Room2", new Vector2(36, 4), new Vector2(24, 14));

            // Room3 [48,72]: CrumblingPlatform 4개 연속. 밟으면 무너지고 되감기로 되살린다.
            Floor(tilemap, 48, 58, 0);
            Floor(tilemap, 70, 72, 0);
            PlaceTiles(tilemap, GroundTile(), 58, 70, -8, -6); // 추락 시 안전 바닥
            BuildCrumblingPlatform(root.transform, new Vector2(59.5f, 0f));
            BuildCrumblingPlatform(root.transform, new Vector2(62.5f, 0f));
            BuildCrumblingPlatform(root.transform, new Vector2(65.5f, 0f));
            BuildCrumblingPlatform(root.transform, new Vector2(68.5f, 0f));
            BuildRoom(rooms.transform, "Room3", new Vector2(60, 4), new Vector2(24, 14));

            // Room4 [72,96]: Enemy(Residue) 2, Gate(Rewind), 출구. 곁가지: 되돌아왔을 때만
            // 열리는 Gate(requiresFinalCondition) 뒤에 HiddenFragment.
            // 클라이맥스 연출(WORLD_MAP 2.3절): 출구 뒤로 무너진 거대 탑의 실루엣을 배경에 세운다.
            Floor(tilemap, 72, 96, 0);
            BuildDecor(root.transform, "RuinTower_Left", new Vector2(88, 4.5f), new Vector2(1.6f, 9f),
                "Tile", new Color(0.16f, 0.17f, 0.24f), 6f);
            BuildDecor(root.transform, "RuinTower_Mid", new Vector2(91.5f, 5.5f), new Vector2(2f, 12f),
                "Tile", new Color(0.13f, 0.14f, 0.2f), -3f);
            BuildDecor(root.transform, "RuinTower_Right", new Vector2(94.5f, 4f), new Vector2(1.4f, 8f),
                "Tile", new Color(0.18f, 0.19f, 0.26f), 10f);
            BuildDecor(root.transform, "RuinBell", new Vector2(91.5f, 10f), new Vector2(2.4f, 2.4f),
                "Eye", new Color(0.35f, 0.28f, 0.18f), 0f, -4);
            BuildEnemy(root.transform, new Vector2(78, 1), null); // 프리팹 기본값이 이미 Enemy_Residue
            BuildEnemy(root.transform, new Vector2(86, 1), null);
            BuildGate(root.transform, new Vector2(90, 1.5f), EmotionId.Rewind, false);
            BuildZoneTrigger(root.transform, new Vector2(94, 1), new Vector2(2, 3), false);

            // 백트래킹 전용 곁가지: 점프로만 닿는 얇은 선반 위 최종 Gate + HiddenFragment.
            PlaceTiles(tilemap, GroundTile(), 76, 86, 4, 5);
            BuildGate(root.transform, new Vector2(80, 6.5f), EmotionId.None, true);
            BuildHiddenFragment(root.transform, new Vector2(83, 5.5f), "residue_hidden_final",
                "결국 남은 건, 스스로에게조차 하지 못한 용서.");
            BuildRoom(rooms.transform, "Room4", new Vector2(84, 5), new Vector2(24, 16));

            BuildBoundary(root.transform, "RightBoundary", 97f);

            SaveScene(scene, "Zone_Residue");
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
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/Bootstrap.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Title.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Prologue.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Residue.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Gaze.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Zone_Fracture.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Ending.unity", true),
            };
        }
    }
}
