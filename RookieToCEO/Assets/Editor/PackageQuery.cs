using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // 아직 manifest.json에 없는 패키지들의 "현재 에디터와 호환되는 최신 버전"을 조회만 하는 스크립트.
    // Client.Search는 어셈블리를 바꾸지 않으므로(=도메인 리로드가 없으므로) PackageSetup.cs에서
    // 겪었던 "리로드 때문에 콜백 구독이 끊기는" 문제 없이 안전하게 순차 실행할 수 있다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.PackageQuery.Run
    public static class PackageQuery
    {
        private static readonly Queue<string> Targets = new Queue<string>(new[]
        {
            "com.unity.test-framework",
            "com.unity.2d.sprite",
            "com.unity.2d.tilemap",
        });

        private static SearchRequest _request;

        public static void Run()
        {
            Next();
        }

        private static void Next()
        {
            if (Targets.Count == 0)
            {
                Debug.Log("[PackageQuery] 조회 완료");
                EditorApplication.Exit(0);
                return;
            }

            var id = Targets.Dequeue();
            _request = Client.Search(id);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success && _request.Result.Length > 0)
            {
                var info = _request.Result[0];
                Debug.Log($"[PackageQuery] RESOLVED {info.name}={info.version}");
            }
            else
            {
                Debug.LogError($"[PackageQuery] 조회 실패: {_request.Error?.message}");
            }

            Next();
        }
    }
}
