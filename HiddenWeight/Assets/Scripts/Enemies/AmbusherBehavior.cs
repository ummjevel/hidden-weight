using System.Collections;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Enemies
{
    // 매복형 — "매달린 손가락"(CONTENT_SYSTEM.md 4절). 천장에 붙어 있다가 아래를 지나면 떨어진다.
    //
    // 명세(R06): "천장 (18,11), 바닥에 1.5초간 흔들리는 그림자 예고", "매복 적 착지점 좌우에
    // 각각 3유닛 안전 공간". 예고가 이 적의 전부다 — 그림자를 보고 비키면 맞지 않아야 한다.
    // 그래서 낙하는 예고가 끝난 뒤에 시작하고, 예고 중에는 플레이어를 따라가지 않는다.
    public class AmbusherBehavior : EnemyBehavior
    {
        [SerializeField] float triggerWidth = 1.5f;  // 이 폭 안으로 들어오면 예고 시작
        [SerializeField] float shadowSeconds = 1.5f; // 명세가 지정한 그림자 예고 시간
        [SerializeField] SpriteRenderer shadow;      // 착지 예정 지점에 그리는 그림자

        // 응시의 "매달린 관객"은 같은 매복형이지만 숨죽인 플레이어 바로 아래를 지나가도
        // 떨어지지 않는다(GAZE_LEVEL_DESIGN.md 6.1절). 잔재의 매달린 손가락은 이 값을
        // 끄고 그대로 쓴다 — 적을 새로 만들지 않고 반응 규칙 하나만 갈아끼운다.
        [SerializeField] bool ignoreHushedPlayer = false;

        bool _triggered;

        protected override void Awake()
        {
            base.Awake();

            // 매복 중에는 중력을 받지 않는다. 천장에 붙어 있어야 하기 때문이다.
            Body.bodyType = RigidbodyType2D.Kinematic;
            if (shadow != null) shadow.enabled = false;
        }

        void Update()
        {
            if (_triggered || Player == null) return;
            if (ignoreHushedPlayer && PlayerLayers.IsHushed(Player.gameObject)) return;

            // 아래를 지날 때만. 위나 옆에 있을 때는 반응하지 않는다.
            bool below = Player.position.y < transform.position.y;
            bool aligned = Mathf.Abs(Player.position.x - transform.position.x) <= triggerWidth;
            if (below && aligned) StartCoroutine(DropRoutine());
        }

        IEnumerator DropRoutine()
        {
            _triggered = true;

            // 예고: 그림자를 깜빡인다. 이 동안 위치는 고정이라 비키면 확실히 피할 수 있다.
            if (shadow != null) shadow.enabled = true;
            float elapsed = 0f;
            while (elapsed < shadowSeconds)
            {
                if (shadow != null)
                {
                    var color = shadow.color;
                    color.a = Mathf.PingPong(elapsed * 4f, 0.6f) + 0.2f;
                    shadow.color = color;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (shadow != null) shadow.enabled = false;

            // 낙하. 여기서부터 일반 물리로 돌려 바닥에 착지하고, 이후에는 순찰형처럼 굴러간다.
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.linearVelocity = new Vector2(0f, -Data.dropSpeed);

            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = true;
        }
    }
}
