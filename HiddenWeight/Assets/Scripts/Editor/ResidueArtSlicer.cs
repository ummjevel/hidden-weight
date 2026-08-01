using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 잔재 아트 시트를 문서(Art/Residue/Environment/README.md, Gameplay/README.md)의 격자대로
    // 잘라 이름 붙인다. 이름을 붙여 두면 빌더가 "Item_Currency"처럼 의미로 집을 수 있어서,
    // 시트에 칸이 추가돼도 인덱스를 다시 세지 않아도 된다.
    //
    // 행 번호는 문서 표기(맨 위가 1행)를 따른다. Unity 텍스처 좌표는 아래가 0이라 여기서 뒤집는다.
    public static class ResidueArtSlicer
    {
        const string Root = "Assets/Art/Residue";

        sealed class Sheet
        {
            public string Path;
            public int Columns;
            public int Rows;
            public Vector2 Pivot;
            public string Prefix;          // 이름 배열이 없을 때 Prefix_r{행}_c{열}로 붙인다
            public string[] Names;         // 좌→우, 위→아래 순서
            public string[] RowClips;      // 행 = 클립. 프레임 이름은 "클립_00" 형식이 된다
        }

        // 문서의 "시트 분할" 표 그대로.
        static readonly Sheet[] Sheets =
        {
            // --- Environment (피벗 Bottom Center: 바닥에 세워 놓기 좋다) ---
            new Sheet { Path = "Environment/Terrain/Residue_TerrainTiles_v2.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0f), Prefix = "Terrain" },
            new Sheet { Path = "Environment/Terrain/Residue_Platforms_v1.png",
                        Columns = 6, Rows = 3, Pivot = new Vector2(0.5f, 0f), Prefix = "Platform" },
            new Sheet { Path = "Environment/Interactables/Residue_RewindStructures_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0f), Prefix = "Rewind" },
            new Sheet { Path = "Environment/Hazards/Residue_Hazards_v1.png",
                        Columns = 6, Rows = 3, Pivot = new Vector2(0.5f, 0f), Prefix = "Hazard" },
            new Sheet { Path = "Environment/Props/Residue_EnvironmentProps_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0f), Prefix = "Prop" },
            new Sheet { Path = "Environment/VFX/Residue_AmbientVFX_v1.png",
                        Columns = 6, Rows = 3, Pivot = new Vector2(0.5f, 0.5f), Prefix = "AmbientVFX" },

            // --- Gameplay ---
            new Sheet { Path = "Gameplay/Player/Player_KeyPoses_v1.png",
                        Columns = 4, Rows = 2, Pivot = new Vector2(0.5f, 0f),
                        Names = new[] { "Player_Idle", "Player_Walk", "Player_Run", "Player_Jump",
                                        "Player_Fall", "Player_Land", "Player_Attack", "Player_Dash" } },
            new Sheet { Path = "Gameplay/Enemies/Residue_Enemies_Atlas_v1.png",
                        Columns = 2, Rows = 2, Pivot = new Vector2(0.5f, 0.5f),
                        Names = new[] { "Enemy_Walker", "Enemy_Finger",
                                        "Enemy_Carrier", "Enemy_Hardened" } },
            new Sheet { Path = "Gameplay/Items/Residue_ItemsHazards_Atlas_v1.png",
                        Columns = 3, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        Names = new[] { "Item_Currency", "Item_Healing", "Item_HealthShard",
                                        "Item_Fragment", "Item_Spike", "Item_VoidWarning",
                                        "Item_CrumbleIntact", "Item_CrumbleFractured", "Item_BrokenPulley" } },
            new Sheet { Path = "Gameplay/Props/Residue_Shortcuts_Atlas_v1.png",
                        Columns = 3, Rows = 2, Pivot = new Vector2(0.5f, 0f),
                        Names = new[] { "Shortcut_ChainBroken", "Shortcut_ChainRestored", "Shortcut_LiftDormant",
                                        "Shortcut_LiftActive", "Shortcut_PulleyBroken", "Shortcut_PulleyRestored" } },
            new Sheet { Path = "Gameplay/Bosses/WristWatcher_Poses_v1.png",
                        Columns = 3, Rows = 2, Pivot = new Vector2(0.5f, 0.5f),
                        Names = new[] { "Watcher_Idle", "Watcher_SweepAnticipation", "Watcher_ChargeAnticipation",
                                        "Watcher_ChargeImpact", "Watcher_DropAttack", "Watcher_Hurt" } },

            // --- 애니메이션 시트 (행 = 클립, 열 = 프레임) ---
            // 이름은 "클립_00" 형식이라 런타임에서 클립 단위로 모을 수 있다.
            new Sheet { Path = "Gameplay/Player/Animation/Player_Locomotion_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PlayerIdle", "PlayerWalk", "PlayerRun" } },
            new Sheet { Path = "Gameplay/Player/Animation/Player_Aerial_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PlayerJump", "PlayerAirMove", "PlayerFall", "PlayerLand" } },
            new Sheet { Path = "Gameplay/Player/Animation/Player_Actions_v1.png",
                        Columns = 6, Rows = 2, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PlayerAttack", "PlayerDash" } },
            new Sheet { Path = "Gameplay/Player/Animation/Player_Wall_v1.png",
                        Columns = 6, Rows = 2, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PlayerWallCling", "PlayerWallJump" } },
            new Sheet { Path = "Gameplay/VFX/PlayerVFX_v1.png",
                        Columns = 6, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PlayerHit", "PlayerDeath", "PlayerRespawn" } },
            // 2행(참격 궤적)이 공격 스윙 연출로 쓰인다(AttackVisual). 나머지 행도 이름을
            // 붙여 두면 나중에 히트·폭발·링 연출에 그대로 가져다 쓸 수 있다.
            new Sheet { Path = "Gameplay/VFX/CombatVFX_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "SwingSpark", "SwingSlash", "SwingBurst", "SwingRing" } },

            // _v1은 _v2로 교체됐다. 그림 자체는 남겨 두되 클립 이름에서 비켜 세운다 —
            // PrefabBuilder.FindFrames가 스프라이트 "이름"으로 찾기 때문에, 두 세대가 같은
            // 이름을 내면 어느 쪽이 걸릴지 알 수 없게 된다.
            new Sheet { Path = "Gameplay/Enemies/Animation/ResidueWalker_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WalkerV1Idle", "WalkerV1Walk", "WalkerV1Attack", "WalkerV1Hit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HangingFinger_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "FingerV1Idle", "FingerV1Walk", "FingerV1Attack", "FingerV1Hit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/MourningCarrier_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "CarrierV1Idle", "CarrierV1Walk", "CarrierV1Attack", "CarrierV1Hit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HardenedResidue_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "HardenedV1Idle", "HardenedV1Walk", "HardenedV1Attack", "HardenedV1Hit" } },

            // _v2 새 원화(부드러운 렌더링). 8×6이고 셀은 캐릭터마다 181px 또는(캐리어)
            // 202×161px로 제각각이라 파일별 실제 격자를 넣는다.
            //
            // 행 순서는 문서에 없어 시트를 직접 열어 읽었다. 여섯 행이
            // Idle / Walk / Run / Attack / Hit / Death 순이며, 워커와 캐리어 두 시트가
            // 서로 독립적으로 같은 배열을 보인다(3행에만 공격 궤적이 그려져 있고, 마지막
            // 행만 바닥으로 무너진다). _v1의 Idle/Walk/Attack/Hit 순서와도 어긋나지 않는다.
            //
            // 런타임이 쓰는 클립은 네 개뿐이라 Run·Death 행은 이름만 붙여 두고 아직
            // 소비하지 않는다. 사망 연출을 넣을 때 그대로 가져다 쓰면 된다.
            new Sheet { Path = "Gameplay/Enemies/Animation/ResidueWalker_v2.png",
                        Columns = 8, Rows = 6, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WalkerIdle", "WalkerWalk", "WalkerRun",
                                           "WalkerAttack", "WalkerHit", "WalkerDeath" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HangingFinger_v2.png",
                        Columns = 8, Rows = 6, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "FingerIdle", "FingerWalk", "FingerRun",
                                           "FingerAttack", "FingerHit", "FingerDeath" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/MourningCarrier_v2.png",
                        Columns = 8, Rows = 6, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "CarrierIdle", "CarrierWalk", "CarrierRun",
                                           "CarrierAttack", "CarrierHit", "CarrierDeath" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HardenedResidue_v2.png",
                        Columns = 8, Rows = 6, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "HardenedIdle", "HardenedWalk", "HardenedRun",
                                           "HardenedAttack", "HardenedHit", "HardenedDeath" } },

            new Sheet { Path = "Gameplay/Bosses/Animation/WristWatcher_Combat_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WatcherAnimIdle", "WatcherAnimSweep", "WatcherAnimCharge", "WatcherAnimStun" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/WristWatcher_Reactions_v1.png",
                        Columns = 6, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WatcherAnimDrop", "WatcherAnimHit", "WatcherAnimDeath" } },

            // --- R12 기억의 교관. 행 구성은 residue-animation-sprites/PROMPTS.md 납품 표 그대로.
            // 전용 대기 행이 없어 CoreHalo 1행(후광 순환)을 대기 클립으로 쓴다.
            // 피격 행 이름이 Hit인 것은 Enemy.PlayClip("Hit") 계약 때문이다(Watcher와 같다).
            new Sheet { Path = "Gameplay/Bosses/Animation/MemoryInstructor_Attacks_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "InstructorSweep", "InstructorHook",
                                           "InstructorSlam", "InstructorRecover" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/MemoryInstructor_Reactions_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "InstructorHit", "InstructorPhase", "InstructorDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/MemoryInstructor_CoreHalo_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "InstructorHalo", "InstructorCore", "InstructorOverload" } },

            new Sheet { Path = "Environment/Interactables/Animation/RewindPlatforms_Animation_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "RewindSmall", "RewindMedium", "RewindChainBridge" } },
            new Sheet { Path = "Environment/Hazards/Animation/Hazards_Animation_v2.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "HazardSpike", "HazardTentacle", "HazardCrusher" } },
            new Sheet { Path = "Environment/Props/Animation/AmbientProps_Animation_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PropShroud", "PropCage", "PropLantern" } },

            // --- 잔재 제작 마감 세트 9종 ---
            // 규격은 docs/concept-art/generated/residue-completion-assets/PROMPTS.md의 납품 표 그대로다.
            // 전부 8열 192x192이고, 바닥에 세우는 것(발판·방 전환)만 Bottom Center 피벗을 쓴다.
            new Sheet { Path = "Gameplay/VFX/Animation/ResidueEnemyProjectiles_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "ProjSplinter", "ProjClaw", "ProjChargeTrail", "ProjShockwave" } },
            new Sheet { Path = "Gameplay/VFX/Animation/ResidueBossProjectiles_v1.png",
                        Columns = 8, Rows = 5, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "BossWave", "BossRing", "BossNeedle", "BossRewindOrb", "BossRupture" } },
            new Sheet { Path = "Environment/Terrain/Animation/ResiduePlatformStates_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PlatformCrack", "PlatformCollapse", "PlatformBroken", "PlatformRestore" } },
            new Sheet { Path = "Gameplay/VFX/Animation/ResidueImpactVFX_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "ImpactMelee", "ImpactWall", "ImpactLand", "ImpactHeavy" } },
            new Sheet { Path = "Environment/VFX/Animation/ResidueForegroundMotion_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "FgChains", "FgCage", "FgFinger", "FgDust" } },
            new Sheet { Path = "Environment/VFX/Animation/ResidueBackgroundMotion_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "BgSmoke", "BgWindows", "BgHand", "BgCrowd" } },
            new Sheet { Path = "Environment/Interactables/Animation/ResidueRoomTransitions_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "SealClose", "SealOpen", "ShortcutRewind", "SecretWall" } },
            new Sheet { Path = "UI/ResidueUIIcons_v1.png",
                        Columns = 8, Rows = 4, Pivot = new Vector2(0.5f, 0.5f), Prefix = "UIIcon" },
            new Sheet { Path = "UI/Animation/ResidueStatusUI_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "StatusRewind", "StatusDanger", "StatusProgress" } },
        };

        [MenuItem("Hidden Weight/Art/Slice Residue Sheets")]
        public static void SliceAll()
        {
            int sliced = 0;
            foreach (var sheet in Sheets)
            {
                string path = $"{Root}/{sheet.Path}";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[ResidueArtSlicer] 시트를 찾지 못했다: {path}");
                    continue;
                }

                importer.GetSourceTextureWidthAndHeight(
                    out int sourceWidth,
                    out int sourceHeight);

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                // 원본 해상도 그대로 잘라야 격자가 어긋나지 않는다.
                importer.maxTextureSize =
                    Mathf.Max(2048, Mathf.Max(sourceWidth, sourceHeight));

                importer.spritesheet =
                    BuildRects(sourceWidth, sourceHeight, sheet);
                importer.SaveAndReimport();
                // 서브에셋 강제 갱신(엄폐물 시트가 meta만 갱신되고 씬 참조 0이던 것과 같은
                // 실패를 막는다 — GazeEnvironmentArtSlicer의 같은 자리 주석 참고).
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sliced++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[ResidueArtSlicer] 시트 {sliced}개 분할 완료");
        }

        static SpriteMetaData[] BuildRects(int width, int height, Sheet sheet)
        {
            int cellW = width / sheet.Columns;
            int cellH = height / sheet.Rows;
            var list = new List<SpriteMetaData>(sheet.Columns * sheet.Rows);

            for (int row = 0; row < sheet.Rows; row++)
            {
                // 문서의 1행은 맨 위다. Unity는 아래가 y=0이므로 뒤집는다.
                int y = height - (row + 1) * cellH;

                for (int col = 0; col < sheet.Columns; col++)
                {
                    int index = row * sheet.Columns + col;
                    string name;
                    if (sheet.RowClips != null && row < sheet.RowClips.Length)
                        name = $"{sheet.RowClips[row]}_{col:00}";
                    else if (sheet.Names != null && index < sheet.Names.Length)
                        name = sheet.Names[index];
                    else
                        name = $"{sheet.Prefix}_r{row + 1}_c{col + 1}";

                    list.Add(new SpriteMetaData
                    {
                        name = name,
                        rect = new Rect(col * cellW, y, cellW, cellH),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = sheet.Pivot,
                    });
                }
            }

            return list.ToArray();
        }
    }
}
