using UnityEngine.SceneManagement;

namespace HiddenWeight.Core
{
    // 씬 이름 상수와 씬 전환 진입점을 한곳에 모은다.
    public static class SceneFlow
    {
        public const string Bootstrap = "Bootstrap";
        public const string Title = "Title";
        public const string Prologue = "Zone_Prologue";
        public const string Residue = "Zone_Residue";
        public const string Gaze = "Zone_Gaze";
        public const string Fracture = "Zone_Fracture";
        public const string Ending = "Ending";

        // UI(ScreenFader)가 채우는 훅. Core는 UI 모듈을 참조하지 않는다.
        public static System.Action<string, float> FadeLoader;

        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);

        public static void LoadWithFade(string sceneName, float fadeSeconds = 0.5f)
        {
            if (FadeLoader != null) FadeLoader(sceneName, fadeSeconds);
            else Load(sceneName);
        }
    }
}
