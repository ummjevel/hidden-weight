using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 원거리·지원형 — 균열의 "가능성 수집자"(FRACTURE_LEVEL_DESIGN.md 6.1절). 착지 예정
    // 발판에 지연 폭발을 배치해 이동 결정을 재촉한다.
    //
    // 노리는 곳은 플레이어의 현재 위치가 아니라 "곧 발을 디딜 자리"다. 그래서 수평 속도를
    // 그대로 연장한 지점에 놓는다 — 가만히 서 있으면 발밑에, 달리고 있으면 앞쪽에 떨어져
    // "멈춰서 기다리는 것"과 "계속 달리는 것" 어느 쪽도 정답이 되지 않는다(4.7절).
    //
    // 폭발 자체는 World/DelayedBlast가 맡는다. 이 적은 어디에 놓을지만 정한다.
    [RequireComponent(typeof(Enemy))]
    public class CollectorBehavior : EnemyBehavior
    {
        [SerializeField] LayerMask playerMask;
        [SerializeField] Sprite blastSprite;
        [SerializeField] float intervalSeconds = 3.5f;

        float _timer;

        protected override void Awake()
        {
            base.Awake();
            _timer = intervalSeconds;

            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
        }

        void Update()
        {
            if (Player == null) return;
            if (DistanceToPlayer > Data.detectRange) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = intervalSeconds;

            FaceTowards(DirectionToPlayer);
            Self.PlayClip("Attack");
            PlaceBlast(PredictLanding());
        }

        // 플레이어의 수평 속도를 도화선 시간만큼 연장한 지점.
        Vector3 PredictLanding()
        {
            var body = Player.GetComponent<Rigidbody2D>();
            float vx = body != null ? body.linearVelocity.x : 0f;
            return Player.position + new Vector3(vx * Data.blastFuse * 0.5f, 0f, 0f);
        }

        void PlaceBlast(Vector3 position)
        {
            var go = new GameObject("DelayedBlast");
            go.transform.position = position;

            var visualGO = new GameObject("Visual");
            visualGO.transform.SetParent(go.transform, false);
            visualGO.transform.localScale = new Vector3(Data.blastRadius * 2f, Data.blastRadius * 2f, 1f);

            var renderer = visualGO.AddComponent<SpriteRenderer>();
            renderer.sprite = blastSprite;
            renderer.color = new Color(1f, 0.55f, 0.4f, 0.5f);
            renderer.sortingOrder = 6;

            var blast = go.AddComponent<DelayedBlast>();
            blast.Configure(Data.blastFuse, Data.blastRadius, Data.contactDamage, playerMask, renderer);
        }
    }
}
