using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // M2 자동화 파이프라인: 필요한 Unity 패키지를 GUI(Package Manager 창) 없이 배치모드에서 설치한다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.PackageSetup.InstallRequiredPackages
    //
    // 주의(중요한 함정): 패키지를 하나씩 Client.Add로 설치하면서 매번 완료를 기다리는 방식은
    // 실제로 시도했을 때 두 번째 패키지에서 무한 대기에 빠졌다. 원인은 패키지 설치로 어셈블리가
    // 바뀌면 Unity가 "도메인 리로드"를 하는데, 이때 정적 필드와 EditorApplication.update
    // 구독이 전부 초기화되기 때문이다. 그래서 이 스크립트는 두 가지로 그 문제를 피한다.
    // 1) Client.AddAndRemove로 패키지 전체를 한 번의 요청으로 묶어서 설치한다.
    // 2) [InitializeOnLoad] 정적 생성자 + SessionState로, 도메인 리로드가 나더라도
    //    "아직 설치 진행 중"이라는 사실을 기억해 두었다가 다시 폴링을 구독한다.
    [InitializeOnLoad]
    public static class PackageSetup
    {
        private const string RunningKey = "RookieToCEO_PackageSetup_Running";

        private static readonly string[] RequiredPackages =
        {
            "com.unity.render-pipelines.universal", // URP 2D 렌더러 (탑뷰 스프라이트용)
            "com.unity.inputsystem",                 // WASD 이동 + Space/R/E/Shift 고정 바인딩
            "com.unity.test-framework",               // EditMode 배치모드 검증
            "com.unity.2d.sprite",                    // 도트 스프라이트 임포트
            "com.unity.2d.tilemap",                   // 층별 바닥 타일 구성
        };

        private static AddAndRemoveRequest _request;

        // 도메인 리로드 직후에도 이 정적 생성자가 다시 호출되므로, 설치가 아직 안 끝났다면
        // 폴링을 재구독해서 이어간다.
        static PackageSetup()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Debug.Log("[PackageSetup] 도메인 리로드 이후 재개, 완료 여부 재확인");
                EditorApplication.update += Poll;
            }
        }

        public static void InstallRequiredPackages()
        {
            Debug.Log("[PackageSetup] 일괄 설치 요청: " + string.Join(", ", RequiredPackages));
            SessionState.SetBool(RunningKey, true);
            _request = Client.AddAndRemove(RequiredPackages);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            // 요청 도중 도메인 리로드가 나면 _request 참조 자체가 날아간다. 리로드가 일어났다는 것은
            // 이미 매니페스트 반영과 임포트가 끝났다는 뜻이므로 이 경우엔 성공으로 간주하고 종료한다.
            if (_request == null)
            {
                Finish(true);
                return;
            }

            if (!_request.IsCompleted) return;

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log("[PackageSetup] 일괄 설치 성공");
                Finish(true);
            }
            else
            {
                Debug.LogError("[PackageSetup] 설치 실패: " + _request.Error?.message);
                Finish(false);
            }
        }

        private static void Finish(bool success)
        {
            EditorApplication.update -= Poll;
            SessionState.SetBool(RunningKey, false);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
