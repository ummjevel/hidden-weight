using System.IO;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // docs의 승인된 룸 콘셉트를 런타임용 단일 배경 스프라이트로 가져온다.
    // 원본은 기획 기록으로 남기고 Assets 아래 복사본만 Unity 임포트 설정을 적용한다.
    public static class FractureConceptArtImporter
    {
        const string Destination = "Assets/Art/Fracture/Rooms";
        static readonly (string room, string file)[] Rooms =
        {
            ("FractureRoom01", "01-glass-garden.png"),
            ("FractureRoom02", "02-misaligned-promenade.png"),
            ("FractureRoom03", "03-possibility-plaza.png"),
            ("FractureRoom04", "04-swaying-lower-garden.png"),
            ("FractureRoom05", "05-foresight-sanctuary.png"),
            ("FractureRoom06", "06-time-lag-greenhouse.png"),
            ("FractureRoom07", "07-floating-architecture.png"),
            ("FractureRoom08", "08-reversing-elevator-shaft.png"),
            ("FractureRoom09", "09-mirrored-possibility-hall.png"),
            ("FractureRoom10", "10-second-hand-watchtower.png"),
            ("FractureRoom11", "11-not-yet-ruins.png"),
            ("FractureRoom12", "12-tomorrows-fracture.png"),
            ("FractureSecret01", "S1-abandoned-possibility.png"),
            ("FractureSecret02", "S2-still-afternoon.png"),
            ("FractureSecret03", "S3-unselected-door.png"),
        };

        [MenuItem("Hidden Weight/Art/Import Fracture Concept Backgrounds")]
        public static void Run()
        {
            EnsureFolder("Assets/Art", "Fracture");
            EnsureFolder("Assets/Art/Fracture", "Rooms");
            string sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../docs/concept-art/generated/fracture-map-v1/rooms"));

            foreach (var entry in Rooms)
            {
                string source = Path.Combine(sourceRoot, entry.file);
                string assetPath = Destination + "/" + entry.room + ".png";
                string destination = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
                if (!File.Exists(source)) throw new FileNotFoundException("Fracture 콘셉트 원본이 없습니다.", source);
                File.Copy(source, destination, true);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var entry in Rooms)
            {
                string assetPath = Destination + "/" + entry.room + ".png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                // 전체 지역이 한 씬에 들어가므로 15장을 2048로 유지하면 배경만으로 메모리
                // 예산을 크게 넘는다. 카메라 줌에서 충분한 1024를 평면 합성 기준으로 고정한다.
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                standalone.overridden = true;
                standalone.maxTextureSize = 1024;
                standalone.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SetPlatformTextureSettings(standalone);
                importer.SaveAndReimport();
            }
            Debug.Log("[FractureConceptArtImporter] 룸 배경 15장 임포트 완료");
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
