using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 응시 지역의 애니메이션·UI 시트를 행 단위 클립으로 자른다.
    //
    // GazeEnvironmentArtSlicer는 정지 아틀라스(지형·발판·엄폐물 등)를 Prefix_r{행}_c{열}로
    // 자르는 도구다. 이쪽은 "행 하나 = 클립 하나"인 애니메이션 시트를 다루므로 이름 규칙이
    // 다르다(클립_00, 클립_01 …). 두 규칙을 한 파일에 섞으면 표를 읽기 어려워져 따로 둔다.
    //
    // 규격은 docs/concept-art/generated/gaze-animation-sprites/PROMPTS.md와
    // gaze-completion-assets/PROMPTS.md의 납품 표 그대로다. 전부 8열이다.
    //
    // 적 시트의 2행은 문서상 "Move"지만 클립 이름은 Walk로 붙인다 — EnemyPatrol이
    // PlayClip("Walk")를 부르기 때문이다. 이름이 어긋나면 잘려 있어도 재생되지 않는다.
    public static class GazeAnimationArtSlicer
    {
        const string Root = "Assets/Art/Gaze";

        sealed class Sheet
        {
            public string Path;
            public int Rows;
            public Vector2 Pivot;
            public string[] RowClips;
            public string Prefix;   // RowClips가 없을 때 Prefix_r{행}_c{열}로 붙인다
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        static readonly Sheet[] Sheets =
        {
            // --- 적 4종 (8x6: Idle / Move / Telegraph / Attack / Hit / Death) ---
            new Sheet { Path = "Gameplay/Enemies/Animation/BlindPilgrim_v1.png", Rows = 6, Pivot = Center,
                        RowClips = new[] { "PilgrimIdle", "PilgrimWalk", "PilgrimTelegraph",
                                           "PilgrimAttack", "PilgrimHit", "PilgrimDeath" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/InformingMouth_v1.png", Rows = 6, Pivot = Center,
                        RowClips = new[] { "MouthIdle", "MouthWalk", "MouthTelegraph",
                                           "MouthAttack", "MouthHit", "MouthDeath" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/HangingAudience_v1.png", Rows = 6, Pivot = Center,
                        RowClips = new[] { "AudienceIdle", "AudienceWalk", "AudienceTelegraph",
                                           "AudienceAttack", "AudienceHit", "AudienceDeath" } },
            new Sheet { Path = "Gameplay/Enemies/Animation/FacelessJudge_v1.png", Rows = 6, Pivot = Center,
                        RowClips = new[] { "JudgeIdle", "JudgeWalk", "JudgeTelegraph",
                                           "JudgeAttack", "JudgeHit", "JudgeDeath" } },

            // --- 보스 2종 ---
            new Sheet { Path = "Gameplay/Bosses/Animation/IrisGatekeeper_Combat_v1.png", Rows = 7, Pivot = Center,
                        RowClips = new[] { "GatekeeperIdle", "GatekeeperGazeSweep", "GatekeeperEyelid",
                                           "GatekeeperCharge", "GatekeeperDualGaze", "GatekeeperHit",
                                           "GatekeeperDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/IrisGatekeeper_Transitions_v1.png", Rows = 3, Pivot = Center,
                        RowClips = new[] { "GatekeeperEntrance", "GatekeeperOverload", "GatekeeperShortcut" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/GazeOfAll_Combat_v1.png", Rows = 7, Pivot = Center,
                        RowClips = new[] { "AllEyesIdle", "AllEyesFixedGaze", "AllEyesRotatingGaze",
                                           "AllEyesProjectile", "AllEyesTrueStrike", "AllEyesHit",
                                           "AllEyesDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/GazeOfAll_Deceptions_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "AllEyesFalseTelegraph", "AllEyesTrueTelegraph",
                                           "AllEyesDelayedImitation", "AllEyesDisappear" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/GazeOfAll_Reactions_v1.png", Rows = 3, Pivot = Center,
                        RowClips = new[] { "AllEyesAwarenessExposure", "AllEyesFinal", "AllEyesAudienceTurn" } },

            // --- 환경 전환 ---
            new Sheet { Path = "Environment/Hazards/Animation/EyeHazardTransitions_v1.png", Rows = 5, Pivot = Center,
                        RowClips = new[] { "GazeEyeOpen", "GazeEyeClose", "GazeBeamTelegraph",
                                           "GazeBeamDischarge", "GazeClusterAlarm" } },
            new Sheet { Path = "Environment/Interactables/Animation/CoverTransitions_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "CoverCurtainClose", "CoverCurtainOpen",
                                           "CoverMaskShield", "CoverBreak" } },
            new Sheet { Path = "Environment/Interactables/Animation/TransitTransitions_v1.png", Rows = 3, Pivot = Bottom,
                        RowClips = new[] { "TransitCageLift", "TransitIrisBridge", "TransitChainRelease" } },
            new Sheet { Path = "Environment/Interactables/Animation/AwarenessObjectTransitions_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "AwarenessShrine", "AwarenessTruthLens",
                                           "AwarenessMemoryMirror", "AwarenessObservationSeal" } },
            new Sheet { Path = "Environment/Interactables/Animation/GazeArenaTransitions_v1.png", Rows = 3, Pivot = Bottom,
                        RowClips = new[] { "ArenaIrisClose", "ArenaIrisOpen", "ArenaAudienceBarrier" } },
            new Sheet { Path = "Environment/Interactables/Animation/GazeCheckpointTransitions_v1.png", Rows = 3, Pivot = Bottom,
                        RowClips = new[] { "GazeCheckpointDormant", "GazeCheckpointActivate",
                                           "GazeCheckpointLoop" } },
            new Sheet { Path = "Environment/Interactables/Animation/GazeRoomTransitions_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "GazeSealClose", "GazeSealOpen",
                                           "GazeShortcutOpen", "GazeSecretPassage" } },
            new Sheet { Path = "Environment/Terrain/Animation/GazePlatformStates_v1.png", Rows = 4, Pivot = Bottom,
                        RowClips = new[] { "GazePlatformWatch", "GazePlatformCollapse",
                                           "GazePlatformEmpty", "GazePlatformRestore" } },

            // --- 환경 모션 ---
            new Sheet { Path = "Environment/VFX/Animation/GazeAmbientMotion_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeAmbientDust", "GazeAmbientWeb",
                                           "GazeAmbientDistantEyes", "GazeAmbientMist" } },
            new Sheet { Path = "Environment/VFX/Animation/GazeBackgroundMotion_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeBgIris", "GazeBgWindows", "GazeBgCage", "GazeBgCrowd" } },
            new Sheet { Path = "Environment/VFX/Animation/GazeForegroundMotion_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeFgChains", "GazeFgCurtain", "GazeFgMask", "GazeFgMist" } },

            // --- 게임플레이 VFX·공격체 ---
            new Sheet { Path = "Gameplay/Items/Animation/GazeCollectibleTransitions_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeItemShard", "GazeItemToken",
                                           "GazeItemHealing", "GazeItemMap" } },
            new Sheet { Path = "Gameplay/VFX/GazeSecondaryVFX_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeVfxHit", "GazeVfxReveal",
                                           "GazeVfxBuildup", "GazeVfxDeath" } },
            new Sheet { Path = "Gameplay/VFX/Animation/GazeEnemyProjectiles_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeProjSound", "GazeProjScream",
                                           "GazeProjShadow", "GazeProjVerdict" } },
            new Sheet { Path = "Gameplay/VFX/Animation/GazeBossProjectiles_v1.png", Rows = 5, Pivot = Center,
                        RowClips = new[] { "GazeBossScanBeam", "GazeBossEyelidShard", "GazeBossChainWhip",
                                           "GazeBossFalseEye", "GazeBossTrueStrike" } },
            new Sheet { Path = "Gameplay/VFX/Animation/GazeImpactVFX_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "GazeImpactMelee", "GazeImpactBeam",
                                           "GazeImpactLand", "GazeImpactGuardBreak" } },

            // --- UI ---
            new Sheet { Path = "UI/GazeUIIcons_v1.png", Rows = 4, Pivot = Center, Prefix = "GazeUIIcon" },
            new Sheet { Path = "UI/Animation/GazeStatusUI_v1.png", Rows = 3, Pivot = Center,
                        RowClips = new[] { "GazeStatusTruth", "GazeStatusExposed", "GazeStatusProgress" } },
        };

        const int Columns = 8;

        [MenuItem("Hidden Weight/Art/Slice Gaze Animation Sheets")]
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
                    Debug.LogWarning($"[GazeAnimationArtSlicer] 시트를 찾지 못했다: {path}");
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

                // SaveAndReimport만으로는 .meta에는 이름이 들어가는데 실제 서브에셋이 갱신되지
                // 않는 경우가 있다(InformingMouth가 실제로 그랬다 — 잘려 있는데 코드에서
                // 이름으로 찾으면 없었다). 강제 재임포트로 확실히 반영시킨다.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sliced++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[GazeAnimationArtSlicer] 시트 {sliced}개 분할 완료");
        }

        static SpriteMetaData[] BuildRects(int width, int height, Sheet sheet)
        {
            int cellWidth = width / Columns;
            int cellHeight = height / sheet.Rows;
            bool fixMouthVerticalBleed = sheet.Path.EndsWith("InformingMouth_v1.png");
            var sprites = new List<SpriteMetaData>(Columns * sheet.Rows);

            for (int row = 0; row < sheet.Rows; row++)
            {
                // 밀고하는 입은 원본 그림이 각 170px 행 아래로 약 20px씩 넘쳐 있다.
                // 균등 분할하면 이전 행의 꼬리가 다음 프레임 위에 사각 조각으로 붙고 본체
                // 하단은 잘린다. 첫 행은 아래 여백까지 넓히고, 이후 행은 창을 20px 내린다.
                int cropTop = row * cellHeight;
                int cropHeight = cellHeight;
                if (fixMouthVerticalBleed)
                {
                    const int bleed = 20;
                    if (row == 0)
                        cropHeight += bleed;
                    else
                    {
                        cropTop += bleed;
                        if (row == sheet.Rows - 1) cropHeight -= bleed;
                    }
                }

                // 문서는 맨 위를 1행으로 적고 Unity 텍스처 좌표는 아래가 0이라 여기서 뒤집는다.
                int y = height - cropTop - cropHeight;

                for (int column = 0; column < Columns; column++)
                {
                    string name = sheet.RowClips != null
                        ? $"{sheet.RowClips[row]}_{column:00}"
                        : $"{sheet.Prefix}_r{row + 1}_c{column + 1}";

                    sprites.Add(new SpriteMetaData
                    {
                        name = name,
                        rect = new Rect(column * cellWidth, y, cellWidth, cropHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = sheet.Pivot,
                    });
                }
            }

            return sprites.ToArray();
        }
    }
}
