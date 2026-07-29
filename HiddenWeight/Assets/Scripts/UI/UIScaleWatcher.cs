using UnityEngine;
using UnityEngine.UI;

namespace HiddenWeight.UI
{
    // 실행 중 UI 배율 변경을 이미 열린 모든 런타임 캔버스에 즉시 반영한다.
    public class UIScaleWatcher : MonoBehaviour
    {
        CanvasScaler _scaler;

        void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void OnEnable() { UISettings.Changed += Apply; Apply(); }
        void OnDisable() => UISettings.Changed -= Apply;

        void Apply()
        {
            if (_scaler != null) _scaler.referenceResolution = UIBuilder.ReferenceResolution / UISettings.UiScale;
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                var colors = button.colors;
                colors.selectedColor = UISettings.HighContrast
                    ? new Color(0.1f, 0.75f, 0.82f, 0.95f)
                    : new Color(0.72f, 0.9f, 0.92f, 0.32f);
                button.colors = colors;
            }
        }
    }
}
