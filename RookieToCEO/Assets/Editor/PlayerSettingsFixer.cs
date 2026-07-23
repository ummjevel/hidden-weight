using UnityEditor;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // 개발 중 디버깅을 쉽게 하기 위해 창모드(테두리 있는 윈도우)로 강제 전환한다.
    // 기본값(borderless fullscreen)은 화면에 아무것도 안 보이는 문제를 진단할 때
    // 창이 실제로 열려 있는지조차 구분하기 어려워서 바꿨다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.PlayerSettingsFixer.UseWindowedMode -quit
    public static class PlayerSettingsFixer
    {
        public static void UseWindowedMode()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.visibleInBackground = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerSettingsFixer] 창모드(1280x720)로 전환 완료");
        }
    }
}
