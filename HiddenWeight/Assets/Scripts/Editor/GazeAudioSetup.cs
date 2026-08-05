using UnityEditor;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.EditorTools
{
    // 응시 배경음 임포트 설정과 ZoneData 연결. ResidueAudioSetup과 같은 이유·같은 규칙이다 —
    // 파일을 Assets/Audio에 넣어 두는 것만으로는 재생되지 않는다.
    public static class GazeAudioSetup
    {
        const string AudioFolder = "Assets/Audio";
        const string DataFolder = "Assets/ScriptableObjects";

        [MenuItem("Hidden Weight/Audio/Configure And Link Gaze BGM")]
        public static void Run()
        {
            // BGM은 길고 한 번에 하나만 재생되므로 스트리밍이 맞다. 통째로 메모리에 올리면
            // 원본 WAV(수십 MB)가 압축 해제된 채로 그대로 남는다.
            ConfigureBgm($"{AudioFolder}/Gaze_BGM.wav");

            LinkBgm("Zone_Gaze", $"{AudioFolder}/Gaze_BGM.wav");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void ConfigureBgm(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[GazeAudioSetup] 오디오를 찾지 못했다: {path}");
                return;
            }

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();

            Debug.Log($"[GazeAudioSetup] {path} 스트리밍으로 설정");
        }

        static void LinkBgm(string zoneAssetName, string clipPath)
        {
            var zone = AssetDatabase.LoadAssetAtPath<ZoneData>($"{DataFolder}/{zoneAssetName}.asset");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

            if (zone == null || clip == null)
            {
                Debug.LogWarning($"[GazeAudioSetup] 연결 실패: zone={zone}, clip={clip}");
                return;
            }

            var so = new SerializedObject(zone);
            so.FindProperty("bgm").objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);

            Debug.Log($"[GazeAudioSetup] Zone_Gaze.bgm ← {clip.name} ({clip.length:F0}초)");
        }
    }
}
