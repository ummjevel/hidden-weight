using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // 프롤로그에서 세계의 정체와 목표만 짧게 설명한다. 조작 안내와 분리해 한 번 재생한 뒤
    // 사라지며, 확인 입력이나 이동 정지를 요구하지 않는다.
    public class PrologueConceptHint : MonoBehaviour
    {
        [SerializeField, TextArea(1, 3)] string message;
        [SerializeField] float showRadius = 3f;
        [SerializeField] float visibleSeconds = 5.5f;
        [SerializeField] float fadeSpeed = 4f;

        TextMesh _text;
        PlayerController _player;
        float _alpha;
        float _hideAt;

        public string Message => message;
        public bool HasShown { get; private set; }

        void Start()
        {
            var textObject = new GameObject("ConceptText");
            textObject.transform.SetParent(transform, false);

            _text = textObject.AddComponent<TextMesh>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 48;
            _text.characterSize = 0.048f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.lineSpacing = 0.9f;
            _text.text = message;
            _text.color = new Color(1f, 1f, 1f, 0f);

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.material = _text.font.material;
            renderer.sortingOrder = 39;

            _player = PlayerController.Instance;
        }

        void Update()
        {
            if (_text == null) return;
            if (_player == null) _player = PlayerController.Instance;

            if (!HasShown && _player != null
                && Vector2.Distance(_player.transform.position, transform.position) <= showRadius)
            {
                HasShown = true;
                _hideAt = Time.unscaledTime + visibleSeconds;
            }

            bool show = HasShown && Time.unscaledTime < _hideAt;

            // 달리는 중에도 문장이 화면 밖으로 밀리지 않게 카메라 안쪽에 고정한다.
            Camera camera = Camera.main;
            if (show && camera != null)
                _text.transform.position = new Vector3(
                    camera.transform.position.x,
                    camera.transform.position.y + camera.orthographicSize * 0.34f,
                    transform.position.z);

            _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f,
                fadeSpeed * Time.unscaledDeltaTime);
            _text.color = new Color(0.9f, 0.92f, 1f, _alpha * 0.94f);
        }
    }
}
