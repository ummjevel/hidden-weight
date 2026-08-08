using System.Collections;
using UnityEngine;
using HiddenWeight.Player;
using HiddenWeight.World;
using HiddenWeight.Core;

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
        //   Projectile: 예고 후 공격체를 쏜다. 무엇을 쏘는지는 projectileName이 정한다
        //               (잔재의 감시 파동·기억침·되감기 구체).
        public enum Move { GroundSweep, Charge, Slam, GazeSweep, WallClose, TimeSkip, Projectile }

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

        [Header("공격체")]
        [SerializeField] string projectileName = "BossWave";
        [SerializeField] float projectileTelegraph = 1.0f;
        // 단계가 오르면 한 번에 두 발을 쏜다. 예고 길이는 그대로 두고 발수만 늘린다.
        [SerializeField] float projectileBurstGap = 0.35f;

        [Header("보스 전용 연출")]
        // 낙하 착지와 단계 전환에 얹는 효과(ResidueBossProjectiles_v1의 낙하 고리·페이즈 파열).
        [SerializeField] string slamImpactEffect = "BossRing";
        [SerializeField] string phaseChangeEffect = "BossRupture";

        [Header("전투 애니메이션")]
        // moves와 나란한 배열. moves[i]를 시작할 때 moveClipNames[i]를 튼다. 비어 있거나
        // 클립이 시트에 없으면 조용히 넘어간다 — 아트가 덜 들어온 보스도 싸울 수는 있어야 한다.
        // 피격·사망은 여기 없다: Enemy가 clipPrefix+"Hit"/"Death"로 직접 재생한다.
        [SerializeField] string[] moveClipNames;
        [SerializeField] string idleClipName;
        [SerializeField] string phaseClipName;

        // 체력 비율이 이 값 아래로 내려가면 다음 단계. 명세의 R12 3단계(1.0 → 0.6 → 0.3)를 기본값으로.
        [SerializeField] float[] phaseThresholds = { 0.6f, 0.3f };

        Enemy _self;
        Rigidbody2D _body;
        SpriteRenderer _sprite;
        HiddenWeight.World.SpriteAnimator _animator;
        int _moveIndex;
        bool _showAttackRanges;
        GameObject _activeRangeIndicator;
        static Material _rangeMaterial;

        [Header("보스 애니메이션")]
        [SerializeField] string idleClip = "WatcherAnimIdle";
        [SerializeField] string sweepClip = "WatcherAnimSweep";
        [SerializeField] string chargeClip = "WatcherAnimCharge";
        [SerializeField] string slamClip = "WatcherAnimDrop";
        [SerializeField] string projectileClip = "WatcherAnimSweep";
        [SerializeField] string phaseClip = "WatcherAnimStun";
        [SerializeField] Rewindable[] arenaRewindables;

        public int Phase { get; private set; }
        public bool AttackRangesVisible => _showAttackRanges;

        void Awake()
        {
            _self = GetComponent<Enemy>();
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<HiddenWeight.World.SpriteAnimator>();
        }

        // 클립이 시트에 있으면 튼다. 피격 애니메이션(Enemy.PlayClip)이 재생 중일 수 있으므로
        // restart로 밀어 넣는다 — 공격 시작이 피격 잔상에 가려지면 예고를 읽을 수 없다.
        void PlayBossClip(string clipName)
        {
            if (_animator == null || string.IsNullOrEmpty(clipName)) return;
            if (_animator.Has(clipName)) _animator.Play(clipName, true);
        }

        public void ConfigurePresentation(string idle, string sweep, string charge, string slam,
                                          string projectile, string phase)
        {
            // 기존 씬이 아직 새 Instructor 시트로 재생성되지 않은 경우에는 Watcher 클립을
            // 유지한다. 없는 이름으로 덮어쓰면 보스가 전투 내내 정지 포즈가 되는 회귀가 난다.
            idleClip = AvailableOr(idle, idleClip);
            sweepClip = AvailableOr(sweep, sweepClip);
            chargeClip = AvailableOr(charge, chargeClip);
            slamClip = AvailableOr(slam, slamClip);
            projectileClip = AvailableOr(projectile, projectileClip);
            phaseClip = AvailableOr(phase, phaseClip);
            PlayClip(idleClip, true);
        }

        string AvailableOr(string requested, string fallback)
            => _animator != null && _animator.Has(requested) ? requested : fallback;

        public void ConfigureArena(params Rewindable[] rewindables) => arenaRewindables = rewindables;

        // 보스별 판정 도형 표시를 제어한다. 잔재 R10/R12는 false로 두어 준비 자세와
        // 애니메이션만으로 공격을 읽게 하고, 다른 지역은 각 빌더의 설정을 그대로 따른다.
        public void ConfigureAttackReadability(bool showRanges)
        {
            _showAttackRanges = showRanges;
            if (!showRanges) ClearAttackRange();
        }

        // 개별 보스의 난도 튜닝용이다. 기본 직렬화 값은 건드리지 않고 호출된 인스턴스에만
        // 적용하므로, R10을 완화해도 R12와 다른 지역 보스의 속도는 그대로 유지된다.
        public void ConfigureDifficulty(float recovery, float charge, float sweepWarning,
                                        float chargeWarning, float slamWarning,
                                        float projectileWarning, float range)
        {
            recoverSeconds = Mathf.Max(0.1f, recovery);
            chargeSpeed = Mathf.Max(0.1f, charge);
            sweepTelegraph = Mathf.Max(0.1f, sweepWarning);
            chargeTelegraph = Mathf.Max(0.1f, chargeWarning);
            slamTelegraph = Mathf.Max(0.1f, slamWarning);
            projectileTelegraph = Mathf.Max(0.1f, projectileWarning);
            sweepRange = Mathf.Max(0.5f, range);
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
                    int index = _moveIndex % moves.Length;
                    if (moveClipNames != null && index < moveClipNames.Length)
                        PlayBossClip(moveClipNames[index]);
                    yield return PerformMove(moves[index]);
                    _moveIndex++;
                }

                // 공격이 끝나면 대기 자세로 돌아와 회복 시간을 보낸다 — 마지막 공격의
                // 정지 프레임으로 굳어 있으면 "쉬는 틈"이 화면에서 읽히지 않는다.
                PlayBossClip(idleClipName);
                yield return new WaitForSeconds(recoverSeconds);
            }
        }

        void UpdatePhase()
        {
            float ratio = _self.Data.maxHealth <= 0 ? 1f : (float)_self.Health / _self.Data.maxHealth;
            int phase = 0;
            for (int i = 0; i < phaseThresholds.Length; i++)
                if (ratio <= phaseThresholds[i]) phase = i + 1;

            // 단계가 오르는 순간을 화면에 알린다. 패턴이 바뀌는 것을 수치가 아니라
            // 연출로 읽게 하는 것이 목적이다.
            if (phase != Phase)
            {
                HiddenWeight.World.ImpactVFX.Play(phaseChangeEffect, transform.position);
                // 공통 보스용 클립 배열과 잔재 보스의 동적 프레젠테이션을 모두 유지한다.
                // 해당 애니메이터에 없는 클립은 각 헬퍼가 조용히 건너뛴다.
                PlayBossClip(phaseClipName);
                AudioManager.Instance?.PlaySfx(SfxCue.BossPhase, 0.75f);
                PlayClip(phaseClip, true);
                if (phase > Phase && arenaRewindables != null && arenaRewindables.Length > 0)
                {
                    int index = Mathf.Clamp(phase - 1, 0, arenaRewindables.Length - 1);
                    var target = arenaRewindables[index];
                    if (target != null)
                    {
                        float direction = index % 2 == 0 ? -1f : 1f;
                        target.BreakForEncounter(new Vector2(direction * 3f, 5f));
                    }
                }
            }

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
                case Move.Projectile: telegraph = projectileTelegraph; break;
                case Move.WallClose: telegraph = wallCloseTelegraph; break;
                case Move.TimeSkip: telegraph = timeSkipTelegraph; break;
                default: telegraph = slamTelegraph; break;
            }

            // 낙하 계열은 떨어질 자리를 바닥 그림자로 먼저 보여준다 — 좌우로 비키면 피할 수
            // 있어야 한다. 시간 건너뛰기는 예고 시점의 위치를 그대로 쓴다(균열 7.2절:
            // 고스트가 보여준 위치와 실제가 항상 일치한다).
            Vector3 lockedTarget = player.transform.position;
            int lockedDirection = lockedTarget.x >= transform.position.x ? 1 : -1;
            GameObject shadow = null;
            if (move == Move.Slam || move == Move.TimeSkip)
                shadow = ShowDropShadow(lockedTarget);
            Vector3 skipTarget = lockedTarget;

            ClearAttackRange();
            if (_showAttackRanges)
                _activeRangeIndicator = ShowAttackRange(move, lockedTarget, lockedDirection);

            Telegraph(true);
            AudioManager.Instance?.PlaySfx(SfxCue.BossTelegraph, 0.45f);
            PlayMoveClip(move);
            yield return new WaitForSeconds(telegraph);
            Telegraph(false);
            ClearAttackRange();
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
                    int dir = lockedDirection;
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
                    var target = new Vector3(lockedTarget.x, transform.position.y + slamHeight, 0f);
                    transform.position = target;
                    yield return new WaitForSeconds(0.6f);
                    _body.linearVelocity = new Vector2(0f, -18f);
                    yield return new WaitForSeconds(0.5f);
                    HitPlayersInCircle(transform.position, sweepRange * 0.8f);
                    HiddenWeight.World.ImpactVFX.Play(slamImpactEffect, transform.position);
                    break;
                }

                case Move.Projectile:
                {
                    // 예고가 끝난 방향으로 쏜다. 쏜 뒤에는 따라오지 않으므로 옆으로 비키면 피한다.
                    int dir = lockedDirection;
                    int shots = Phase == 0 ? 1 : 2;

                    for (int i = 0; i < shots; i++)
                    {
                        HiddenWeight.World.ProjectileSpawner.Fire(projectileName,
                            transform.position + new Vector3(dir * 1.2f, 0f, 0f), new Vector2(dir, 0f));
                        if (i + 1 < shots) yield return new WaitForSeconds(projectileBurstGap);
                    }
                    break;
                }

                case Move.GazeSweep:
                {
                    // 홍채 훑기 — 시선이 바닥을 한 방향으로 지나간다. 숨거나(숨죽이기) 훑는
                    // 선 바깥으로 비키면(엄폐·이동) 둘 다 정답이 되도록, 판정은 좁은 세로
                    // 띠 하나가 천천히 움직이는 형태다.
                    //
                    // 엄폐: 판정 박스에 들어와도 보스와의 직선(origin→플레이어)이 지형
                    // (obstacleMask = Ground|Wall)에 막히면 맞지 않는다 — 엄폐 기둥 뒤에
                    // 서면 실제로 안전해진다(GazeHazard의 라인캐스트 차단과 같은 방식).
                    int direction = player.transform.position.x >= transform.position.x ? 1 : -1;
                    float distance = sweepRange * 2f;
                    float elapsed = 0f;
                    var sweep = ShowDropShadow(transform.position);
                    var origin = new Vector2(transform.position.x, transform.position.y - 0.5f);

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
                        if (seen != null && !Physics2D.Linecast(origin, seen.transform.position, obstacleMask))
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

            PlayClip(idleClip);
        }

        void PlayMoveClip(Move move)
        {
            switch (move)
            {
                case Move.GroundSweep: PlayClip(sweepClip, true); break;
                case Move.Charge: PlayClip(chargeClip, true); break;
                case Move.Slam:
                case Move.TimeSkip: PlayClip(slamClip, true); break;
                default: PlayClip(projectileClip, true); break;
            }
        }

        void PlayClip(string clip, bool restart = false)
        {
            if (_animator != null && !string.IsNullOrEmpty(clip) && _animator.Has(clip))
                _animator.Play(clip, restart);
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

        GameObject ShowAttackRange(Move move, Vector3 target, int direction)
        {
            switch (move)
            {
                case Move.GroundSweep:
                case Move.Charge:
                case Move.Projectile:
                    // 직사각형 선은 실제 게임 화면에서 디버그 판정처럼 보인다. 방향성 공격은
                    // 보스의 준비 자세와 점멸로만 예고하고, 정확한 지점이 필요한 낙하만 남긴다.
                    return null;

                case Move.Slam:
                case Move.TimeSkip:
                    return CreateRangeCircle("BossSlamRange", target, sweepRange * 0.8f);

                default:
                    return null;
            }
        }

        static LineRenderer CreateRangeLine(string name, int pointCount)
        {
            var go = new GameObject(name);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = pointCount;
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.startColor = new Color(1f, 0.58f, 0.16f, 0.92f);
            line.endColor = line.startColor;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.sortingOrder = 35;
            if (_rangeMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _rangeMaterial = new Material(shader)
                    {
                        name = "BossAttackRange_Runtime",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }
            if (_rangeMaterial != null) line.sharedMaterial = _rangeMaterial;
            return line;
        }

        static GameObject CreateRangeCircle(string name, Vector2 center, float radius)
        {
            const int points = 48;
            var line = CreateRangeLine(name, points);
            line.loop = true;
            for (int i = 0; i < points; i++)
            {
                float angle = Mathf.PI * 2f * i / points;
                line.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return line.gameObject;
        }

        void ClearAttackRange()
        {
            if (_activeRangeIndicator != null) Destroy(_activeRangeIndicator);
            _activeRangeIndicator = null;
        }

        void OnDisable() => ClearAttackRange();

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
