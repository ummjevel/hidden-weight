using System.Collections;
using UnityEngine;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 매복형 — 균열의 "선행 그림자"(FRACTURE_LEVEL_DESIGN.md 6.1절). 본체보다 미래 타격
    // 그림자가 먼저 나타난다.
    //
    // 순서가 이 적의 전부다: 타격 지점을 leadSeconds 먼저 바닥에 그리고, 그 시간이 지난
    // 뒤에 그 자리를 친다. 그림자가 뜬 뒤에는 지점이 절대 바뀌지 않는다 — 예지가 보여주는
    // 위치와 실제가 어긋나면 안 되고(7.2절), 예지 없이도 그림자만 보고 피할 수 있어야 한다.
    [RequireComponent(typeof(Enemy))]
    public class PrecursorBehavior : EnemyBehavior, IForeseeable
    {
        [SerializeField] LayerMask playerMask;
        [SerializeField] Sprite markerSprite;
        [SerializeField] float strikeRadius = 1.8f;
        [SerializeField] float cooldownSeconds = 3f;

        bool _busy;
        float _cooldown;
        Vector3 _strikePoint;

        public bool HasPendingStrike { get; private set; }
        public Vector3 StrikePoint => _strikePoint;

        public Transform Transform => transform;
        public Sprite CurrentSprite => Sprite != null ? Sprite.sprite : null;

        protected override void Awake()
        {
            base.Awake();

            // 자리를 지키는 적이다. 물리로 밀려나지도 않는다.
            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
        }

        void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_busy || _cooldown > 0f || Player == null) return;
            if (DistanceToPlayer > Data.detectRange) return;

            StartCoroutine(StrikeRoutine());
        }

        IEnumerator StrikeRoutine()
        {
            _busy = true;

            // 타격 지점을 지금 확정한다. 이후 플레이어가 움직여도 따라가지 않는다.
            _strikePoint = Player.position;
            HasPendingStrike = true;
            FaceTowards(DirectionToPlayer);

            var marker = SpawnMarker(_strikePoint);
            yield return new WaitForSeconds(Data.leadSeconds);
            if (marker != null) Destroy(marker);

            Self.PlayClip("Attack");
            var hit = Physics2D.OverlapCircle(_strikePoint, strikeRadius, playerMask);
            if (hit != null)
            {
                var health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(Data.contactDamage, _strikePoint);
            }

            HasPendingStrike = false;
            yield return new WaitForSeconds(Data.recoverSeconds);
            _cooldown = cooldownSeconds;
            _busy = false;
        }

        GameObject SpawnMarker(Vector3 position)
        {
            if (markerSprite == null) return null;

            var go = new GameObject("PrecursorShadow");
            go.transform.position = new Vector3(position.x, position.y - 0.5f, 0f);
            go.transform.localScale = new Vector3(strikeRadius * 2f, 0.4f, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            renderer.color = new Color(0.2f, 0.1f, 0.3f, 0.55f);
            renderer.sortingOrder = 4;
            return go;
        }

        // 예지 반응(6.1절): "공격 종료 위치와 활성 여부 표시". 예고 중이면 타격 지점을,
        // 아니면 본체 자리를 돌려준다.
        public Vector3 PredictPosition(float leadSeconds)
            => HasPendingStrike ? _strikePoint : transform.position;

        public bool PredictActive(float leadSeconds) => Self.IsAlive;
    }
}
