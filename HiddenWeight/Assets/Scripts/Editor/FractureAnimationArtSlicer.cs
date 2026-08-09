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

            // 한 행에 두 동작이 이어 붙어 있는 경우. (행 번호, 뒷 동작이 시작하는 열, 뒷 동작 이름).
            //
            // 적 시트 4행은 "피격·사망"이 한 줄에 들어 있다(이 파일 위쪽 시트 목록 주석).
            // 그런데 행 전체를 {접두사}Hit 한 클립으로 잘라 놓아, **한 대 맞을 때마다**
            // 적이 꽃잎으로 흩어져 사라졌다가 원래대로 되돌아왔다. 정작 죽을 때는
            // {접두사}Death가 없어 아무 연출 없이 그냥 사라졌다.
            public (int row, int splitColumn, string secondClip)[] RowSplits;

            // 프레임 x 위치를 격자가 아니라 **그림에서 재서** 자를지.
            //
            // 이 시트들을 만든 생성기는 프레임을 시트 폭/열 수 간격이 아니라 자기 나름의
            // 간격으로 늘어놓았다. 선행 그림자는 8열짜리 시트에 167px 간격으로 아홉 번,
            // 갈라진 자아는 161px 간격으로 아홉 번 그렸다. 192px 격자로 자르면 오차가
            // 누적돼 뒤쪽 프레임일수록 인물이 반으로 잘리고, 다섯 번째 칸쯤에서는 두
            // 인물의 반쪽이 한 칸에 같이 들어온다 — 게임에서 "몬스터 그림이 이상하다"로
            // 보이던 것이 이것이다. 캐릭터 시트만 켠다. 이펙트 시트는 그림이 칸을 넘나들어
            // 간격을 잴 수 없다.
            public bool MeasureFrames;

            public (string clip, int index) ClipAt(int row, int column)
            {
                if (RowSplits != null)
                    foreach (var split in RowSplits)
                        if (split.row == row && column >= split.splitColumn)
                            return (split.secondClip, column - split.splitColumn);
                return (RowClips[row], column);
            }
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        static readonly Sheet[] Sheets =
        {
            // --- 적 4종 (8x4: idle / movement / attack / hit·death) ---
            // 4행은 이름 그대로 피격과 사망이 한 줄에 붙어 있다. RowSplits로 갈라 준다.
            new Sheet { Path = "Gameplay/Enemies/Animation/AnxiousSprout_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "SproutIdle", "SproutWalk", "SproutAttack", "SproutHit" },
                        MeasureFrames = true,
                        // 4행 = 피격 2장 + 사망 6장. 앞 두 장만 피격 반응이고 나머지는 흩어져 사라지는 그림이다.
                        RowSplits = new[] { (3, 2, "SproutDeath") } },
            new Sheet { Path = "Gameplay/Enemies/Animation/LeadingShadow_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "PrecursorIdle", "PrecursorWalk", "PrecursorAttack", "PrecursorHit" },
                        MeasureFrames = true,
                        // 4행 = 피격 2장 + 사망 6장. 앞 두 장만 피격 반응이고 나머지는 흩어져 사라지는 그림이다.
                        RowSplits = new[] { (3, 2, "PrecursorDeath") } },
            new Sheet { Path = "Gameplay/Enemies/Animation/PossibilityCollector_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "CollectorIdle", "CollectorWalk", "CollectorAttack", "CollectorHit" },
                        MeasureFrames = true,
                        // 4행 = 피격 2장 + 사망 6장. 앞 두 장만 피격 반응이고 나머지는 흩어져 사라지는 그림이다.
                        RowSplits = new[] { (3, 2, "CollectorDeath") } },
            new Sheet { Path = "Gameplay/Enemies/Animation/SplitSelf_v1.png", Rows = 4, Pivot = Center,
                        RowClips = new[] { "SplitSelfIdle", "SplitSelfWalk", "SplitSelfAttack", "SplitSelfHit" },
                        MeasureFrames = true,
                        // 4행 = 피격 2장 + 사망 6장. 앞 두 장만 피격 반응이고 나머지는 흩어져 사라지는 그림이다.
                        RowSplits = new[] { (3, 2, "SplitSelfDeath") } },

            // --- 중간 보스: 초침의 감시자. 이 시트만 10열이다(PROMPTS.md 납품 표). ---
            new Sheet { Path = "Gameplay/Bosses/Animation/SecondHandWatcher_Combat_v1.png",
                        Rows = 7, Columns = 10, Pivot = Center, MeasureFrames = true,
                        RowClips = new[] { "SecondHandIdle", "SecondHandStalk", "SecondHandSlash",
                                           "SecondHandDelayed", "SecondHandTimeBolt", "SecondHandHit",
                                           "SecondHandDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/SecondHandWatcher_Transitions_v1.png", Rows = 4, Pivot = Center, MeasureFrames = true,
                        RowClips = new[] { "SecondHandEntrance", "SecondHandTeleport",
                                           "SecondHandPhase", "SecondHandShortcut" } },

            // --- 지역 보스: 아직 오지 않은 나 ---
            //
            // 이 시트들은 프레임마다 칸 안 여백이 크게 다르다(Possibilities 행은 그림 바닥이
            // 최대 47px, Reactions 행은 가로 중심이 최대 46px 흔들린다). 응시는 같은 문제를
            // _Aligned_v2 시트를 새로 그려서 풀었지만(GazeAnimationArtSlicer 주석), 균열은
            // 시트를 건드리지 않고 SpriteAnimator 쪽에서 잡는다 — 이 스프라이트들은
            // spriteMeshType이 Tight라 sprite.bounds가 "실제로 그려진 영역"을 돌려주므로,
            // uniformScale / lockFeetToGround / lockReferenceCenter가 그대로 먹는다
            // (Enemy.Awake의 보스 정렬 블록 참고).
            //
            // PNG 픽셀을 직접 옮겨 맞추는 방법은 쓰지 말 것. Tight 메시에서는 그림을 칸
            // 아래로 붙이는 순간 피벗(칸 중앙)과 그림 중심이 벌어져 보스가 통째로 내려앉는다.
            new Sheet { Path = "Gameplay/Bosses/Animation/UnarrivedSelf_Combat_v1.png", Rows = 7, Pivot = Center, MeasureFrames = true,
                        RowClips = new[] { "NotYetMeIdle", "NotYetMeGlide", "NotYetMeRibbon",
                                           "NotYetMeShards", "NotYetMeStagger", "NotYetMeHit",
                                           "NotYetMeDeath" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/UnarrivedSelf_Possibilities_v1.png", Rows = 4, Pivot = Center, MeasureFrames = true,
                        RowClips = new[] { "NotYetMeWinged", "NotYetMeBeast",
                                           "NotYetMeDivided", "NotYetMeOracle" } },
            new Sheet { Path = "Gameplay/Bosses/Animation/UnarrivedSelf_Reactions_v1.png", Rows = 3, Pivot = Center, MeasureFrames = true,
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
                importer.spritesheet = BuildRects(texture.width, texture.height, sheet, path);
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sliced++;
            }

            // 동기 임포트로 끝내는 이유는 FractureEnvironmentArtSlicer의 같은 자리 주석 참고.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[FractureAnimationArtSlicer] 시트 {sliced}개 분할 완료");
        }

        static SpriteMetaData[] BuildRects(int width, int height, Sheet sheet, string assetPath)
        {
            int cellWidth = width / sheet.Columns;
            int cellHeight = height / sheet.Rows;
            var sprites = new List<SpriteMetaData>(sheet.Columns * sheet.Rows);

            // 행마다 프레임 x 중심을 잰다. 재지 못한 행은 격자 그대로 간다.
            float[][] measured = sheet.MeasureFrames
                ? MeasureFrameCenters(assetPath, width, height, sheet)
                : null;

            for (int row = 0; row < sheet.Rows; row++)
            {
                int y = height - (row + 1) * cellHeight;
                float[] centers = measured != null ? measured[row] : null;

                for (int column = 0; column < sheet.Columns; column++)
                {
                    var (clip, index) = sheet.ClipAt(row, column);

                    Rect rect;
                    if (centers != null)
                    {
                        // 잰 간격을 칸 폭으로 쓴다. 격자 칸보다 좁아지므로 이웃 프레임이
                        // 딸려 들어오지 않는다.
                        float frameWidth = centers[centers.Length - 1];   // 마지막 칸에 폭을 실어 보낸다
                        float left = Mathf.Clamp(centers[column] - frameWidth * 0.5f,
                                                 0f, width - frameWidth);
                        rect = new Rect(Mathf.Round(left), y, Mathf.Round(frameWidth), cellHeight);
                    }
                    else
                    {
                        rect = new Rect(column * cellWidth, y, cellWidth, cellHeight);
                    }

                    sprites.Add(new SpriteMetaData
                    {
                        name = $"{clip}_{index:00}",
                        rect = rect,
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = sheet.Pivot,
                    });
                }
            }

            return sprites.ToArray();
        }

        // 시트 원본 파일을 직접 읽어 행마다 프레임 중심 x를 잰다.
        //
        // 임포트된 텍스처는 읽기 불가로 들어오므로 GetPixels를 쓸 수 없다. 파일 바이트를
        // 임시 Texture2D에 올리면 항상 읽을 수 있고, 에셋 임포트 설정을 건드리지 않는다.
        //
        // 반환은 행마다 길이 Columns+1인 배열이다 — 앞의 Columns개가 중심, 마지막 한 칸이
        // 프레임 폭이다. 재지 못한 행은 null.
        static float[][] MeasureFrameCenters(string assetPath, int importedWidth,
                                             int importedHeight, Sheet sheet)
        {
            byte[] bytes;
            try { bytes = System.IO.File.ReadAllBytes(assetPath); }
            catch { return null; }

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(bytes, markNonReadable: false))
            {
                Object.DestroyImmediate(source);
                return null;
            }

            int rawWidth = source.width, rawHeight = source.height;
            var pixels = source.GetPixels32();
            Object.DestroyImmediate(source);

            // 원본과 임포트 크기가 다르면(NPOT 확대) 잰 값을 임포트 좌표로 옮긴다.
            float toImported = rawWidth <= 0 ? 1f : (float)importedWidth / rawWidth;
            int rawCellHeight = rawHeight / sheet.Rows;
            float nominalPitch = (float)rawWidth / sheet.Columns;

            var result = new float[sheet.Rows][];
            for (int row = 0; row < sheet.Rows; row++)
            {
                // Texture2D 좌표는 아래가 0이라 문서 기준 행 번호를 뒤집는다.
                int bottom = rawHeight - (row + 1) * rawCellHeight;
                var mass = new int[rawWidth];
                for (int y = bottom; y < bottom + rawCellHeight; y++)
                {
                    int line = y * rawWidth;
                    for (int x = 0; x < rawWidth; x++)
                        if (pixels[line + x].a > 16) mass[x]++;
                }

                var centers = BlobCenters(mass, gap: 8);
                if (!TryFitPitch(centers, nominalPitch, sheet.Columns,
                                 out float start, out float pitch))
                {
                    result[row] = null;
                    continue;
                }

                var fitted = new float[sheet.Columns + 1];
                for (int i = 0; i < sheet.Columns; i++)
                    fitted[i] = (start + pitch * i) * toImported;
                fitted[sheet.Columns] = pitch * toImported;
                result[row] = fitted;
            }

            bool any = false;
            foreach (var row in result) if (row != null) any = true;
            if (!any) return null;

            // 재지 못한 행은 잰 행 중 하나를 빌려 쓴다. 같은 시트의 모든 행은 같은
            // 자리에 그려져 있고(생성기가 행마다 같은 x 배치를 쓴다), 이펙트가 번진 행은
            // 덩어리가 붙어 버려 스스로는 잴 수 없기 때문이다.
            float[] fallback = null;
            foreach (var row in result) if (row != null) { fallback = row; break; }
            for (int row = 0; row < sheet.Rows; row++)
                if (result[row] == null) result[row] = fallback;

            return result;
        }

        // 알파가 있는 열의 덩어리 중심들. gap 픽셀 이하로 떨어진 것은 한 덩어리로 본다.
        static List<float> BlobCenters(int[] mass, int gap)
        {
            var centers = new List<float>();
            int start = -1, last = -1;
            for (int x = 0; x < mass.Length; x++)
            {
                if (mass[x] <= 0) continue;
                if (start < 0) start = x;
                else if (x - last > gap) { centers.Add((start + last) * 0.5f); start = x; }
                last = x;
            }
            if (start >= 0) centers.Add((start + last) * 0.5f);
            return centers;
        }

        // 덩어리 중심들에서 "시작 + 일정 간격" 배치를 찾는다.
        //
        // 덩어리 수가 열 수와 다를 수 있다 — 생성기가 열 수보다 한 장 더 그려 두거나,
        // 이펙트가 붙어 두 프레임이 한 덩어리가 되기도 한다. 그래서 개수를 믿지 않고,
        // 이웃 간격이 격자 폭 근처인 것만 모아 최소제곱으로 직선을 맞춘다.
        static bool TryFitPitch(List<float> centers, float nominalPitch, int columns,
                                out float start, out float pitch)
        {
            start = 0f; pitch = nominalPitch;
            if (centers.Count < 4) return false;

            // 이웃 간격이 격자 폭의 0.6~1.4배인 구간만 한 줄로 이어 붙인다.
            var run = new List<float> { centers[0] };
            var best = new List<float>(run);
            for (int i = 1; i < centers.Count; i++)
            {
                float step = centers[i] - centers[i - 1];
                if (step > nominalPitch * 0.6f && step < nominalPitch * 1.4f)
                    run.Add(centers[i]);
                else
                {
                    if (run.Count > best.Count) best = new List<float>(run);
                    run = new List<float> { centers[i] };
                }
            }
            if (run.Count > best.Count) best = run;
            if (best.Count < 4) return false;

            // index → center 최소제곱 직선.
            int n = best.Count;
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumXX = 0f;
            for (int i = 0; i < n; i++)
            {
                sumX += i; sumY += best[i];
                sumXY += i * best[i]; sumXX += (float)i * i;
            }
            float denominator = n * sumXX - sumX * sumX;
            if (Mathf.Abs(denominator) < 0.001f) return false;

            pitch = (n * sumXY - sumX * sumY) / denominator;
            start = (sumY - pitch * sumX) / n;

            // 맞은 간격이 격자와 크게 다르면 잘못 잡은 것으로 보고 격자로 돌아간다.
            if (pitch < nominalPitch * 0.6f || pitch > nominalPitch * 1.4f) return false;
            // 마지막 프레임이 시트 밖으로 나가면 시작점을 잘못 잡은 것이다.
            if (start < 0f || start + pitch * (columns - 1) > nominalPitch * columns) return false;
            return true;
        }
    }
}
