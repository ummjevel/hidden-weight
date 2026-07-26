using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.UI
{
    // 조작 안내 (기획서 WORLD_MAP 1.3절 튜토리얼 UI — 텍스트 방식 채택).
    // 캔버스 없이 월드 공간 TextMesh 하나로, 플레이어가 가까이 오면 서서히 떠오른다.
    // 씬 빌더가 배치하고 message만 넣어 주면 나머지는 스스로 만든다.
    public class TutorialHint : MonoBehaviour
    {
        [SerializeField, TextArea(1, 3)] string message;
        [SerializeField] float showRadius = 5f;
        [SerializeField] float fadeSpeed = 4f;

        TextMesh _text;
        float _alpha;

        void Start()
        {
            var go = new GameObject("HintText");
            go.transform.SetParent(transform, false);

            _text = go.AddComponent<TextMesh>();
            _text.text = message;
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 48;
            _text.characterSize = 0.06f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = new Color(1f, 1f, 1f, 0f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = _text.font.material;
            renderer.sortingOrder = 40; // 플레이어(10)·고스트(50) 사이, 항상 배경 위
        }

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
