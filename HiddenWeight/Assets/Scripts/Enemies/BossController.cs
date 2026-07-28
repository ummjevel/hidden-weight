using System.Collections;
using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.Enemies
{
    // 보스 전장 관리자(RESIDUE_ROOM_IMPLEMENTATION.md 2.2절). R10 '손목의 감시자'와
    // R12 '기억의 교수자'가 같은 컴포넌트를 쓰고 단계 구성만 다르게 한다.
    //
    // 설계 원칙(R10·R12 명세 공통): 난도는 공격 속도가 아니라 조합 순서로 올린다.
    // 그래서 단계가 올라가도 telegraphSeconds는 절대 줄이지 않고, 대신 연속 사용 횟수만 늘린다.
    // "첫 도전에서도 각 공격을 최소 한 번은 보고 살아남을 수 있어야 한다"가 검증 항목이기 때문이다.
    [RequireComponent(typeof(Enemy))]
    public class BossController : MonoBehaviour
    {
        // 잔재는 앞의 셋만 쓴다. 뒤의 셋은 응시·균열이 추가로 쓴다 — 보스 클래스를 지역마다
        // 새로 만들지 않고 무브 목록만 갈아끼운다.
        //   GazeSweep : 시선 공격. 숨죽인 플레이어에게는 닿지 않는다(응시 7.1절).
        //   WallClose : 눈꺼풀 닫기. 좌우 벽이 좁아지고 중앙 안전지대만 남는다(응시 7.1절).
        //   TimeSkip  : 시간 건너뛰기. 사라졌다가 예고 지점에 나타나 낙하한다(균열 7.1절).
        public enum Move { GroundSweep, Charge, Slam, GazeSweep, WallClose, TimeSkip }

        [SerializeField] Move[] moves = { Move.GroundSweep, Move.Charge, Move.Slam };
        // 공격별 예고 시간(R10 명세: 지상 쓸기 0.7 / 돌진 1.0 / 낙하 1.2).
        // 단계가 올라가도 이 값은 줄이지 않는다 — 난도는 조합으로만 올린다.
        [SerializeField] float sweepTelegraph = 0.7f;
        [SerializeField] float chargeTelegraph = 1.0f;
        [SerializeField] float slamTelegraph = 1.2f;
        [SerializeField] float sweepHeight = 1.2f;   // 이 높이 위로 뛰면 쓸기를 넘는다
        [SerializeField] Sprite shadowSprite;        // 낙하 예고용 바닥 그림자
        [SerializeField] float recoverSeconds = 1.0f;
        [SerializeField] float sweepRange = 5f;
        [SerializeField] float chargeSpeed = 12f;
        [SerializeField] float slamHeight = 6f;
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask obstacleMask;

        [Header("시선 공격 — 숨죽이기로 회피된다")]
        // playerMask와 달리 PlayerHushed를 넣지 않는다. 숨죽이면 시선 공격만 통하지 않고
        // 물리 돌진은 그대로 맞는다는 규칙(GAZE_LEVEL_DESIGN.md 7.1절)이 이 한 줄로 성립한다.
        [SerializeField] LayerMask gazeMask;
        [SerializeField] float gazeSweepTelegraph = 1.0f;
        [SerializeField] float gazeSweepSeconds = 2.5f;  // 명세: 바닥을 2.5초에 걸쳐 훑는다
        [SerializeField] float gazeSweepWidth = 3f;

        [Header("눈꺼풀 닫기")]
        [SerializeField] Transform[] closingWalls;       // 좌우 벽 2개
        [SerializeField] float wallCloseTelegraph = 1.2f;
        [SerializeField] float wallCloseDistance = 4f;   // 안쪽으로 이 거리만큼 좁힌다
        [SerializeField] float wallCloseHoldSeconds = 1.5f;

        [Header("시간 건너뛰기")]
        [SerializeField] float timeSkipTelegraph = 1.0f;
        [SerializeField] float timeSkipLead = 2f;        // 예지 선행 시간과 같은 값

        // 체력 비율이 이 값 아래로 내려가면 다음 단계. 명세의 R12 3단계(1.0 → 0.6 → 0.3)를 기본값으로.
        [SerializeField] float[] phaseThresholds = { 0.6f, 0.3f };

        Enemy _self;
        Rigidbody2D _body;
        SpriteRenderer _sprite;
        int _moveIndex;

        public int Phase { get; private set; }

        void Awake()
        {
            _self = GetComponent<Enemy>();
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable() => StartCoroutine(FightRoutine());

        IEnumerator FightRoutine()
        {
            // 입장 직후 한 박자 쉰다 — 명세의 "입장 후 2초간 보스를 관찰"과 짝을 이룬다.
            yield return new WaitForSeconds(1f);

            while (_self.IsAlive)
            {
                UpdatePhase();

                // 단계가 오를수록 한 번에 잇는 공격 수만 늘린다(1 → 2 → 2).
                int combo = Phase == 0 ? 1 : 2;
                for (int i = 0; i < combo && _self.IsAlive; i++)
                {
                    yield return PerformMove(moves[_moveIndex % moves.Length]);
                    _moveIndex++;
                }

                yield return new WaitForSeconds(recoverSeconds);
            }
        }

        void UpdatePhase()
        {
            float ratio = _self.Data.maxHealth <= 0 ? 1f : (float)_self.Health / _self.Data.maxHealth;
            int phase = 0;
            for (int i = 0; i < phaseThresholds.Length; i++)
                if (ratio <= phaseThresholds[i]) phase = i + 1;
            Phase = phase;
        }

        IEnumerator PerformMove(Move move)
        {
            var player = PlayerController.Instance;
            if (player == null) yield break;

            // 예고는 어떤 단계에서도 같은 길이다. 공격마다 길이가 다르다(명세 표).
            float telegraph;
            switch (move)
            {
                case Move.GroundSweep: telegraph = sweepTelegraph; break;
                case Move.Charge: telegraph = chargeTelegraph; break;
                case Move.GazeSweep: telegraph = gazeSweepTelegraph; break;
                case Move.WallClose: telegraph = wallCloseTelegraph; break;
                case Move.TimeSkip: telegraph = timeSkipTelegraph; break;
                default: telegraph = slamTelegraph; break;
            }

            // 낙하 계열은 떨어질 자리를 바닥 그림자로 먼저 보여준다 — 좌우로 비키면 피할 수
            // 있어야 한다. 시간 건너뛰기는 예고 시점의 위치를 그대로 쓴다(균열 7.2절:
            // 고스트가 보여준 위치와 실제가 항상 일치한다).
            GameObject shadow = null;
            if (move == Move.Slam || move == Move.TimeSkip)
                shadow = ShowDropShadow(player.transform.position);
            Vector3 skipTarget = player.transform.position;

            Telegraph(true);
            yield return new WaitForSeconds(telegraph);
            Telegraph(false);
            if (shadow != null) Destroy(shadow);

            switch (move)
            {
                case Move.GroundSweep:
                    // 지상 쓸기 — 바닥에 붙은 납작한 판정이라 점프로 넘을 수 있다.
                    // 예전에는 반경 5의 원이라 가까이 있으면 점프해도 맞았다.
                    HitPlayersInBox(
                        new Vector2(transform.position.x, transform.position.y - 0.5f + sweepHeight * 0.5f),
                        new Vector2(sweepRange * 2f, sweepHeight));
                    break;

                case Move.Charge:
                {
                    // 감시탑 돌진 — 복원벽 뒤로 피하면 벽에 박고 큰 빈틈이 생긴다.
                    int dir = player.transform.position.x >= transform.position.x ? 1 : -1;
                    float elapsed = 0f;
                    while (elapsed < 1.2f)
                    {
                        _body.linearVelocity = new Vector2(dir * chargeSpeed, _body.linearVelocity.y);
                        if (Physics2D.Raycast(transform.position, Vector2.right * dir, 1f, obstacleMask))
                        {
                            _body.linearVelocity = Vector2.zero;
                            Telegraph(true);          // 경직도 눈에 보이게
                            yield return new WaitForSeconds(1.8f);
                            Telegraph(false);
                            yield break;
                        }
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
                    break;
                }

                case Move.Slam:
                {
                    // 상부 낙하 — 그림자를 보고 좌우로 비킨다. 예고 동안 위치가 고정이라 확실히 피할 수 있다.
                    var target = new Vector3(player.transform.position.x, transform.position.y + slamHeight, 0f);
                    transform.position = target;
                    yield return new WaitForSeconds(0.6f);
                    _body.linearVelocity = new Vector2(0f, -18f);
                    yield return new WaitForSeconds(0.5f);
                    HitPlayersInCircle(transform.position, sweepRange * 0.8f);
                    break;
                }

                case Move.GazeSweep:
                {
                    // 홍채 훑기 — 시선이 바닥을 한 방향으로 지나간다. 숨거나(숨죽이기) 훑는
                    // 선 바깥으로 비키면(엄폐·이동) 둘 다 정답이 되도록, 판정은 좁은 세로
                    // 띠 하나가 천천히 움직이는 형태다.
                    int direction = player.transform.position.x >= transform.position.x ? 1 : -1;
                    float distance = sweepRange * 2f;
                    float elapsed = 0f;
                    var sweep = ShowDropShadow(transform.position);

                    while (elapsed < gazeSweepSeconds)
                    {
                        float t = elapsed / gazeSweepSeconds;
                        float x = transform.position.x + direction * distance * t;
                        var center = new Vector2(x, transform.position.y - 0.5f);

                        if (sweep != null)
                        {
                            sweep.transform.position = new Vector3(x, transform.position.y - 1.1f, 0f);
                            sweep.transform.localScale = new Vector3(gazeSweepWidth, 0.4f, 1f);
                        }

                        var seen = Physics2D.OverlapBox(center, new Vector2(gazeSweepWidth, sweepHeight), 0f, gazeMask);
                        if (seen != null)
                        {
                            var health = seen.GetComponentInParent<PlayerHealth>();
                            if (health != null) health.TakeDamage(_self.Data.contactDamage, center);
                        }

                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (sweep != null) Destroy(sweep);
                    break;
                }

                case Move.WallClose:
                {
                    // 눈꺼풀 닫기 — 좌우 벽이 안쪽으로 좁아지고 중앙 안전지대만 남는다.
                    // 피해 판정이 없다. 기본 이동만으로 대응할 수 있어야 한다는 명세대로,
                    // 위험은 "이 동안 다른 공격을 피할 공간이 줄어든다"는 것뿐이다.
                    if (closingWalls == null || closingWalls.Length == 0) break;

                    var origins = new Vector3[closingWalls.Length];
                    for (int i = 0; i < closingWalls.Length; i++)
                        if (closingWalls[i] != null) origins[i] = closingWalls[i].position;

                    yield return MoveWalls(origins, wallCloseDistance, 0.4f);
                    yield return new WaitForSeconds(wallCloseHoldSeconds);
                    yield return MoveWalls(origins, 0f, 0.6f);
                    break;
                }

                case Move.TimeSkip:
                {
                    // 시간 건너뛰기 — 지금 위치에서 사라지고, 예고해 둔 자리에 나타나 떨어진다.
                    // 사라져 있는 동안 위치가 바뀌지 않으므로 예지 고스트와 실제가 어긋나지 않는다.
                    var mark = ShowDropShadow(skipTarget);
                    if (_sprite != null) _sprite.enabled = false;
                    _body.linearVelocity = Vector2.zero;

                    yield return new WaitForSeconds(timeSkipLead);

                    transform.position = new Vector3(skipTarget.x, skipTarget.y + slamHeight, 0f);
                    if (_sprite != null) _sprite.enabled = true;
                    if (mark != null) Destroy(mark);

                    _body.linearVelocity = new Vector2(0f, -18f);
                    yield return new WaitForSeconds(0.5f);
                    HitPlayersInCircle(transform.position, sweepRange * 0.8f);
                    break;
                }
            }
        }

        // 좌우 벽을 안쪽으로 inset만큼 옮긴다. 0을 주면 원래 자리로 돌아온다.
        IEnumerator MoveWalls(Vector3[] origins, float inset, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                float t = seconds <= 0f ? 1f : elapsed / seconds;
                for (int i = 0; i < closingWalls.Length; i++)
                {
                    if (closingWalls[i] == null) continue;

                    // 전장 중심(보스 기준)을 향해 좁힌다.
                    float direction = origins[i].x >= transform.position.x ? -1f : 1f;
                    var target = origins[i] + new Vector3(direction * inset, 0f, 0f);
                    closingWalls[i].position = Vector3.Lerp(closingWalls[i].position, target, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 떨어질 자리를 바닥에 그려 준다. 예고 동안 위치가 고정이라 비키면 확실히 피한다.
        GameObject ShowDropShadow(Vector3 target)
        {
            if (shadowSprite == null) return null;

            var go = new GameObject("BossDropShadow");
            go.transform.position = new Vector3(target.x, target.y - 0.6f, 0f);
            go.transform.localScale = new Vector3(3f, 0.4f, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = shadowSprite;
            renderer.color = new Color(0f, 0f, 0f, 0.55f);
            renderer.sortingOrder = 4;
            return go;
        }

        void HitPlayersInBox(Vector2 center, Vector2 size)
        {
            var hit = Physics2D.OverlapBox(center, size, 0f, playerMask);
            if (hit == null) return;

            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(_self.Data.contactDamage, center);
        }

        void HitPlayersInCircle(Vector2 center, float radius)
        {
            var hit = Physics2D.OverlapCircle(center, radius, playerMask);
            if (hit == null) return;

            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(_self.Data.contactDamage, center);
        }

        void Telegraph(bool on)
        {
            if (_sprite == null) return;
            _sprite.color = on ? Color.Lerp(_self.Data.tint, Color.white, 0.7f) : _self.Data.tint;
        }
    }
}
