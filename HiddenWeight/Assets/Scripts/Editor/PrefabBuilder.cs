using UnityEditor;
using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;
using HiddenWeight.Emotions;
using HiddenWeight.Enemies;
using HiddenWeight.World;
using HiddenWeight.UI;

namespace HiddenWeight.EditorTools
{
    // Assets/Prefabs/에 프리팹 13종을 생성한다. GameObject.AddComponent로 조립하고
    // PrefabUtility.SaveAsPrefabAsset으로 저장한 뒤 씬의 임시 오브젝트는 DestroyImmediate로 지운다.
    //
    // 순서 메모: GameManager(+balance)를 가장 먼저 만들어 씬에 살려 둔다. Player/Enemy 등 다른
    // 컴포넌트들의 Awake()가 GameManager.Instance.Balance.*를 참조하기 때문에, 이 인스턴스가
    // 없으면 프리팹을 짓는 동안(에디터에서도 Awake는 AddComponent 시점에 즉시 돈다)
    // NullReferenceException 콘솔 노이즈가 난다. GameManager는 모든 프리팹을 다 지은 뒤
    // 마지막에 저장하고 파괴한다.
    public static class PrefabBuilder
    {
        const string Folder = "Assets/Prefabs";
        const string ArtFolder = "Assets/Art/Placeholder";
        const string DataFolder = "Assets/ScriptableObjects";

        public static void Run()
        {
            EnsureFolder();

            var gameManagerRoot = BuildGameManagerRoot();

            BuildPlayer();
            BuildEnemy();
            BuildMovingPlatform();
            BuildCrumblingPlatform();
            BuildRewindableBlock();
            BuildGazeHazard();
            BuildGate();
            BuildStoryFragment();
            BuildHiddenFragment();
            BuildCheckpoint();
            BuildMainCamera();
            BuildHUD();

            SavePrefab(gameManagerRoot, "GameManager");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabBuilder] 프리팹 13개 생성 완료");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
        static T LoadData<T>(string name) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{name}.asset");

        static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        // ---------------- GameManager (가장 먼저 생성, 가장 나중에 저장) ----------------
        static GameObject BuildGameManagerRoot()
        {
            var root = new GameObject("GameManager");

            var gm = root.AddComponent<GameManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("balance").objectReferenceValue = LoadData<BalanceData>("BalanceData");
            so.ApplyModifiedProperties();

            root.AddComponent<AudioManager>();
            root.AddComponent<ScreenFader>();

            return root;
        }

