using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // 조작 안내 (기획서 WORLD_MAP 1.3절 튜토리얼 UI — 텍스트 방식 채택).
    // 월드 공간 캔버스에서 플레이어가 가까이 오면 서서히 떠오른다.
    // 씬 빌더가 배치하고 message만 넣어 주면 나머지는 스스로 만든다.
    public class TutorialHint : MonoBehaviour
    {
        [SerializeField, TextArea(1, 3)] string message;
        [SerializeField] float showRadius = 5f;
        [SerializeField] float fadeSpeed = 4f;

        Text _text;
        float _alpha;

        void Start()
        {
            // 화면 UI와 같은 uGUI 렌더링 경로를 사용한다. WebGL에서만 TextMesh의 동적
            // 글리프 메시가 비는 문제를 피하면서 이미 포함된 폰트를 공유해 용량을 늘리지 않는다.
            _text = UIBuilder.CreateWorldText(transform, "HintText", new Vector2(720f, 140f),
                0.01f, 48, 40);
            _text.color = new Color(1f, 1f, 1f, 0f);
            RefreshPrompt(InputPrompts.CurrentDevice);

            InputPrompts.DeviceChanged += RefreshPrompt;
        }

        void OnDestroy() => InputPrompts.DeviceChanged -= RefreshPrompt;

        void RefreshPrompt(InputDeviceKind _) => _text.text = InputPrompts.Format(message);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || _text == null) return;

            bool near = Vector2.Distance(player.transform.position, transform.position) <= showRadius;
            _alpha = Mathf.MoveTowards(_alpha, near ? 1f : 0f, fadeSpeed * Time.deltaTime);
            _text.color = new Color(1f, 1f, 1f, _alpha * 0.9f);
        }
    }
}
