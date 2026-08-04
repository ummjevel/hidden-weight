using UnityEngine;

namespace HiddenWeight.World
{
    // 방 출구를 "여기로 나갈 수 있다"고 알리는 은은한 맥동.
    //
    // 방 문은 트리거 콜라이더만 있어 눈으로는 벽과 구분되지 않았다. 문 너머는 아직 로드되지
    // 않은 다른 씬이라 배경에도 아무 단서가 없다 — 처음 플레이하는 사람이 다음에 어디로
    // 가야 할지 알 방법이 없던 가장 큰 이유다.
    //
    // HUD 화살표를 쓰지 않는 것은 의도적이다(설계 원칙 2 "세계가 먼저, UI가 보조한다").
    // 길 안내는 화면 위에 얹은 기호가 아니라 공간 안의 빛이어야 한다.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DoorBeacon : MonoBehaviour
    {
        [SerializeField] float minAlpha = 0.34f;
        [SerializeField] float maxAlpha = 0.72f;
        [SerializeField] float period = 2.6f;

        // 문마다 위상을 어긋나게 둔다. 한 방에 문이 둘이면 같이 뛰는 것이 오히려
        // 기계 장치처럼 보인다.
        float _phase;
        SpriteRenderer _renderer;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _phase = Mathf.Repeat(transform.position.x * 0.37f
                                  + transform.position.y * 0.71f, 1f);
        }

        void Update()
        {
            if (_renderer == null) return;

            float t = Mathf.Repeat(Time.time / Mathf.Max(0.1f, period) + _phase, 1f);
            // 삼각파가 아니라 사인이어야 한다. 꺾이는 지점이 있으면 깜빡임으로 읽힌다.
            float amount = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;

            var color = _renderer.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, amount);
            _renderer.color = color;
        }
    }
}
