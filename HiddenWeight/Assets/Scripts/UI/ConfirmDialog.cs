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
            Surface();
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
            Surface();
            UIBuilder.Select(_confirmButton);
        }

        // 이 모달은 일시정지 메뉴와 **같은 부모** 아래에 있고, 섹션 패널(지도·설정…)이
        // 나중에 만들어져 형제 순서상 뒤에 온다. 그래서 그냥 켜면 지도 아래에 깔려
        // 글자만 비쳐 보였다 — 되돌릴 수 없는 선택을 묻는 화면이 읽히지 않았다.
        // 띄우는 순간 맨 앞으로 올린다.
        void Surface()
        {
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
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
            blocker.color = new Color(0.008f, 0.010f, 0.020f, 0.88f);

            var panelImage = UIBuilder.CreateMenuPanel(_root.transform, "DialogPanel", UIBuilder.MenuGlass);
            var panel = panelImage.gameObject;
            var panelRt = panelImage.rectTransform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(720f, 380f);
            panelRt.anchoredPosition = Vector2.zero;

            var caption = UIBuilder.CreateMenuText(panel.transform, "DialogCaption", "MEMORY DECISION", 14,
                TextAnchor.MiddleCenter, true);
            caption.color = new Color(UIBuilder.MenuEdge.r, UIBuilder.MenuEdge.g, UIBuilder.MenuEdge.b, 0.80f);
            var captionRt = caption.rectTransform;
            captionRt.anchorMin = captionRt.anchorMax = new Vector2(0.5f, 1f);
            captionRt.sizeDelta = new Vector2(500f, 28f);
            captionRt.anchoredPosition = new Vector2(0f, -32f);

            _title = UIBuilder.CreateText(panel.transform, "DialogTitle", 36, TextAnchor.MiddleCenter);
            UIBuilder.StyleMenuText(_title, true);
            var titleRt = _title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(600f, 64f);
            titleRt.anchoredPosition = new Vector2(0f, -62f);

            UIBuilder.AddDivider(panel.transform, new Vector2(0.5f, 1f), new Vector2(500f, 2f),
                new Vector2(0f, -130f));

            _message = UIBuilder.CreateText(panel.transform, "DialogMessage", 26, TextAnchor.MiddleCenter);
            UIBuilder.StyleMenuText(_message);
            _message.color = UIBuilder.MenuTextMuted;
            _message.horizontalOverflow = HorizontalWrapMode.Wrap;
            _message.verticalOverflow = VerticalWrapMode.Overflow;
            var messageRt = _message.rectTransform;
            messageRt.anchorMin = messageRt.anchorMax = new Vector2(0.5f, 0.5f);
            messageRt.sizeDelta = new Vector2(580f, 120f);
            messageRt.anchoredPosition = new Vector2(0f, 4f);

            _cancelButton = UIBuilder.CreateButton(panel.transform, "취소", -126f, Cancel);
            var cancelRt = _cancelButton.GetComponent<RectTransform>();
            cancelRt.anchoredPosition = new Vector2(-125f, -126f);
            _cancelLabel = _cancelButton.GetComponentInChildren<Text>();

            _confirmButton = UIBuilder.CreateButton(panel.transform, "확인", -126f, Confirm);
            var confirmRt = _confirmButton.GetComponent<RectTransform>();
            confirmRt.anchoredPosition = new Vector2(125f, -126f);
            _confirmLabel = _confirmButton.GetComponentInChildren<Text>();

            _root.SetActive(false);
        }
    }
}