        // ---------------- Player ----------------
        // 플레이어 콜라이더용 무마찰 재질. 프로젝트에 PhysicsMaterial2D가 하나도 없으면 2D 기본
        // 마찰 0.4가 걸리는데, 그러면 방향키를 벽 쪽으로 누른 채 벽면에 닿는 순간 중력이 마찰을
        // 이기지 못해 플레이어가 공중에서 벽에 붙어 그대로 멈춘다(계단·둔덕 모서리에서 잘 난다).
        // 벽잡기·벽점프는 마찰이 아니라 wallCheck 판정으로 하므로 이 재질에 영향받지 않는다.
        // 검증: Assets/Tests/PlayMode/ResidueR01Tests.cs
        public static PhysicsMaterial2D FrictionlessPlayerMaterial()
        {
            const string path = "Assets/Settings/Player_Frictionless.physicsMaterial2D";

            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (existing != null)
            {
                existing.friction = 0f;
                existing.bounciness = 0f;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var material = new PhysicsMaterial2D("Player_Frictionless") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // 프리팹 13종을 전부 다시 짓지 않고 Player 프리팹에만 무마찰 재질을 붙인다.
        // (PrefabBuilder.Run()을 다시 돌리면 프리팹 내부 fileID가 바뀌어 씬의 인스턴스 오버라이드가
        //  끊기므로, 이 한 가지 수정 때문에 전체를 재생성하지는 않는다.)
        [MenuItem("Hidden Weight/Fix/Apply Player Physics Material")]
        public static void ApplyPlayerPhysicsMaterial()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/Player.prefab");
            var col = prefab.GetComponent<CapsuleCollider2D>();
            col.sharedMaterial = FrictionlessPlayerMaterial();

            // 입력 펌프. 없으면 점프·대시가 프레임 타이밍에 따라 씹힌다.
            if (prefab.GetComponent<PlayerInputPump>() == null) prefab.AddComponent<PlayerInputPump>();

            // 공격 시각 피드백. 없으면 J를 눌러도 화면에 아무 변화가 없다.
            if (prefab.GetComponent<AttackVisual>() == null)
            {
                var visual = prefab.AddComponent<AttackVisual>();
                var so = new SerializedObject(visual);
                so.FindProperty("sprite").objectReferenceValue = LoadSprite("Tile");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[PrefabBuilder] Player 콜라이더에 무마찰 재질 적용 완료");
        }

        static void BuildPlayer()
        {
            var root = new GameObject("Player");
            root.layer = LayerMask.NameToLayer("Player");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Player");
            sr.sortingOrder = 10; // 타일맵(기본 0)과의 정렬 동률을 피한다 — 플레이어가 항상 위에 그려진다.

            var rb = root.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = root.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.8f, 1.4f); // Player.png 32x48px / 32ppu = 1x1.5 유닛 기준
            col.sharedMaterial = FrictionlessPlayerMaterial();

            var groundCheck = NewChild(root.transform, "GroundCheck");
            groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            var wallCheck = NewChild(root.transform, "WallCheck");
            wallCheck.transform.localPosition = new Vector3(0.5f, 0f, 0f);

            var controller = root.AddComponent<PlayerController>();
            var attack = root.AddComponent<PlayerAttack>();
            root.AddComponent<PlayerHealth>();
            root.AddComponent<VoidRespawn>(); // 맵 밖 무한 낙하 소프트락 방지
            root.AddComponent<PlayerAnimator>();
            root.AddComponent<PlayerInputPump>();
            var attackVisual = root.AddComponent<AttackVisual>();
            new SerializedObject(attackVisual).FindProperty("sprite").objectReferenceValue = LoadSprite("Tile");
            root.AddComponent<EmotionSkillController>();
            var rewind = root.AddComponent<RewindSkill>();
            root.AddComponent<HushSkill>();
            root.AddComponent<ForesightSkill>();
            root.AddComponent<AwarenessSystem>();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
            controllerSo.FindProperty("wallCheck").objectReferenceValue = wallCheck.transform;
            controllerSo.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
            controllerSo.FindProperty("wallLayer").intValue = LayerMask.GetMask("Wall");
            controllerSo.ApplyModifiedProperties();

            var attackSo = new SerializedObject(attack);
            attackSo.FindProperty("enemyLayer").intValue = LayerMask.GetMask("Enemy");
            attackSo.ApplyModifiedProperties();

            var rewindSo = new SerializedObject(rewind);
            // 되감기 대상: Interactable(되돌릴 오브젝트 대부분) + Ground(RewindableBlock이 Ground 레이어)
            rewindSo.FindProperty("interactableMask").intValue = LayerMask.GetMask("Interactable", "Ground");
            rewindSo.ApplyModifiedProperties();

            SavePrefab(root, "Player");
        }

        // ---------------- Enemy ----------------
        static void BuildEnemy()
        {
            var root = new GameObject("Enemy");
            root.layer = LayerMask.NameToLayer("Enemy");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Enemy");
            sr.sortingOrder = 8; // 타일맵(0)보다 위, 플레이어(10)보다 아래.

            var rb = root.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            var enemy = root.AddComponent<Enemy>();
            var enemySo = new SerializedObject(enemy);
            // 기본값으로 잔재(Residue) EnemyData를 연결해 둔다. 다른 지역(응시/균열)에 배치할 때는
            // Task 13이 씬에서 이 프리팹의 인스턴스 오버라이드로 Enemy_Gaze/Enemy_Fracture를 끼운다.
            enemySo.FindProperty("data").objectReferenceValue = LoadData<EnemyData>("Enemy_Residue");
            enemySo.ApplyModifiedProperties();

            var edgeCheck = NewChild(root.transform, "EdgeCheck");
            edgeCheck.transform.localPosition = new Vector3(0.6f, -0.5f, 0f);

            var patrol = root.AddComponent<EnemyPatrol>();
            var patrolSo = new SerializedObject(patrol);
            patrolSo.FindProperty("edgeCheck").objectReferenceValue = edgeCheck.transform;
            patrolSo.FindProperty("groundMask").intValue = LayerMask.GetMask("Ground");
            patrolSo.ApplyModifiedProperties();

            root.AddComponent<ContactDamage>();

            SavePrefab(root, "Enemy");
        }

        // ---------------- MovingPlatform ----------------
        static void BuildMovingPlatform()
        {
            var root = new GameObject("MovingPlatform");
            root.layer = LayerMask.NameToLayer("Ground");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Platform");
            sr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f); // Platform.png 96x16px / 32ppu

            root.AddComponent<MovingPlatform>();

            SavePrefab(root, "MovingPlatform");
        }

        // ---------------- CrumblingPlatform ----------------
        static void BuildCrumblingPlatform()
        {
            var root = new GameObject("CrumblingPlatform");
            root.layer = LayerMask.NameToLayer("Ground");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Platform");
            sr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            root.AddComponent<CrumblingPlatform>();
            root.AddComponent<RewindHighlight>(); // 무너진 뒤 되감기 가능해지면 골드 아웃라인

            SavePrefab(root, "CrumblingPlatform");
        }

