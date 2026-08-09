using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // 프롤로그에서 세계의 정체와 목표만 짧게 설명한다. 조작 안내와 분리해 한 번 재생한 뒤
    // 사라지며, 확인 입력이나 이동 정지를 요구하지 않는다.
    public class PrologueConceptHint : MonoBehaviour
    {
        static PrologueConceptHint _active;

        [SerializeField, TextArea(1, 3)] string message;
        [SerializeField] float showRadius = 3f;
        [SerializeField] float visibleSeconds = 5.5f;
        [SerializeField] float fadeSpeed = 4f;

        Text _text;
        Transform _textRoot;
        PlayerController _player;
        float _alpha;
        float _hideAt;

        public string Message => message;
        public bool HasShown { get; private set; }

        public static void DismissActiveImmediately()
        {
            if (_active == null) return;
            _active._hideAt = Time.unscaledTime;
            _active._alpha = 0f;
            if (_active._text != null)
                _active._text.color = new Color(0.94f, 0.93f, 1f, 0f);
            _active = null;
        }

        void Start()
        {
            _text = UIBuilder.CreateWorldText(transform, "ConceptText", new Vector2(1000f, 220f),
                0.01f, 50, 39, false);
            _textRoot = _text.transform.parent;
            _text.lineSpacing = 1.05f;
            _text.text = message;
            _text.color = new Color(1f, 1f, 1f, 0f);

            _player = PlayerController.Instance;
        }

        void OnDestroy()
        {
            if (_active == this) _active = null;
        }

        void Update()
        {
            if (_text == null) return;
            if (_player == null) _player = PlayerController.Instance;

            if (!HasShown && _player != null
                && !PrologueActionHint.HasActiveHint
                && Vector2.Distance(_player.transform.position, transform.position) <= showRadius)
            {
                DismissActiveImmediately();
                _active = this;
                HasShown = true;
                _hideAt = Time.unscaledTime + visibleSeconds;
            }

            bool show = HasShown && Time.unscaledTime < _hideAt;

            // 달리는 중에도 문장이 화면 밖으로 밀리지 않게 카메라 안쪽에 고정한다.
            Camera camera = Camera.main;
            if (show && camera != null)
                _textRoot.position = new Vector3(
                    camera.transform.position.x,
                    camera.transform.position.y + camera.orthographicSize * 0.34f,
                    transform.position.z);

            _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f,
                fadeSpeed * Time.unscaledDeltaTime);
            _text.color = new Color(0.94f, 0.93f, 1f, _alpha * 0.98f);
        }
    }
}
