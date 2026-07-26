using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 배치모드 검증용. 컴파일이 깨지면 -executeMethod 자체가 실행되지 않으므로,
    // 이 메서드가 돌아서 exit 0을 남겼다는 것이 곧 "컴파일 통과"의 증거다.
    public static class BuildScript
    {
        public static void Compile()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("[BuildScript] 스크립트 컴파일 실패");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[BuildScript] 컴파일 통과");
            EditorApplication.Exit(0);
        }

        public static void BuildMac()
        {
            var scenes = new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/Zone_Prologue.unity",
                "Assets/Scenes/Zone_Residue.unity",
                "Assets/Scenes/Zone_Gaze.unity",
                "Assets/Scenes/Zone_Fracture.unity",
                "Assets/Scenes/Ending.unity",
            };

            var report = BuildPipeline.BuildPlayer(
                scenes, "Builds/macOS/HiddenWeight.app", BuildTarget.StandaloneOSX, BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] 빌드 실패: {report.summary.result}, 에러 {report.summary.totalErrors}개");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[BuildScript] 빌드 성공: {report.summary.outputPath}");
            EditorApplication.Exit(0);
        }
    }
}
