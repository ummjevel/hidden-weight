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

            // --- Gameplay (피벗 Center: 캐릭터·아이템은 가운데가 편하다) ---
            new Sheet { Path = "Gameplay/Player/Player_KeyPoses_v1.png",
                        Columns = 4, Rows = 2, Pivot = new Vector2(0.5f, 0.5f),
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
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "PlayerIdle", "PlayerWalk", "PlayerRun" } },
            new Sheet { Path = "Gameplay/Player/Animation/Player_Aerial_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "PlayerJump", "PlayerAirMove", "PlayerFall", "PlayerLand" } },
            new Sheet { Path = "Gameplay/Player/Animation/Player_Actions_v1.png",
                        Columns = 6, Rows = 2, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "PlayerAttack", "PlayerDash" } },
            new Sheet { Path = "Gameplay/Player/Animation/Player_Wall_v1.png",
                        Columns = 6, Rows = 2, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "PlayerWallCling", "PlayerWallJump" } },

            new Sheet { Path = "Gameplay/Enemies/Animation/ResidueWalker_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WalkerIdle", "WalkerWalk", "WalkerAttack", "WalkerHit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HangingFinger_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "FingerIdle", "FingerWalk", "FingerAttack", "FingerHit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/MourningCarrier_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "CarrierIdle", "CarrierWalk", "CarrierAttack", "CarrierHit" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HardenedResidue_v1.png",
                        Columns = 4, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "HardenedIdle", "HardenedWalk", "HardenedAttack", "HardenedHit" } },

            new Sheet { Path = "Gameplay/Bosses/Animation/WristWatcher_Combat_v1.png",
                        Columns = 6, Rows = 4, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WatcherAnimIdle", "WatcherAnimSweep", "WatcherAnimCharge", "WatcherAnimStun" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/WristWatcher_Reactions_v1.png",
                        Columns = 6, Rows = 3, Pivot = new Vector2(0.5f, 0.5f),
                        RowClips = new[] { "WatcherAnimDrop", "WatcherAnimHurt", "WatcherAnimDeath" } },

            new Sheet { Path = "Environment/Interactables/Animation/RewindPlatforms_Animation_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "RewindSmall", "RewindMedium", "RewindChainBridge" } },
            new Sheet { Path = "Environment/Hazards/Animation/Hazards_Animation_v2.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "HazardSpike", "HazardTentacle", "HazardCrusher" } },
            new Sheet { Path = "Environment/Props/Animation/AmbientProps_Animation_v1.png",
                        Columns = 8, Rows = 3, Pivot = new Vector2(0.5f, 0f),
                        RowClips = new[] { "PropShroud", "PropCage", "PropLantern" } },
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

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                // 원본 해상도 그대로 잘라야 격자가 어긋나지 않는다.
                importer.maxTextureSize = Mathf.Max(2048, Mathf.Max(texture.width, texture.height));

                importer.spritesheet = BuildRects(texture.width, texture.height, sheet);
                importer.SaveAndReimport();
                sliced++;
            }

            AssetDatabase.Refresh();
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
