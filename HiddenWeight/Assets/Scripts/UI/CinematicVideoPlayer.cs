using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    public class CinematicVideoPlayer : MonoBehaviour
    {
        const string VideoFolder = "Videos";

        static CinematicVideoPlayer _active;

        VideoPlayer _videoPlayer;
        RawImage _screen;
        Button _skipButton;
        RenderTexture _targetTexture;
        System.Action _onFinished;
        bool _finished;
        bool _prepared;
        string _fileName;

        public static void Play(string fileName, System.Action onFinished)
        {
            if (_active != null) _active.Finish();

            var go = new GameObject("CinematicVideoPlayer");
            DontDestroyOnLoad(go);
            _active = go.AddComponent<CinematicVideoPlayer>();
            _active.Begin(fileName, onFinished);
        }

        void Begin(string fileName, System.Action onFinished)
        {
            _fileName = fileName;
            _onFinished = onFinished;
            PlayerInput.Enabled = false;
            BuildHierarchy();
            StartCoroutine(PlayRoutine());
        }

        void Update()
        {
            if (_finished) return;

            if (Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                Finish();
            }
        }

        void OnDestroy()
        {
            if (_active == this) _active = null;
            if (_targetTexture != null)
            {
                _targetTexture.Release();
                Destroy(_targetTexture);
            }
        }

        IEnumerator PlayRoutine()
        {
            string basePath = Application.streamingAssetsPath.TrimEnd('/', '\\');
            string url = basePath + "/" + VideoFolder + "/" + _fileName;

            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = url;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.isLooping = false;
            _videoPlayer.skipOnDrop = true;
            _videoPlayer.errorReceived += (_, message) =>
            {
                Debug.LogWarning("Cinematic video failed: " + message + " (" + url + ")");
                Finish();
            };
            _videoPlayer.loopPointReached += _ => Finish();
            _videoPlayer.prepareCompleted += _ => _prepared = true;
            _videoPlayer.Prepare();

            float wait = 0f;
            while (!_prepared && wait < 5f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_prepared)
            {
                Debug.LogWarning("Cinematic video prepare timeout: " + url);
                Finish();
                yield break;
            }

            int width = Mathf.Max(16, (int)_videoPlayer.width);
            int height = Mathf.Max(9, (int)_videoPlayer.height);
            _targetTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = Path.GetFileNameWithoutExtension(_fileName) + "_RT",
            };
            _targetTexture.Create();
            _videoPlayer.targetTexture = _targetTexture;
            _screen.texture = _targetTexture;

            var fitter = _screen.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = width / (float)height;

            _videoPlayer.Play();
        }

        void Finish()
        {
            if (_finished) return;
            _finished = true;

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.targetTexture = null;
            }

            PlayerInput.Enabled = true;
            var callback = _onFinished;
            Destroy(gameObject);
            callback?.Invoke();
        }

        void BuildHierarchy()
        {
            var canvasGO = new GameObject("CinematicCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;
            UIBuilder.ConfigureScaler(canvasGO.AddComponent<CanvasScaler>());
            canvasGO.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("CinematicEventSystem");
                eventSystem.transform.SetParent(transform, false);
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            var blocker = new GameObject("CinematicBlackout", typeof(RectTransform));
            blocker.transform.SetParent(canvasGO.transform, false);
            var blockerRt = (RectTransform)blocker.transform;
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = blockerRt.offsetMax = Vector2.zero;
            blocker.AddComponent<Image>().color = Color.black;

            var screenGO = new GameObject("CinematicScreen", typeof(RectTransform));
            screenGO.transform.SetParent(canvasGO.transform, false);
            _screen = screenGO.AddComponent<RawImage>();
            _screen.color = Color.white;
            _screen.raycastTarget = false;
            var screenRt = _screen.rectTransform;
            screenRt.anchorMin = Vector2.zero;
            screenRt.anchorMax = Vector2.one;
            screenRt.offsetMin = screenRt.offsetMax = Vector2.zero;

            var skipPanel = UIBuilder.CreateMenuPanel(canvasGO.transform, "SkipPanel",
                new Color(0.025f, 0.026f, 0.043f, 0.78f));
            var panelRt = skipPanel.rectTransform;
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.sizeDelta = new Vector2(208f, 72f);
            panelRt.anchoredPosition = new Vector2(-34f, -28f);

            _skipButton = UIBuilder.CreateButton(skipPanel.transform, "건너뛰기", 0f, Finish);
            var buttonRt = _skipButton.GetComponent<RectTransform>();
            buttonRt.anchorMin = Vector2.zero;
            buttonRt.anchorMax = Vector2.one;
            buttonRt.offsetMin = new Vector2(12f, 8f);
            buttonRt.offsetMax = new Vector2(-12f, -8f);
            buttonRt.sizeDelta = Vector2.zero;
            buttonRt.anchoredPosition = Vector2.zero;

            var label = _skipButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 20;
                label.text = "건너뛰기";
            }

            UIBuilder.Select(_skipButton);
        }
    }
}
