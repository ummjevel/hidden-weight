using UnityEngine;
using UnityEngine.EventSystems;

namespace HiddenWeight.UI
{
    // 포커스 위치를 색뿐 아니라 작은 크기 변화로도 알려 주되, 동작 줄이기에서는 즉시 전환한다.
    public class UIFocusPulse : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        Vector3 _target = Vector3.one;

        public void OnSelect(BaseEventData _) => SetFocused(true);
        public void OnDeselect(BaseEventData _) => SetFocused(false);
        public void OnPointerEnter(PointerEventData _) => SetFocused(true);
        public void OnPointerExit(PointerEventData _) => SetFocused(false);

        void SetFocused(bool focused)
        {
            _target = focused ? Vector3.one * 1.035f : Vector3.one;
            if (UISettings.ReduceMotion) transform.localScale = _target;
        }

        void Update()
        {
            if (UISettings.ReduceMotion) return;
            transform.localScale = Vector3.Lerp(transform.localScale, _target, 16f * Time.unscaledDeltaTime);
        }

        void OnDisable()
        {
            _target = Vector3.one;
            transform.localScale = Vector3.one;
        }
    }
}
