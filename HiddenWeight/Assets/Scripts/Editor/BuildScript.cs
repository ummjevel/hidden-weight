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
            // 씬 목록은 EditorBuildSettings 하나만 진실로 삼는다. 여기에 따로 적어 두면
            // ZoneSceneBuilder가 새 씬을 등록해도 빌드에는 안 들어가서, 게임 안에서 그 씬으로
            // 넘어가려 할 때 "has not been added to the active build profile"로 조용히 실패한다
            // (실제로 잔재 신규 지역이 그렇게 빠졌다).
            var scenes = System.Array.ConvertAll(
                System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled),
                s => s.path);

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] 빌드 설정에 씬이 하나도 없다. ZoneSceneBuilder를 먼저 실행할 것.");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[BuildScript] 빌드에 포함할 씬 {scenes.Length}개: {string.Join(", ", scenes)}");

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
