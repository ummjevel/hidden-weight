using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HiddenWeight.UI
{
    // 진행도 초기화·체크포인트 복귀처럼 되돌리기 어려운 행동을 한 번 더 확인하는 공통 모달.
    // 위험한 행동의 기본 포커스는 항상 취소다(UI_UX_DESIGN 10.1·10.2절).
    public class ConfirmDialog : MonoBehaviour
    {
        GameObject _root;
        Text _title;
        Text _message;
        Button _confirmButton;
        Button _cancelButton;
        Text _confirmLabel;
        Text _cancelLabel;

        UnityAction _onConfirm;
        Selectable _returnSelection;

        public bool IsVisible => _root != null && _root.activeSelf;

        void Awake() => BuildHierarchy();

        public void ShowConfirm(
            string title,
            string message,
            string confirmLabel,
            UnityAction onConfirm,
            Selectable returnSelection)
        {
            _title.text = title;
            _message.text = message;
            _confirmLabel.text = confirmLabel;
            _cancelLabel.text = "취소";
            _onConfirm = onConfirm;
            _returnSelection = returnSelection;
            _cancelButton.gameObject.SetActive(true);
            _root.SetActive(true);
            UIBuilder.Select(_cancelButton);
        }

        public void ShowInfo(string title, string message, Selectable returnSelection)
        {
            _title.text = title;
            _message.text = message;
            _confirmLabel.text = "닫기";
            _onConfirm = null;
            _returnSelection = returnSelection;
            _cancelButton.gameObject.SetActive(false);
            _root.SetActive(true);
            UIBuilder.Select(_confirmButton);
        }

        public void Confirm()
        {
            var action = _onConfirm;
            Hide(false);
            action?.Invoke();
        }

        public void Cancel() => Hide(true);

        public void Hide(bool restoreFocus = true)
        {
            if (_root == null) return;
            _root.SetActive(false);
            _onConfirm = null;
            if (restoreFocus) UIBuilder.Select(_returnSelection);
        }

        void BuildHierarchy()
        {
            _root = new GameObject("ConfirmDialogRoot", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var rootRt = (RectTransform)_root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var blocker = _root.AddComponent<Image>();
            blocker.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);

            var panel = new GameObject("DialogPanel", typeof(RectTransform));
            panel.transform.SetParent(_root.transform, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(680f, 340f);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.055f, 0.065f, 0.08f, 0.98f);

            _title = UIBuilder.CreateText(panel.transform, "DialogTitle", 36, TextAnchor.MiddleCenter);
            var titleRt = _title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(600f, 64f);
            titleRt.anchoredPosition = new Vector2(0f, -36f);

            _message = UIBuilder.CreateText(panel.transform, "DialogMessage", 26, TextAnchor.MiddleCenter);
            _message.horizontalOverflow = HorizontalWrapMode.Wrap;
            _message.verticalOverflow = VerticalWrapMode.Overflow;
            var messageRt = _message.rectTransform;
            messageRt.anchorMin = messageRt.anchorMax = new Vector2(0.5f, 0.5f);
            messageRt.sizeDelta = new Vector2(580f, 120f);
            messageRt.anchoredPosition = new Vector2(0f, 28f);

            _cancelButton = UIBuilder.CreateButton(panel.transform, "취소", -105f, Cancel);
            var cancelRt = _cancelButton.GetComponent<RectTransform>();
            cancelRt.anchoredPosition = new Vector2(-125f, -105f);
            _cancelLabel = _cancelButton.GetComponentInChildren<Text>();

            _confirmButton = UIBuilder.CreateButton(panel.transform, "확인", -105f, Confirm);
            var confirmRt = _confirmButton.GetComponent<RectTransform>();
            confirmRt.anchoredPosition = new Vector2(125f, -105f);
            _confirmLabel = _confirmButton.GetComponentInChildren<Text>();

            _root.SetActive(false);
        }
    }
}
