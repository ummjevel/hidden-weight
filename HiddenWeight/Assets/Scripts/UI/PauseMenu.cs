using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // PlayerInput.PausePressed(Enabled 여부와 무관하게 항상 동작)로 토글되는 일시정지 화면.
    // 열릴 때 GameManager.SetState(Paused) + PlayerInput.Enabled = false,
    // 닫힐 때 Playing + PlayerInput.Enabled = true.
    public class PauseMenu : MonoBehaviour
    {
        const float FadeDuration = 0.18f;

        GameObject _root;
        CanvasGroup _rootGroup;
        Coroutine _fadeRoutine;
        Button _resumeButton;
        Button _checkpointButton;
        Button _titleButton;
        ConfirmDialog _dialog;
        PauseSectionPanel _sections;

        public bool IsOpen => _root != null && _root.activeSelf;
        public bool IsConfirming => _dialog != null && _dialog.IsVisible;
        public PauseSection? CurrentSection => _sections != null && _sections.IsVisible ? _sections.CurrentSection : (PauseSection?)null;

        void Awake()
        {
            BuildHierarchy();
        }

        void Start()
        {
            // 이미 Paused 상태로 씬이 시작하는 경우는 없지만, 방어적으로 현재 상태를 반영한다.
            bool paused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;
            _root.SetActive(paused);
            _rootGroup.alpha = paused ? 1f : 0f;
        }

        void Update()
        {
            if (PlayerInput.MapPressed && GameManager.Instance != null)
            {
                // 지도 키로 들어오고 나갈 때는 일시정지음 대신 지도음을 낸다. 같은 키가
                // 여는 문이 다르면 소리도 달라야 어디로 들어왔는지 헷갈리지 않는다.
                if (GameManager.Instance.State == GameState.Playing)
                {
                    Open(openCue: SfxCue.UiMapOpen);
                    _sections.Show(PauseSection.Map);
                }
                else if (GameManager.Instance.State == GameState.Paused && _sections.IsVisible
                    && _sections.CurrentSection == PauseSection.Map)
                {
                    Close(SfxCue.UiMapClose);
                }
                else if (GameManager.Instance.State == GameState.Paused)
                {
                    AudioManager.Instance?.PlaySfx(SfxCue.UiMapOpen, 0.4f);
                    _sections.Show(PauseSection.Map);
                }
                return;
            }

            if (!PlayerInput.PausePressed) return;

            if (_dialog != null && _dialog.IsVisible)
            {
                AudioManager.Instance?.PlaySfx(SfxCue.UiCancel, 0.4f);
                _dialog.Cancel();
                return;
            }

            if (_sections != null && _sections.IsVisible)
            {
                AudioManager.Instance?.PlaySfx(SfxCue.UiCancel, 0.4f);
                _sections.Hide();
                UIBuilder.Select(_resumeButton);
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.State == GameState.Playing) Open();
            else if (gm.State == GameState.Paused) Close();
        }

        public void Open(bool selectResume = true, SfxCue openCue = SfxCue.UiPause)
        {
            // 일시정지는 timeScale을 0으로 만들어 화면이 얼어붙는다. 페이드가 끝나기 전까지
            // 입력이 먹혔는지 알 수 없으므로 여는 순간 바로 소리로 답한다.
            AudioManager.Instance?.PlaySfx(openCue, 0.4f);
            PlayerInput.Enabled = false;
            GameManager.Instance.SetState(GameState.Paused);

            // GameManager.SetState가 Time.timeScale을 0으로 만들므로 페이드는 반드시
            // unscaledDeltaTime 기준으로 돌아야 한다(FragmentLog.Fade와 같은 이유).
            _root.SetActive(true);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(1f));
            if (selectResume) StartCoroutine(SelectResumeNextFrame());
        }

        public void OpenSection(PauseSection section)
        {
            if (!IsOpen) Open(false);
            _sections.Show(section);
        }

        IEnumerator SelectResumeNextFrame()
        {
            yield return null;
            UIBuilder.Select(_resumeButton);
        }

        public void Close(SfxCue closeCue = SfxCue.UiUnpause)
        {
            AudioManager.Instance?.PlaySfx(closeCue, 0.4f);
            if (_dialog != null) _dialog.Hide(false);
            if (_sections != null) _sections.Hide();
            PlayerInput.Enabled = true;
            GameManager.Instance.SetState(GameState.Playing);

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeTo(0f));
        }

        IEnumerator FadeTo(float target)
        {
            if (UISettings.ReduceMotion)
            {
                _rootGroup.alpha = target;
                if (target <= 0f) _root.SetActive(false);
                _fadeRoutine = null;
                yield break;
            }
            float start = _rootGroup.alpha;
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _rootGroup.alpha = Mathf.Lerp(start, target, t / FadeDuration);
                yield return null;
            }
            _rootGroup.alpha = target;

            // 꺼질 때만 마지막에 비활성화한다 — 켜질 때 미리 SetActive(true) 해 둬야
            // 알파가 실제로 0→1로 보간되는 걸 볼 수 있다(꺼진 오브젝트는 코루틴이 안 돈다).
            if (target <= 0f) _root.SetActive(false);
            _fadeRoutine = null;
        }

        public void RequestReturnToCheckpoint()
        {
            _dialog.ShowConfirm(
                "최근 기억으로 돌아가기",
                "현재 방의 일시적인 변화가 초기화됩니다.\n최근 체크포인트로 돌아갈까요?",
                "돌아가기",
                ReturnToCheckpoint,
                _checkpointButton);
        }

        void ReturnToCheckpoint()
        {
            Close();
            GameManager.Instance.RespawnPlayer();
        }

        public void RequestGoToTitle()
        {
            _dialog.ShowConfirm(
                "타이틀로 돌아가기",
                "진행 상황을 기억에 남기고 타이틀로 돌아갈까요?",
                "타이틀로",
                GoToTitle,
                _titleButton);
        }

        void GoToTitle()
        {
            _dialog.Hide(false);
            _root.SetActive(false);
            PlayerInput.Enabled = true;
            GameManager.Instance.SaveProgress();
            // 타이틀 씬으로 넘어가므로 timeScale을 되돌리는 의미로도 상태를 Title로 되돌린다.
            GameManager.Instance.SetState(GameState.Title);
            SceneFlow.LoadWithFade(SceneFlow.Title);
        }

        void OnDestroy()
        {
            // 일시정지 중 예외적으로 씬이 내려가도 다음 씬의 입력과 timeScale을 잠그지 않는다.
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused)
            {
                PlayerInput.Enabled = true;
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("PauseCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            UIBuilder.ConfigureScaler(canvasGO.AddComponent<CanvasScaler>());
            canvasGO.AddComponent<GraphicRaycaster>();

            _root = new GameObject("PausePanel", typeof(RectTransform));
            _root.transform.SetParent(canvasGO.transform, false);
            var panelRt = (RectTransform)_root.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var bg = _root.AddComponent<Image>();
            bg.color = UIBuilder.PanelBackground;

            _rootGroup = _root.AddComponent<CanvasGroup>();
            _rootGroup.alpha = 0f;

            var title = UIBuilder.CreateText(_root.transform, "일시정지", 36);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.65f);
            titleRt.sizeDelta = new Vector2(400f, 60f);
            titleRt.anchoredPosition = Vector2.zero;

            // Close에 기본 인자가 생겨 메서드 그룹으로는 UnityAction에 안 맞는다.
            _resumeButton = UIBuilder.CreateButton(_root.transform, "계속하기", 10f, () => Close());
            _checkpointButton = UIBuilder.CreateButton(
                _root.transform, "체크포인트로 돌아가기", -60f, RequestReturnToCheckpoint);
            _titleButton = UIBuilder.CreateButton(_root.transform, "타이틀로", -130f, RequestGoToTitle);

            CreateSectionButton("지도", -330f, PauseSection.Map);
            CreateSectionButton("기억 기록", -110f, PauseSection.Journal);
            CreateSectionButton("조작법", 110f, PauseSection.Controls);
            CreateSectionButton("설정", 330f, PauseSection.Settings);

            _dialog = _root.AddComponent<ConfirmDialog>();
            _sections = _root.AddComponent<PauseSectionPanel>();

            _root.SetActive(false);
        }

        void CreateSectionButton(string label, float x, PauseSection section)
        {
            var button = UIBuilder.CreateButton(_root.transform, label, 240f, () => _sections.Show(section));
            var rt = (RectTransform)button.transform;
            rt.anchoredPosition = new Vector2(x, 240f);
            rt.sizeDelta = new Vector2(190f, 48f);
        }
    }
}
