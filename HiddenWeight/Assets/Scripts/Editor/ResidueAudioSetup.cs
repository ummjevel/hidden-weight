using UnityEditor;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.EditorTools
{
    // 배경음 임포트 설정과 ZoneData 연결. 파일을 넣어 두는 것만으로는 재생되지 않는다 —
    // ZoneData.bgm에 물려야 GameManager가 지역 진입 시 틀어 준다.
    public static class ResidueAudioSetup
    {
        const string AudioFolder = "Assets/Audio";
        const string DataFolder = "Assets/ScriptableObjects";

        [MenuItem("Hidden Weight/Audio/Configure And Link BGM")]
        public static void Run()
        {
            // BGM은 길고 한 번에 하나만 재생되므로 스트리밍이 맞다. 통째로 메모리에 올리면
            // 2~3MB짜리가 압축 해제되며 수십 MB가 된다.
            ConfigureBgm($"{AudioFolder}/Residue_BGM.mp3");

            LinkBgm("Zone_Residue", $"{AudioFolder}/Residue_BGM.mp3");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void ConfigureBgm(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as UnityEditor.AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[AudioImporter] 오디오를 찾지 못했다: {path}");
                return;
            }

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();

            Debug.Log($"[AudioImporter] {path} 스트리밍으로 설정");
        }

        static void LinkBgm(string zoneAssetName, string clipPath)
        {
            var zone = AssetDatabase.LoadAssetAtPath<ZoneData>($"{DataFolder}/{zoneAssetName}.asset");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

            if (zone == null || clip == null)
            {
                Debug.LogWarning($"[AudioImporter] 연결 실패: zone={zone}, clip={clip}");
                return;
            }

            var so = new SerializedObject(zone);
            so.FindProperty("bgm").objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);

            Debug.Log($"[AudioImporter] {zoneAssetName}.bgm ← {clip.name} ({clip.length:F0}초)");
        }
    }
}