        // ---------------- RewindableBlock ----------------
        static void BuildRewindableBlock()
        {
            var root = new GameObject("RewindableBlock");
            root.layer = LayerMask.NameToLayer("Ground");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Tile");
            sr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.

            root.AddComponent<Rigidbody2D>(); // 되감기로 밀린 위치를 되돌려야 하므로 Dynamic 그대로 둔다

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f); // Tile.png 32x32px / 32ppu

            root.AddComponent<Rewindable>();
            root.AddComponent<RewindHighlight>(); // 제자리를 벗어나면(되감기 가능) 골드 아웃라인

            SavePrefab(root, "RewindableBlock");
        }

        // ---------------- GazeHazard ----------------
        static void BuildGazeHazard()
        {
            var root = new GameObject("GazeHazard");
            root.layer = LayerMask.NameToLayer("Hazard");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Eye");
            sr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.

            var hazard = root.AddComponent<GazeHazard>();
            var so = new SerializedObject(hazard);
            // PlayerHushed는 절대 넣지 않는다 — 숨죽이기 중에는 레이어가 바뀌어 시선에서 벗어난다.
            so.FindProperty("playerMask").intValue = LayerMask.GetMask("Player");
            so.FindProperty("groundMask").intValue = LayerMask.GetMask("Ground");
            so.ApplyModifiedProperties();

            SavePrefab(root, "GazeHazard");
        }

        // ---------------- Gate ----------------
        static void BuildGate()
        {
            var root = new GameObject("Gate");

            var blocker = NewChild(root.transform, "Blocker");
            blocker.layer = LayerMask.NameToLayer("Ground");
            var blockerSr = blocker.AddComponent<SpriteRenderer>();
            blockerSr.sprite = LoadSprite("Gate");
            blockerSr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.
            var blockerCol = blocker.AddComponent<BoxCollider2D>();
            blockerCol.size = new Vector2(1f, 3f); // Gate.png 32x96px / 32ppu

            var hint = NewChild(root.transform, "Hint");
            hint.transform.localPosition = new Vector3(0f, 2f, 0f);
            var hintSr = hint.AddComponent<SpriteRenderer>();
            hintSr.sortingOrder = 5;

            var gate = root.AddComponent<Gate>();
            var so = new SerializedObject(gate);
            so.FindProperty("blocker").objectReferenceValue = blocker;
            so.FindProperty("hintIcon").objectReferenceValue = hintSr;
            so.ApplyModifiedProperties();

            SavePrefab(root, "Gate");
        }

        // ---------------- StoryFragment ----------------
        static void BuildStoryFragment()
        {
            var root = new GameObject("StoryFragment");
            root.layer = LayerMask.NameToLayer("Interactable");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Fragment");
            sr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f; // Fragment.png 16x16px / 32ppu

            root.AddComponent<StoryFragment>();

            SavePrefab(root, "StoryFragment");
        }

        // ---------------- HiddenFragment ----------------
        static void BuildHiddenFragment()
        {
            var root = new GameObject("HiddenFragment");
            root.layer = LayerMask.NameToLayer("Interactable");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Fragment");
            sr.sortingOrder = 5; // 타일맵(0)과의 정렬 동률을 피한다.
            sr.enabled = false; // 자각(L 홀드)으로 드러나기 전에는 보이지 않는다

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            var hidden = root.AddComponent<HiddenFragment>();
            var so = new SerializedObject(hidden);
            so.FindProperty("visual").objectReferenceValue = sr;
            so.ApplyModifiedProperties();

            SavePrefab(root, "HiddenFragment");
        }

        // ---------------- Checkpoint ----------------
        static void BuildCheckpoint()
        {
            var root = new GameObject("Checkpoint");

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            root.AddComponent<Checkpoint>();

            SavePrefab(root, "Checkpoint");
        }

        // ---------------- MainCamera ----------------
        static void BuildMainCamera()
        {
            var root = new GameObject("MainCamera");
            root.tag = "MainCamera";

            var cam = root.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            root.AddComponent<RoomCamera>();

            SavePrefab(root, "MainCamera");
        }

        // ---------------- HUD ----------------
        static void BuildHUD()
        {
            // HUD·FragmentLog 둘 다 Awake()에서 Canvas 이하 계층을 스스로 만드는 self-building
            // 컴포넌트라(HUD.cs/FragmentLog.cs 참고), 여기서는 컴포넌트만 얹으면 된다.
            var root = new GameObject("HUD");
            root.AddComponent<HUD>();
            root.AddComponent<FragmentLog>();

            SavePrefab(root, "HUD");
        }
    }
}
