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
            // 프리팹 에셋에 "자식 GameObject 추가"까지 하려면 LoadPrefabContents로 열어 편집한 뒤
            // 다시 저장해야 한다. LoadAssetAtPath로 얻은 인스턴스에 AddComponent만 하면 컴포넌트는
            // 저장되지만 새로 만든 자식은 사라진다 — 실제로 플레이어 Art 자식이 그렇게 날아갔다.
            string path = $"{Folder}/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var col = root.GetComponent<CapsuleCollider2D>();
                col.sharedMaterial = FrictionlessPlayerMaterial();

                if (root.GetComponent<PlayerInputPump>() == null) root.AddComponent<PlayerInputPump>();

                {
                    var visual = root.GetComponent<AttackVisual>();
                    if (visual == null) visual = root.AddComponent<AttackVisual>();

                    var so = new SerializedObject(visual);
                    so.FindProperty("sprite").objectReferenceValue = LoadSprite("Tile");

                    // 참격 궤적(CombatVFX_v1 2행). 있으면 사각 섬광 대신 이것이 재생된다.
                    var slash = FindFrames("SwingSlash");
                    var frames = so.FindProperty("slashFrames");
                    frames.arraySize = slash.Length;
                    for (int i = 0; i < slash.Length; i++)
                        frames.GetArrayElementAtIndex(i).objectReferenceValue = slash[i];
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (slash.Length == 0)
                        Debug.LogWarning("[PrefabBuilder] SwingSlash 프레임을 찾지 못했다 — " +
                            "ResidueArtSlicer.SliceAll()을 먼저 실행할 것.");
                }

                // 루트 스케일은 절대 건드리지 않는다. 캡슐 콜라이더와 GroundCheck가 함께 줄어든다.
                root.transform.localScale = Vector3.one;

                var idle = FindResidueSprite("Player_Idle");
                if (idle != null)
                {
                    // 꺼 두기만 하면 좌우 반전·피격 점멸이 이 렌더러를 먼저 집어서 문제가 난다
                    // (안 보이는 그림만 뒤집히고, 점멸이 끝나면 구형 그림이 켜진 채 남는다).
                    // 아예 없애면 "보이는 렌더러는 Art 자식 하나"가 되어 헷갈릴 여지가 없다.
                    var rootRenderer = root.GetComponent<SpriteRenderer>();
                    if (rootRenderer != null) Object.DestroyImmediate(rootRenderer);

                    var artTransform = root.transform.Find("Art");
                    var artObject = artTransform != null ? artTransform.gameObject : new GameObject("Art");
                    artObject.transform.SetParent(root.transform, false);

                    var artRenderer = artObject.GetComponent<SpriteRenderer>();
                    if (artRenderer == null) artRenderer = artObject.AddComponent<SpriteRenderer>();
                    artRenderer.sprite = idle;
                    artRenderer.sortingOrder = 10;

                    // 크기는 애니메이터가 프레임마다 맞춘다(클립별 원본 셀 크기가 달라서
                    // 여기서 한 번 정해 두면 동작마다 캐릭터가 커졌다 작아진다).
                    artObject.transform.localScale = Vector3.one;

                    AttachPlayerAnimator(artObject, artRenderer, 1.5f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PatchMainCamera();
                Debug.Log("[PrefabBuilder] Player 프리팹 갱신 완료(무마찰 재질·입력 펌프·공격 연출·스프라이트 애니메이션)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 착지·벽점프 더스트 파티클용 언릿 머티리얼. FrictionlessPlayerMaterial()과 같은
        // "있으면 재사용, 없으면 생성" 패턴.
        static Material DustParticleMaterial()
        {
            const string path = "Assets/Settings/DustParticle.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var sprite = LoadSprite("Dust");
            if (sprite == null)
            {
                PlaceholderArtBuilder.Run(); // Dust 스프라이트가 아직 없으면 플레이스홀더 세트를 생성
                sprite = LoadSprite("Dust");
            }
            if (sprite != null) mat.mainTexture = sprite.texture;

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // 착지("LandDust")·벽점프("WallDust") 더스트 프리팹을 만든다. playOnAwake +
        // stopAction=Destroy라 Instantiate만 하면 재생 후 스스로 파괴된다 — 호출부(PlayerJuice)에서
        // 별도 타이머를 둘 필요가 없다.
        [MenuItem("Hidden Weight/Fix/Build Dust Prefabs")]
        public static void BuildDustPrefabs()
        {
            EnsureFolder();
            BuildDustPrefab("LandDust");
            BuildDustPrefab("WallDust");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabBuilder] 더스트 프리팹 2종 생성 완료");
        }

        static GameObject BuildDustPrefab(string name)
        {
            string path = $"{Folder}/{name}.prefab";
            var root = new GameObject(name);
            var ps = root.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = 0.3f;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.startColor = new Color(0.85f, 0.82f, 0.75f, 0.9f);
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            shape.arc = 180f; // 위쪽 반원으로만 퍼진다

            var psRenderer = root.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sharedMaterial = DustParticleMaterial();
            psRenderer.sortingOrder = 11; // Player(10)보다 위

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // 프리팹 13종을 전부 다시 짓지 않고 Player 프리팹에만 손맛 이펙트(PlayerJuice)를 붙인다.
        // ApplyPlayerPhysicsMaterial()과 같은 이유로 부분 패치한다(전체 재생성은 인스턴스 오버라이드가 끊김).
        [MenuItem("Hidden Weight/Fix/Apply Player Juice")]
        public static void ApplyPlayerJuice()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/LandDust.prefab") == null
                || AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/WallDust.prefab") == null)
            {
                BuildDustPrefabs();
            }

            string path = $"{Folder}/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var juice = root.GetComponent<PlayerJuice>();
                if (juice == null) juice = root.AddComponent<PlayerJuice>();

                var groundCheck = root.transform.Find("GroundCheck");
                var wallCheck = root.transform.Find("WallCheck");

                var so = new SerializedObject(juice);
                so.FindProperty("groundCheck").objectReferenceValue = groundCheck;
                so.FindProperty("wallCheck").objectReferenceValue = wallCheck;
                so.FindProperty("landDustPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/LandDust.prefab");
                so.FindProperty("wallDustPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/WallDust.prefab");
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[PrefabBuilder] Player 프리팹에 PlayerJuice(손맛 이펙트) 추가 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // HUD 프리팹만 부분 패치한다(전체 재생성은 인스턴스 오버라이드가 끊기므로 지양 —
        // ApplyPlayerPhysicsMaterial()과 같은 이유). 하트를 단색 사각형이 아니라 픽셀아트
        // 하트 모양으로 보여주기 위해 PlaceholderArtBuilder가 만든 Heart 스프라이트를 연결한다.
        [MenuItem("Hidden Weight/Fix/Apply HUD Theme")]
        public static void ApplyHudTheme()
        {
            if (LoadSprite("Heart") == null)
            {
                PlaceholderArtBuilder.Run();
            }

            string path = $"{Folder}/HUD.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var hud = root.GetComponent<HUD>();
                var so = new SerializedObject(hud);
                so.FindProperty("heartSprite").objectReferenceValue = LoadSprite("Heart");
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[PrefabBuilder] HUD 프리팹에 하트 스프라이트 연결 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 카메라에 AudioListener가 없으면 Unity가 씬마다 경고를 내고 사운드가 전혀 재생되지 않는다.
        static void PatchMainCamera()
        {
            string path = $"{Folder}/MainCamera.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<AudioListener>() == null)
                {
                    root.AddComponent<AudioListener>();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log("[PrefabBuilder] MainCamera에 AudioListener 추가");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // "클립_00" 형식 프레임을 순서대로 모은다.
        static Sprite[] FindFrames(string clipName)
        {
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                var frame = FindResidueSprite($"{clipName}_{i:00}");
                if (frame == null) break;
                frames.Add(frame);
            }
            return frames.ToArray();
        }

        // Gameplay/README.md의 권장 FPS와 루프 여부를 그대로 옮겼다.
        static void AttachPlayerAnimator(GameObject target, SpriteRenderer renderer, float displayHeight)
        {
            var definitions = new (string clip, float fps, bool loop)[]
            {
                // 이동·행동 — PlayerState와 1:1로 대응한다(PlayerAnimator.ClipFor).
                ("PlayerIdle", 8f, true), ("PlayerWalk", 12f, true), ("PlayerRun", 14f, true),
                ("PlayerJump", 12f, false), ("PlayerAirMove", 10f, true), ("PlayerFall", 10f, true),
                ("PlayerLand", 14f, false), ("PlayerAttack", 16f, false), ("PlayerDash", 18f, false),
                ("PlayerWallCling", 8f, true), ("PlayerWallJump", 14f, false),

                // 반응 VFX — PlayerVFX_v1. 상태가 아니라 사건이라 PlayerAnimator의
                // 덮어쓰기 계층으로 재생한다(PlayerHealth가 호출).
                ("PlayerHit", 14f, false), ("PlayerDeath", 10f, false), ("PlayerRespawn", 10f, false),

                // 감정 능력 — Art/Player/Abilities의 두 시트. 시작·유지·마무리 3단이라
                // 역시 덮어쓰기 계층으로 재생한다(HushSkill / AwarenessSystem).
                ("HushBegin", 14f, false), ("HushMove", 10f, true), ("HushEnd", 14f, false),
                ("AwarenessBegin", 14f, false), ("AwarenessLoop", 8f, true), ("AwarenessUnlock", 10f, false),
            };

            var valid = new System.Collections.Generic.List<(string, float, bool, Sprite[])>();
            foreach (var definition in definitions)
            {
                var frames = FindFrames(definition.clip);
                if (frames.Length > 0) valid.Add((definition.clip, definition.fps, definition.loop, frames));
            }
            if (valid.Count == 0) return;

            var animator = target.GetComponent<HiddenWeight.World.SpriteAnimator>();
            if (animator == null) animator = target.AddComponent<HiddenWeight.World.SpriteAnimator>();

            var so = new SerializedObject(animator);
            so.FindProperty("target").objectReferenceValue = renderer;
            so.FindProperty("normalizedHeight").floatValue = displayHeight;

            // 프레임별 재정규화가 아니라 대기 포즈 기준 고정 배율을 쓴다. 프레임별로 다시
            // 재면 웅크린 대시 포즈가 서 있는 키까지 뻥튀기되고, 잔상이 섞인 프레임은 몸이
            // 줄어든다("공격·대시 때 작아진다") — 첫 클립이 PlayerIdle이어야 기준이 맞는다.
            so.FindProperty("uniformScale").boolValue = true;

            // 콜라이더 바닥(CapsuleCollider2D size.y=1.4, offset 0 → 로컬 -0.7)에 그림 속
            // 발을 고정한다. 프레임마다 캔버스 여백이 달라 피벗만으로는 "공중에 뜬 것처럼
            // 보인다" 버그가 났다 — SpriteAnimator.Apply()가 매 프레임 다시 계산한다.
            so.FindProperty("lockFeetToGround").boolValue = true;
            so.FindProperty("groundY").floatValue = -0.7f;

            var clips = so.FindProperty("clips");
            clips.arraySize = valid.Count;
            for (int i = 0; i < valid.Count; i++)
            {
                var element = clips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = valid[i].Item1;
                element.FindPropertyRelative("fps").floatValue = valid[i].Item2;
                element.FindPropertyRelative("loop").boolValue = valid[i].Item3;

                var frames = element.FindPropertyRelative("frames");
                frames.arraySize = valid[i].Item4.Length;
                for (int f = 0; f < valid[i].Item4.Length; f++)
                    frames.GetArrayElementAtIndex(f).objectReferenceValue = valid[i].Item4[f];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[PrefabBuilder] 플레이어 클립 {valid.Count}개 연결");
        }

        // HUD 프리팹에 상태 문양 프레임(ResidueStatusUI_v1의 세 행)을 넣는다.
        //
        // 프리팹 13종을 전부 다시 짓지 않는다 — 다시 돌리면 프리팹 내부 fileID가 바뀌어 씬의
        // 인스턴스 오버라이드가 끊긴다(ApplyPlayerPhysicsMaterial의 주석과 같은 이유).
        [MenuItem("Hidden Weight/Fix/Apply HUD Status Emblem")]
        public static void ApplyHudStatusEmblem()
        {
            string path = $"{Folder}/HUD.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var hud = root.GetComponent<HUD>();
                if (hud == null)
                {
                    Debug.LogWarning("[PrefabBuilder] HUD 프리팹에 HUD 컴포넌트가 없다");
                    return;
                }

                int filled = 0;
                filled += AssignFrames(hud, "rewindStatusFrames", "StatusRewind");
                filled += AssignFrames(hud, "dangerStatusFrames", "StatusDanger");
                filled += AssignFrames(hud, "progressStatusFrames", "StatusProgress");

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[PrefabBuilder] HUD 상태 문양 프레임 {filled}장 연결");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 지역별 상태 문양 프레임을 ZoneData에 채운다. HUD 프리팹은 하나뿐이라, 지역이
        // 바뀔 때 갈아끼울 프레임은 지역 데이터가 들고 있어야 한다.
        [MenuItem("Hidden Weight/Fix/Apply Zone Status Frames")]
        public static void ApplyZoneStatusFrames()
        {
            int total = 0;
            total += FillZoneStatus("Zone_Residue", "Assets/Art/Residue",
                "StatusRewind", "StatusDanger", "StatusProgress");
            total += FillZoneStatus("Zone_Gaze", "Assets/Art/Gaze",
                "GazeStatusTruth", "GazeStatusExposed", "GazeStatusProgress");
            // 균열: 예지(지역 능력) / 가능성(위험) / 진행. 행 이름은 FractureAnimationArtSlicer.
            total += FillZoneStatus("Zone_Fracture", "Assets/Art/Fracture",
                "FractureStatusForesight", "FractureStatusPossibility", "FractureStatusProgress");
            total += FillZoneMapIcons("Zone_Residue", "Assets/Art/Residue/UI", "UIIcon_r4_c");
            total += FillZoneMapIcons("Zone_Gaze", "Assets/Art/Gaze/UI", "GazeUIIcon_r4_c");
            total += FillZoneMapIcons("Zone_Fracture", "Assets/Art/Fracture/UI", "FractureUIIcon_r4_c");

            AssetDatabase.SaveAssets();
            Debug.Log($"[PrefabBuilder] 지역 상태 문양 프레임 {total}장 연결");
        }

        static int FillZoneMapIcons(string zoneAsset, string artRoot, string prefix)
        {
            var zone = LoadData<HiddenWeight.Data.ZoneData>(zoneAsset);
            if (zone == null) return 0;

            var so = new SerializedObject(zone);
            var property = so.FindProperty("mapStateIcons");
            if (property == null) return 0;
            property.arraySize = 8;
            int filled = 0;
            for (int i = 0; i < 8; i++)
            {
                var sprite = FindSpriteIn(artRoot, prefix + (i + 1));
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                if (sprite != null) filled++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
            return filled;
        }

        static int FillZoneStatus(string zoneAsset, string artRoot,
                                  string rewindClip, string dangerClip, string progressClip)
        {
            var zone = LoadData<HiddenWeight.Data.ZoneData>(zoneAsset);
            if (zone == null)
            {
                Debug.LogWarning($"[PrefabBuilder] 지역 데이터를 찾지 못했다: {zoneAsset}");
                return 0;
            }

            int filled = 0;
            filled += AssignFramesFrom(zone, "statusRewindFrames", artRoot, rewindClip);
            filled += AssignFramesFrom(zone, "statusDangerFrames", artRoot, dangerClip);
            filled += AssignFramesFrom(zone, "statusProgressFrames", artRoot, progressClip);
            EditorUtility.SetDirty(zone);
            return filled;
        }

        static int AssignFramesFrom(UnityEngine.Object target, string propertyName,
                                    string artRoot, string clipName)
        {
            var frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                var frame = FindSpriteIn(artRoot, $"{clipName}_{i:00}");
                if (frame == null) break;
                frames.Add(frame);
            }

            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);

            // main의 HUD가 상태 문양 필드를 갖고 있지 않을 수 있다(머지에서 main 쪽을 유지했다).
            // 그 경우 조용히 건너뛴다 — 메뉴를 눌렀다고 에디터가 죽으면 안 된다.
            if (property == null) return 0;

            property.arraySize = frames.Count;
            for (int i = 0; i < frames.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return frames.Count;
        }

        static Sprite FindSpriteIn(string root, string spriteName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite && sprite.name == spriteName) return sprite;
            }
            return null;
        }

        static int AssignFrames(UnityEngine.Object target, string propertyName, string clipName)
        {
            var frames = FindFrames(clipName);

            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null) return 0;

            property.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return frames.Length;
        }

        // 잘라 둔 스프라이트를 이름으로 찾는다.
        //
        // 잔재 폴더만 뒤지면 안 된다. 플레이어 능력 시트(숨죽이기·자각)는 지역이 아니라
        // 캐릭터에 딸린 아트라 Assets/Art/Player 아래에 있고, 여기를 빠뜨리면 잘라 둔 프레임을
        // 한 장도 못 찾아 클립이 통째로 비어 버린다.
        static readonly string[] SlicedArtRoots = { "Assets/Art/Residue", "Assets/Art/Player" };

        static Sprite FindResidueSprite(string spriteName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", SlicedArtRoots))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite && sprite.name == spriteName) return sprite;
            }
            return null;
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
            // 오디오 리스너가 없으면 Unity가 매 씬마다 경고를 내고 모든 사운드가 재생되지 않는다.
            // 카메라에 붙여 두면 지역마다 따로 챙길 필요가 없다.
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
