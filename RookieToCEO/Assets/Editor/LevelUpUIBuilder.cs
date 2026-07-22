using RookieToCEO.Gameplay;
using RookieToCEO.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RookieToCEO.EditorTools
{
    // GDD 4번 "레벨업 강화 3개 중 하나 선택" UI를 Day 씬에 배치한다.
    // Canvas/EventSystem/패널/버튼 3개를 코드로 만들고 LevelUpChoiceUI에 연결한다.
    // 사용법: Unity -batchmode -projectPath <경로> -executeMethod RookieToCEO.EditorTools.LevelUpUIBuilder.BuildDayLevelUpUI -quit
    public static class LevelUpUIBuilder
    {
        private const string DayScenePath = "Assets/Scenes/Day.unity";

        public static void BuildDayLevelUpUI()
        {
            var scene = EditorSceneManager.OpenScene(DayScenePath, OpenSceneMode.Single);

            DestroyIfExists("LevelUpCanvas");

            EnsureEventSystem();
            var canvasGo = BuildCanvas();
            var panel = BuildPanel(canvasGo.transform);
            var buttons = BuildChoiceButtons(panel.transform, out var labels);

            var dayWaveManager = Object.FindObjectOfType<DayWaveManager>();
            if (dayWaveManager == null)
            {
                Debug.LogError("[LevelUpUIBuilder] DayWaveManager를 찾을 수 없음 - 먼저 PrefabAndSceneBuilder.BuildDayScene을 실행해야 함");
                return;
            }

            var ui = canvasGo.AddComponent<LevelUpChoiceUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("dayWaveManager").objectReferenceValue = dayWaveManager;
            so.FindProperty("panelRoot").objectReferenceValue = panel;

            var buttonsProp = so.FindProperty("choiceButtons");
            buttonsProp.arraySize = buttons.Length;
            for (var i = 0; i < buttons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }

            var labelsProp = so.FindProperty("choiceLabels");
            labelsProp.arraySize = labels.Length;
            for (var i = 0; i < labels.Length; i++)
            {
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false); // 평소엔 숨겨져 있다가 레벨업 시점에만 LevelUpChoiceUI가 켠다.

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[LevelUpUIBuilder] Day 씬에 레벨업 UI 배치 완료");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

            // 새 Input System만 활성화돼 있으므로(ProjectSettings activeInputHandler=1)
            // 레거시 StandaloneInputModule 대신 InputSystemUIInputModule을 붙여야 버튼 클릭이 동작한다.
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject BuildCanvas()
        {
            var go = new GameObject("LevelUpCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            go.AddComponent<GraphicRaycaster>();

            return go;
        }

        private static GameObject BuildPanel(Transform parent)
        {
            var go = new GameObject("LevelUpPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760, 220);
            rect.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);

            return go;
        }

        private static Button[] BuildChoiceButtons(Transform parent, out Text[] labels)
        {
            const int count = 3;
            var buttons = new Button[count];
            labels = new Text[count];

            for (var i = 0; i < count; i++)
            {
                var buttonGo = new GameObject($"ChoiceButton{i}", typeof(RectTransform));
                buttonGo.transform.SetParent(parent, false);

                var rect = buttonGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(220, 170);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2((i - 1) * 240, 0);

                var image = buttonGo.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                buttons[i] = buttonGo.AddComponent<Button>();

                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(buttonGo.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                var text = textGo.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.fontSize = 24;
                labels[i] = text;
            }

            return buttons;
        }

        private static void DestroyIfExists(string name)
        {
            GameObject found;
            while ((found = GameObject.Find(name)) != null)
            {
                Object.DestroyImmediate(found);
            }
        }
    }
}
