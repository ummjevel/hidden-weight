using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 균열 지역의 정지 아틀라스를 Prefix_r{행}_c{열}로 자른다.
    //
    // 격자는 문서가 아니라 실제 생성기(build_fracture_gameplay_art.py)의 배치 루프에서 가져왔다.
    // 균열 시트는 응시와 달리 파일마다 열 수가 다르다 — 발판은 3열, 문·이동 구조는 4열,
    // 환경 VFX는 9열이다. 응시 기준(6열)으로 자르면 셀이 통째로 어긋난다.
    //
    // 접두사는 공용 빌더가 쓰는 논리 이름과 맞춰야 한다(ZoneArt("Terrain_r1_c1") +
    // _artPrefix="Fracture" → "FractureTerrain_r1_c1"). 접두사를 바꾸면 씬이 조용히
    // 플레이스홀더로 돌아간다.
    public static class FractureEnvironmentArtSlicer
    {
        const string Root = "Assets/Art/Fracture";

        sealed class Sheet
        {
            public string Path;
            public int Columns;
            public int Rows;
            public Vector2 Pivot;
            public string Prefix;
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        static readonly Sheet[] Sheets =
        {
            // terrain_tiles(): 4행 x 6열
            // v1은 PIL로 그린 플레이스홀더라 24칸이 전부 같은 납작한 사각형이었다 — 게임
            // 화면이 박스로 보이던 원인이다. v2는 이미 일러스트로 완성된 발판 시트에서 파생해
            // 끝단·벽 켜·천장·경사를 구분한다(`build_fracture_terrain_v2.py`).
            // 접두사를 v1과 같게 두는 것은 의도적이다. 이름으로 조회하는 쪽
            // (`TraversalArtPalette`, 씬 빌더)이 그대로 새 그림을 받는다. 잔재가
            // Residue_TerrainTiles_v2로 갈아탄 것과 같은 방식이다.
            new Sheet { Path = "Environment/Terrain/Fracture_TerrainTiles_v2.png",
                        Columns = 6, Rows = 4, Pivot = Bottom, Prefix = "FractureTerrain" },

            // platforms(): 12칸, col=i%3 / row=i//3 → 3열 x 4행
            new Sheet { Path = "Environment/Terrain/Fracture_Platforms_v1.png",
                        Columns = 3, Rows = 4, Pivot = Bottom, Prefix = "FracturePlatform" },

            // props(): 12칸, i%4 / i//4 → 4열 x 3행
            new Sheet { Path = "Environment/Props/Fracture_EnvironmentProps_v1.png",
                        Columns = 4, Rows = 3, Pivot = Bottom, Prefix = "FractureProp" },

            // hazard_atlas(): 3행 x 4열
            new Sheet { Path = "Environment/Hazards/Fracture_FutureHazards_v1.png",
                        Columns = 4, Rows = 3, Pivot = Center, Prefix = "FractureHazard" },

            // foresight_objects(): 12칸, 4열 x 3행
            new Sheet { Path = "Environment/Interactables/Fracture_ForesightObjects_v1.png",
                        Columns = 4, Rows = 3, Pivot = Bottom, Prefix = "FractureForesight" },

            // doors(): 8칸, 4열 x 2행. 숏컷 겉모습(닫힘·열림)이 여기서 나온다.
            new Sheet { Path = "Environment/Interactables/Fracture_DoorsShortcuts_v1.png",
                        Columns = 4, Rows = 2, Pivot = Bottom, Prefix = "FractureDoor" },

            // transit_structures(): 8칸, 4열 x 2행
            new Sheet { Path = "Environment/Interactables/Fracture_TransitStructures_v1.png",
                        Columns = 4, Rows = 2, Pivot = Bottom, Prefix = "FractureTransit" },

            // ambient_static(): 36칸, i%9 / i//9 → 9열 x 4행
            new Sheet { Path = "Environment/VFX/Fracture_AmbientVFX_v1.png",
                        Columns = 9, Rows = 4, Pivot = Center, Prefix = "FractureAmbientVFX" },

            // secondary_vfx(): 24칸, i%6 / i//6 → 6열 x 4행.
            // 응시의 같은 이름 시트는 행=클립이었지만 균열 쪽은 칸마다 다른 정지 아이콘이라
            // (kind=i%4가 열 방향으로 돌아간다) 애니메이션으로 묶을 수 없다.
            new Sheet { Path = "Gameplay/VFX/FractureSecondaryVFX_v1.png",
                        Columns = 6, Rows = 4, Pivot = Center, Prefix = "FractureSecondaryVFX" },

            // ui_icons(): 25칸, i%5 / i//5 → 5열 x 5행
            new Sheet { Path = "UI/FractureUIIcons_v1.png",
                        Columns = 5, Rows = 5, Pivot = Center, Prefix = "FractureUIIcon" },
        };

        [MenuItem("Hidden Weight/Art/Slice Fracture Environment Sheets")]
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
                    Debug.LogWarning($"[FractureEnvironmentArtSlicer] 시트를 찾지 못했다: {path}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = Mathf.Max(2048, Mathf.Max(texture.width, texture.height));

                // 주의: 여기서 읽는 texture.width는 **임포트된 뒤**의 크기다. 2의 거듭제곱이
                // 아닌 시트를 넣으면 유니티가 늘려 들여오고(1536x704 → 2048x512) 그 크기로
                // 자른 좌표가 실제 텍스처 밖으로 나가, 서브에셋이 조용히 사라진다(v2 첫
                // 임포트에서 24칸 중 16칸만 생겼다). 시트는 2의 거듭제곱 크기로 만들 것.
                importer.spritesheet = BuildRects(texture.width, texture.height, sheet);
                importer.SaveAndReimport();

                // .meta에는 이름이 들어가는데 서브에셋이 갱신되지 않는 경우가 있다
                // (응시의 InformingMouth가 그랬다). 강제 재임포트로 확실히 반영시킨다.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sliced++;
            }

            // 같은 세션에서 곧바로 이름으로 조회하는 호출자(ZoneSceneBuilder.RunFractureZone)가
            // 있으므로 동기 임포트로 끝낸다. 비동기로 두면 방금 자른 서브에셋이 아직 보이지 않아
            // 씬이 통째로 플레이스홀더로 지어진다 — 실제로 한 번 그렇게 됐다.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[FractureEnvironmentArtSlicer] 시트 {sliced}개 분할 완료");
        }

        static SpriteMetaData[] BuildRects(int width, int height, Sheet sheet)
        {
            int cellWidth = width / sheet.Columns;
            int cellHeight = height / sheet.Rows;
            var sprites = new List<SpriteMetaData>(sheet.Columns * sheet.Rows);

            for (int row = 0; row < sheet.Rows; row++)
            {
                // 문서는 맨 위를 1행으로 적고 Unity 텍스처 좌표는 아래가 0이라 여기서 뒤집는다.
                int y = height - (row + 1) * cellHeight;
                for (int column = 0; column < sheet.Columns; column++)
                {
                    sprites.Add(new SpriteMetaData
                    {
                        name = $"{sheet.Prefix}_r{row + 1}_c{column + 1}",
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
