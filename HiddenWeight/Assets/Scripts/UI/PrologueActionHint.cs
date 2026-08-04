using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // 프롤로그 전용 맥락형 조작 안내. 공통 TutorialHint를 바꾸지 않고,
    // 실제 행동 성공을 확인한 뒤 해당 안내만 영구적으로 숨긴다.
    public class PrologueActionHint : MonoBehaviour
    {
        static PrologueActionHint _active;

        public enum RequiredAction
        {
            Move,
            Jump,
            WallJump,
            Dash,
            Attack
        }

        [SerializeField] RequiredAction action;
        [SerializeField, TextArea(1, 2)] string message;
        [SerializeField] float showRadius = 4.5f;
        [SerializeField] float delaySeconds;
        [SerializeField] float fadeSpeed = 5f;
        [SerializeField] float minimumReadableSeconds = 1f;
        [SerializeField] float visibleSeconds = 5.5f;

        TextMesh _text;
        PlayerController _player;
        float _alpha;
        float _nearSince = -1f;
        float _visibleSince = -1f;
        float _hideAt;
        bool _activated;

        public RequiredAction Action => action;
        public bool IsCompleted { get; private set; }

        void Start()
        {
            var textObject = new GameObject("HintText");
            textObject.transform.SetParent(transform, false);

            _text = textObject.AddComponent<TextMesh>();
            _text.font = Resources.Load<Font>("Fonts/NanumMyeongjo-Bold")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 64;
            _text.characterSize = 0.072f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = new Color(1f, 1f, 1f, 0f);
            RefreshPrompt(InputPrompts.CurrentDevice);

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.material = _text.font.material;
            renderer.sortingOrder = 40;

            _player = PlayerController.Instance;
            InputPrompts.DeviceChanged += RefreshPrompt;
        }

        void OnDestroy()
        {
            InputPrompts.DeviceChanged -= RefreshPrompt;
            if (_active == this) _active = null;
        }

        void RefreshPrompt(InputDeviceKind _) => _text.text = InputPrompts.Format(message);

        bool Matches(PlayerState state)
        {
            return action switch
            {
                RequiredAction.Move => state == PlayerState.Walk || state == PlayerState.Run,
                RequiredAction.Jump => state == PlayerState.Jump || state == PlayerState.AirMove,
                RequiredAction.WallJump => state == PlayerState.WallJump,
                RequiredAction.Dash => state == PlayerState.Dash,
                RequiredAction.Attack => state == PlayerState.Attack,
                _ => false
            };
        }

        void Complete()
        {
            if (IsCompleted) return;
            IsCompleted = true;
        }

        void Update()
        {
            if (_text == null) return;
            if (_player == null) _player = PlayerController.Instance;

            bool near = !_activated && _player != null
                && Vector2.Distance(_player.transform.position, transform.position) <= showRadius;

            if (near)
            {
                if (_nearSince < 0f) _nearSince = Time.unscaledTime;
            }
            else
            {
                _nearSince = -1f;
            }

            if (!_activated && near && Time.unscaledTime - _nearSince >= delaySeconds)
            {
                if (_active != null && _active != this)
                    _active.DismissImmediately();
                _active = this;
                _activated = true;
                _visibleSince = Time.unscaledTime;
                _hideAt = Time.unscaledTime + visibleSeconds;
            }

            bool show = _activated && Time.unscaledTime < _hideAt;

            // 월드 지점에 글자를 고정하면 달리는 플레이어가 1초 만에 지나쳐 버린다.
            // 활성화된 동안은 카메라 상단을 따라가 실제 화면에서 읽을 시간을 보장한다.
            Camera camera = Camera.main;
            if (show && camera != null)
                _text.transform.position = new Vector3(
                    camera.transform.position.x,
                    camera.transform.position.y + camera.orthographicSize * 0.62f,
                    transform.position.z);

            _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            _text.color = new Color(0.94f, 0.95f, 1f, _alpha * 0.98f);

            // 씬 시작부터 모든 상태 변화를 듣고 있으면, 안내 지점에 오기 전에 같은 행동을
            // 한 것만으로 문구가 영영 사라진다. 실제로 문구를 읽을 수 있게 표시한 뒤에만
            // 그 자리에서 수행한 행동을 완료로 인정한다.
            if (show && !IsCompleted && _visibleSince >= 0f
                && Time.unscaledTime - _visibleSince >= minimumReadableSeconds
                && Matches(_player.State))
                Complete();
        }

        void DismissImmediately()
        {
            _hideAt = Time.unscaledTime;
            _alpha = 0f;
            if (_text != null)
                _text.color = new Color(0.94f, 0.95f, 1f, 0f);
            if (_active == this) _active = null;
        }
    }
}
