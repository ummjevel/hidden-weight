using System.IO;
using RookieToCEO.Core;
using UnityEditor;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // M9: docs/DEVELOPMENT_PLAN.md에서 확정한 밸런스 표를 실제 BalanceData.asset으로 만든다.
    // 다른 Editor 자동화 스크립트(PackageSetup, SceneBuilder)와 같은 패턴으로, GUI에서
    // "Create > RookieToCEO > Balance Data"를 눌러 만드는 대신 배치모드로 무인 생성한다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.BalanceDataBuilder.CreateDefaultAsset -quit
    public static class BalanceDataBuilder
    {
        private const string AssetPath = "Assets/ScriptableObjects/BalanceData.asset";

        public static void CreateDefaultAsset()
        {
            if (File.Exists(AssetPath))
            {
                Debug.Log($"[BalanceDataBuilder] 이미 존재해서 건너뜀: {AssetPath}");
                return;
            }

            // BalanceData의 필드 기본값(클래스 선언부)이 곧 docs/DEVELOPMENT_PLAN.md에서 확정한
            // 수치이므로, 인스턴스를 만들기만 하면 그 값 그대로 애셋이 된다.
            var asset = ScriptableObject.CreateInstance<BalanceData>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BalanceDataBuilder] 생성: {AssetPath}");
        }
    }
}
