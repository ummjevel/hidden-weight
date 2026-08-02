using UnityEngine;

namespace HiddenWeight.World
{
    // 가짜 공격 예고를 내는 관객 조각상. 응시 지역 보스 2단계 "가짜 판결"이 쓴다
    // (GAZE_LEVEL_DESIGN.md 7.2절: 여러 조각상이 같은 예고를 보내고, 자각 중에는 실제
    // 그림자만 바닥과 일치한다).
    //
    // 자각 반응을 이 컴포넌트가 직접 맡는다. 보스 본체가 아니라 전장과 정보가 감정 스킬에
    // 반응해야 한다는 규칙(7.2절 공정성 규칙 마지막 항목) 때문이다 — 자각이 켜지면 가짜는
    // 예고를 멈추고 바닥 그림자가 사라져, 남은 하나가 진짜라는 것이 저절로 드러난다.
    public class DecoyTelegraph : MonoBehaviour, IAwarenessReactive
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer groundShadow; // 진짜만 바닥과 일치하는 그림자
        [SerializeField] bool isReal;                 // 실제 공격 주체인지
        [SerializeField] float period = 2.2f;
        [SerializeField] float phaseOffset = 0f;
        [SerializeField] float telegraphSeconds = 1f; // 예고를 켜 두는 길이

        bool _awarenessOn;

        public bool IsReal => isReal;

        // 지금 예고를 내고 있는가. 보스가 실제 타격 타이밍을 이 값과 맞춘다.
        public bool IsTelegraphing
        {
            get
            {
                if (period <= 0f) return false;
                return Mathf.Repeat(ZoneClock.Now + phaseOffset, period) < telegraphSeconds;
            }
        }

        void Awake()
        {
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable() => AwarenessRegistry.Register(this);
        void OnDisable() => AwarenessRegistry.Unregister(this);

        public void OnAwarenessChanged(bool active) => _awarenessOn = active;

        void Update()
        {
            // 자각 중에는 가짜가 조용해진다. 진짜는 자각 여부와 무관하게 똑같이 예고한다 —
            // 자각이 정보를 더 주는 것이지, 진짜를 바꾸는 것이 아니다.
            bool showing = IsTelegraphing && (isReal || !_awarenessOn);

            if (body != null)
                body.color = showing ? new Color(1f, 0.75f, 0.75f) : Color.white;

            if (groundShadow != null)
                groundShadow.enabled = showing && (isReal || !_awarenessOn);
        }
    }
}
