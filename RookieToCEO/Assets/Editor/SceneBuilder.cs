using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RookieToCEO.EditorTools
{
    // M2 자동화 파이프라인 골격: 씬 구성을 GUI 드래그 없이 코드로 만들기 위한 진입점.
    // 지금은 빈 씬 3개(Day/Night/Boss)만 만들어 두고, M3~M8에서 필요한 컴포넌트가 생기는 대로
    // 각 Build*Scene 메서드 안에서 플레이어/스포너/UI 등을 코드로 배치해 나간다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.SceneBuilder.BuildAllScenes -quit
    public static class SceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";

        public static void BuildAllScenes()
        {
            BuildDayScene();
            BuildNightScene();
            BuildBossScene();
        }

        // 낮 디펜스 씬(1~4층 공통 틀, 60초 웨이브). 실제 스포너/UI 배치는 M6에서 채운다.
        public static void BuildDayScene()
        {
            CreateEmptySceneIfMissing("Day");
        }

        // 밤 잠입 씬(1~3층 공통 틀, 60초 제한). 실제 경비/CCTV 배치는 M7에서 채운다.
        public static void BuildNightScene()
        {
            CreateEmptySceneIfMissing("Night");
        }

        // 4층 CEO 최종 웨이브 씬. 실제 보스 패턴 배치는 M8에서 채운다.
        public static void BuildBossScene()
        {
            CreateEmptySceneIfMissing("Boss");
        }

        private static void CreateEmptySceneIfMissing(string sceneName)
        {
            var path = $"{ScenesFolder}/{sceneName}.unity";
            if (File.Exists(path))
            {
                Debug.Log($"[SceneBuilder] 이미 존재해서 건너뜀: {path}");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneBuilder] 생성: {path}");
        }
    }
}
