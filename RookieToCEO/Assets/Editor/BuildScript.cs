using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // M9 통합 검증: 배치모드 빌드가 에러 없이 완료되는지 확인하기 위한 스크립트.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.BuildScript.BuildMac
    public static class BuildScript
    {
        public static void BuildMac()
        {
            // Bootstrap이 진입점(GameFlowManager가 여기서 지속되는 Player를 만들고 Day를 로드)이라
            // 0번 씬으로 둔다. EditorBuildSettings.scenes 순서와 동일하게 맞춘다.
            var scenes = new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Day.unity",
                "Assets/Scenes/Night.unity",
                "Assets/Scenes/Boss.unity",
                "Assets/Scenes/Ending.unity",
            };

            var report = BuildPipeline.BuildPlayer(scenes, "Builds/macOS/RookieToCEO.app", BuildTarget.StandaloneOSX, BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] 빌드 실패: {report.summary.result}, 에러 {report.summary.totalErrors}개");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[BuildScript] 빌드 성공: {report.summary.outputPath}, 크기 {report.summary.totalSize} bytes");
            EditorApplication.Exit(0);
        }
    }
}
