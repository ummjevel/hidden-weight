using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 균열 지역의 애니메이션 시트를 "행 하나 = 클립 하나"로 자른다(클립_00, 클립_01 …).
    // 정지 아틀라스는 FractureEnvironmentArtSlicer가 맡는다.
    //
    // 행 의미는 docs/concept-art/generated/fracture-gameplay-art/PROMPTS.md의 납품 표와
    // 생성기 build_fracture_gameplay_art.py의 행 분기에서 가져왔다.
    //
    // 이름 규칙에서 지키는 것 두 가지:
    //  - 적 클립 접두사는 FractureEnemyKind 이름 그대로다(Sprout / Precursor / Collector /
    //    SplitSelf). Enemy.clipPrefix + "Idle"로 조회하므로 어긋나면 재생되지 않는다.
    //  - 적 2행은 문서상 "movement"지만 클립 이름은 Walk다 — EnemyPatrol이 PlayClip("Walk")를
    //    부른다.
    //  - 타격 연출은 지역 접두사를 붙이지 않는다. PlayerAttack이 ImpactVFX.Play("ImpactMelee")를,
    //    PlayerController가 "ImpactLand"/"ImpactHeavy"를 이름 그대로 부르기 때문이다(잔재와 같은
    //    규칙). 지역 분리는 아트 폴더가 이미 보장한다.
    public static class FractureAnimationArtSlicer
    {
        const string Root = "Assets/Art/Fracture";
        const int DefaultColumns = 8;

        sealed class Sheet
        {
            public string Path;
            public int Rows;
            public int Columns = DefaultColumns;
            public Vector2 Pivot;
            public string[] RowClips;
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        static readonly Sheet[] Sheets =
        {
            // --- 적 4종 (8x4: idle / movement / attack / hit·death) ---
            new Sheet { Path = "Gameplay/Enemies/Animation/AnxiousSprout_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "SproutIdle", "SproutWalk", "SproutAttack", "SproutHit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/LeadingShadow_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "PrecursorIdle", "PrecursorWalk", "PrecursorAttack", "PrecursorHit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/PossibilityCollector_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "CollectorIdle", "CollectorWalk", "CollectorAttack", "CollectorHit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/SplitSelf_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "SplitSelfIdle", "SplitSelfWalk", "SplitSelfAttack", "SplitSelfHit" } },

            // --- 중간 보스: 초침의 감시자. 이 시트만 10열이다(PROMPTS.md 납품 표). ---
            new Sheet { Path = "Gameplay/Bosses/Animation/SecondHandWatcher_Combat_v1.png",
                        Rows = 7, Columns = 10, Pivot = Center,
                        RowClips = new[] { "SecondHandIdle", "SecondHandStalk", "SecondHandSlash",
                                           "SecondHandDelayed", "SecondHandTimeBolt", "SecondHandHit",
                                           "SecondHandDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/SecondHandWatcher_Transitions_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "SecondHandEntrance", "SecondHandTeleport",
                                           "SecondHandPhase", "SecondHandShortcut" } },

            // --- 지역 보스: 아직 오지 않은 나 ---
            new Sheet { Path = "Gameplay/Bosses/Animation/UnarrivedSelf_Combat_v1.png", Rows = 7, Pivot = Center,
                        RowClips = new[] { "NotYetMeIdle", "NotYetMeGlide", "NotYetMeRibbon",
                                           "NotYetMeShards", "NotYetMeStagger", "NotYetMeHit",
                                           "NotYetMeDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/UnarrivedSelf_Possibilities_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "NotYetMeWinged", "NotYetMeBeast",
                                           "NotYetMeDivided", "NotYetMeOracle" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/UnarrivedSelf_Reactions_v1.png", Rows = 3, Pivot = Center,
                        RowClips = new[] { "NotYetMeAwakening", "NotYetMePhase", "NotYetMeAcceptance" } },

            // --- 환경 전환 ---
            new Sheet { Path = "Environment/Terrain/Animation/FracturePlatformStates_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "FracturePlatformSafe", "FracturePlatformFloat",
                                           // 3·4행 이름이 무접두사인 이유: CrumblingPlatform이
                                           // "PlatformCrack"/"PlatformCollapse"를 이름 그대로 부른다.
                                           "PlatformCrack", "PlatformCollapse" } },
            new Sheet { Path = "Environment/Hazards/Animation/FutureHazardTransitions_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureHazardBloom", "FractureHazardPulse",
                                           "FractureHazardBurst", "FractureHazardFade" } },
            new Sheet { Path = "Environment/Interactables/Animation/ForesightObjectTransitions_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "FractureForesightReveal", "FractureForesightFix",
                                           "FractureForesightFade", "FractureForesightLoop" } },
            // 1·2행을 숏컷 봉쇄·해제로 쓴다(응시가 GazeRoomTransitions를 쓴 자리와 같은 역할).
            new Sheet { Path = "Environment/Interactables/Animation/DoorShortcutTransitions_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "FractureSealClose", "FractureSealOpen",
                                           "FractureShortcutOpen", "FractureSecretPassage" } },
            new Sheet { Path = "Environment/Interactables/Animation/TransitTransitions_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "FractureTransitRing", "FractureTransitLift",
                                           "FractureTransitBridge", "FractureTransitRelease" } },
            new Sheet { Path = "Environment/Interactables/Animation/FractureRoomTransitions_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureRoomFade", "FractureRoomRays",
                                           "FractureRoomIris", "FractureRoomGate" } },

            // --- 환경 모션. 원경·전경은 3행이다(생성기 animated_effect(...,3)). ---
            new Sheet { Path = "Environment/VFX/Animation/FractureAmbientMotion_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureAmbientPetals", "FractureAmbientDust",
                                           "FractureAmbientGlass", "FractureAmbientMist" } },
            new Sheet { Path = "Environment/VFX/Animation/FractureBackgroundMotion_v1.png", Rows = 3, Pivot = Center,
                        RowClips = new[] { "FractureBgArches", "FractureBgWater", "FractureBgSkyCrack" } },
            new Sheet { Path = "Environment/VFX/Animation/FractureForegroundMotion_v1.png", Rows = 3, Pivot = Center,
                        RowClips = new[] { "FractureFgVines", "FractureFgFlowers", "FractureFgGlass" } },

            // --- 아이템·공격체·타격 ---
            new Sheet { Path = "Gameplay/Items/Animation/FractureCollectibleTransitions_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureItemShard", "FractureItemToken",
                                           "FractureItemHealing", "FractureItemMap" } },
            // 일반 공격체 시트의 1행은 보스 전용 분기라 비어 있다(생성기 `if boss and row==0`).
            // 이름만 자리로 남기고 쓰지 않는다 — 빈 클립을 등록하면 화면에 아무것도 없는
            // 공격체가 날아간다.
            new Sheet { Path = "Gameplay/VFX/Animation/FractureEnemyProjectiles_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureProjUnused", "FractureProjShards",
                                           "FractureProjArc", "FractureProjRing" } },
            // 3·4행은 보스 착지 고리와 단계 전환 파열로 쓴다(BossController의 기본 이름).
            new Sheet { Path = "Gameplay/VFX/Animation/FractureBossProjectiles_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureBossShard", "FractureBossCrystals",
                                           "BossRupture", "BossRing" } },
            new Sheet { Path = "Gameplay/VFX/Animation/FractureImpactVFX_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "ImpactMelee", "ImpactHeavy", "ImpactLand", "ImpactWall" } },

            // --- UI ---
            new Sheet { Path = "UI/Animation/FractureStatusUI_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "FractureStatusForesight", "FractureStatusPossibility",
                                           "FractureStatusFixed", "FractureStatusProgress" } },
        };

        [MenuItem("Hidden Weight/Art/Slice Fracture Animation Sheets")]
        public static void SliceAll()
        {
            int sliced = 0;
            foreach (var sheet in Sheets)
            {
                string path = $"{Root}/{sheet.Path}";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (importer == null || texture == null)
                {
                    Debug.LogWarning($"[FractureAnimationArtSlicer] 시트를 찾지 못했다: {path}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = Mathf.Max(2048, Mathf.Max(texture.width, texture.height));
                importer.spritesheet = BuildRects(texture.width, texture.height, sheet);
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sliced++;
            }

            // 동기 임포트로 끝내는 이유는 FractureEnvironmentArtSlicer의 같은 자리 주석 참고.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[FractureAnimationArtSlicer] 시트 {sliced}개 분할 완료");
        }

        static SpriteMetaData[] BuildRects(int width, int height, Sheet sheet)
        {
            int cellWidth = width / sheet.Columns;
            int cellHeight = height / sheet.Rows;
            var sprites = new List<SpriteMetaData>(sheet.Columns * sheet.Rows);

            for (int row = 0; row < sheet.Rows; row++)
            {
                int y = height - (row + 1) * cellHeight;
                for (int column = 0; column < sheet.Columns; column++)
                {
                    sprites.Add(new SpriteMetaData
                    {
                        name = $"{sheet.RowClips[row]}_{column:00}",
                        rect = new Rect(column * cellWidth, y, cellWidth, cellHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = sheet.Pivot,
                    });
                }
            }

            return sprites.ToArray();
        }
    }
}
