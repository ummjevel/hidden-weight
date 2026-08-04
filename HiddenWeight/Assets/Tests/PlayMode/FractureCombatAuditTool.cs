using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 균열의 적을 실제로 걸어가서 때려 본다.
    //
    // 기존 공격 검사(AttackSanityTests)는 잔재에서 적을 **순간이동으로 코앞에 세워 두고**
    // 때린다. 그래서 "다가가서 때리면 맞는가"는 한 번도 확인된 적이 없다 — 플레이어가
    // 실제로 겪는 것은 그쪽이다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.FractureCombatAuditTool"
    [Explicit]
    public class FractureCombatAuditTool
    {
        // 적이 있는 방만 본다(FractureEnemyAuditTool 결과).
        static readonly string[] Rooms = { "F02", "F03", "F04", "F07" };

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            PlayerInput.Enabled = true;
            Time.timeScale = 1f;
        }

        // 죽인 적은 사라져야 한다.
        //
        // 사망 클립을 새로 넣자 죽는 경로가 "즉시 사라짐"에서 "연출을 끝까지 보고 사라짐"으로
        // 바뀌었다. 그런데 순찰이 매 프레임 걷기 클립을 다시 틀어 사망 클립이 영영 끝나지
        // 않았고, 죽은 적이 화면에 그대로 남았다.
        [UnityTest]
        public IEnumerator 죽인_적은_사라진다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F02");
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;
            for (int i = 0; i < 10; i++) yield return null;

            Enemy target = null;
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                if (enemy.isActiveAndEnabled && enemy.IsAlive) { target = enemy; break; }
            Assert.IsNotNull(target, "F02에 적이 없다.");

            int maxHealth = target.Data.maxHealth;
            for (int i = 0; i < maxHealth + 2 && target.IsAlive; i++)
            {
                target.TakeDamage(1, (Vector2)target.transform.position + Vector2.left);
                yield return null;
            }
            Assert.IsFalse(target.IsAlive, $"체력 {maxHealth}만큼 때렸는데 아직 살아 있다.");

            float deadline = Time.time + 3f;
            while (target.gameObject.activeSelf && Time.time < deadline) yield return null;

            var animator = target.GetComponentInChildren<SpriteAnimator>(true);
            Debug.Log($"[사망] 사라짐={!target.gameObject.activeSelf} "
                      + $"마지막 클립={animator?.CurrentClip ?? "없음"}");
            Assert.IsFalse(target.gameObject.activeSelf,
                "죽은 적이 3초가 지나도 화면에 그대로 남아 있다.");
        }

        // 적이 하나도 없어도 휘두르기는 나가야 한다.
        //
        // 스윙 연출은 Attacked 이벤트를 구독하는 쪽이 만든다. 그 호출이 판정 루프 **뒤에**
        // 있으면, 판정에서 무엇 하나만 어긋나도 공격이 화면에 아예 나타나지 않는다 —
        // 플레이어에게는 "눌렀는데 아무 일도 없다"로 보인다.
        [UnityTest]
        public IEnumerator 적이_없어도_휘두르기는_나간다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F01");   // F01에는 적이 없다
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;
            for (int i = 0; i < 10; i++) yield return null;

            var player = PlayerController.Instance;
            Assert.IsNotNull(player, "플레이어가 없다.");
            var attack = player.GetComponent<PlayerAttack>();
            Assert.IsNotNull(attack, "PlayerAttack이 없다.");
            Assert.IsTrue(attack.CanAttack, "공격이 잠겨 있다(CanAttack=false).");

            int swings = 0;
            void Count() => swings++;
            attack.Attacked += Count;

            bool sawAttackState = false;
            for (int frame = 0; frame < 90; frame++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { attackPressed = frame % 30 == 0 };
                yield return new WaitForFixedUpdate();
                if (player.State == PlayerState.Attack) sawAttackState = true;
            }
            attack.Attacked -= Count;

            Debug.Log($"[헛스윙] 휘두른 횟수 {swings} · Attack 상태 진입 {sawAttackState}");
            Assert.Greater(swings, 0, "적이 없다고 휘두르기 자체가 안 나갔다.");
            Assert.IsTrue(sawAttackState, "공격했는데 PlayerState가 Attack으로 바뀌지 않았다.");
        }

        // 적이 어느 자리에 있을 때 공격이 닿는가를 표로 만든다.
        //
        // "붙어 있는데 안 맞는다"는 보고가 계속 나왔다. 사거리 안이면 맞을 것이라고 믿고
        // 거리만 재던 것이 문제였다 — 실제 판정에는 **각도**도 있고, 낮은 적은 중심이
        // 발목 높이라 바짝 붙을수록 각도 밖으로 밀려난다.
        [UnityTest]
        public IEnumerator 공격이_닿는_자리를_표로_만든다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F02");
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;
            for (int i = 0; i < 10; i++) yield return null;

            var player = PlayerController.Instance;
            Enemy target = null;
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                if (enemy.isActiveAndEnabled && enemy.IsAlive) { target = enemy; break; }
            Assert.IsNotNull(player, "플레이어가 없다.");
            Assert.IsNotNull(target, "F02에 적이 없다.");

            // 적을 세워 두고 자리만 바꾼다. 물리로 밀려나지 않게 정지시킨다.
            var patrol = target.GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
            foreach (var behavior in target.GetComponents<EnemyBehavior>()) behavior.enabled = false;
            var targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null) targetBody.bodyType = RigidbodyType2D.Kinematic;
            var attack = player.GetComponent<PlayerAttack>();

            (string label, Vector2 offset)[] spots =
            {
                ("정면 0.4",        new Vector2( 0.4f,  0.0f)),
                ("정면 0.9",        new Vector2( 0.9f,  0.0f)),
                ("정면 1.1",        new Vector2( 1.1f,  0.0f)),
                ("정면 1.6(사거리밖)", new Vector2( 1.6f,  0.0f)),
                ("발밑 겹침",        new Vector2( 0.0f, -0.5f)),
                ("발밑 살짝 앞",      new Vector2( 0.2f, -0.6f)),
                ("완전 겹침",        new Vector2( 0.0f,  0.0f)),
                ("정면 아래 대각",     new Vector2( 0.7f, -0.7f)),
                ("정면 위 대각",      new Vector2( 0.7f,  0.7f)),
                ("머리 위",          new Vector2( 0.0f,  0.9f)),
                ("등 뒤 0.9",        new Vector2(-0.9f,  0.0f)),
                ("등 뒤 발밑",        new Vector2(-0.2f, -0.6f)),
            };

            var report = new StringBuilder("[공격이 닿는 자리]\n");
            var missedClose = new List<string>();

            foreach (var (label, offset) in spots)
            {
                target.ResetForEncounter();
                var basePos = (Vector2)player.transform.position;
                target.transform.position = basePos + offset;
                if (targetBody != null) targetBody.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return null;

                int before = target.Health;
                // 둘 다 매 프레임 제자리에 붙들어 둔다. 그러지 않으면 플레이어가 입력을
                // 따라 걸어가 버려서 "1.6(사거리 밖)"이 맞는 것으로 나온다 — 실제로 그랬다.
                for (int frame = 0; frame < 30 && target.Health >= before; frame++)
                {
                    PlayerInput.Injected = new PlayerInput.Frame
                    {
                        horizontal = 0.05f,          // 오른쪽을 보게만 한다
                        attackPressed = frame % 12 == 0,
                    };
                    player.TeleportTo(basePos);
                    target.transform.position = basePos + offset;
                    Physics2D.SyncTransforms();
                    yield return new WaitForFixedUpdate();
                }

                bool hit = target.Health < before;
                report.AppendLine($"    {label,-18} → {(hit ? "맞음" : "빗나감")}");

                // 사거리 안(1.2)이고 등 뒤가 아닌데 안 맞으면 결함이다.
                if (!hit && offset.magnitude <= 1.2f && offset.x >= -0.05f)
                    missedClose.Add(label);
            }

            Debug.Log(report.ToString());
            Assert.IsEmpty(missedClose,
                "사거리 안 정면인데 안 맞는 자리: " + string.Join(", ", missedClose));
        }

        // 맞았을 때 화면에서 무엇이 달라지는가. 세 가지가 동시에 일어나야 "맞았다"가 읽힌다:
        // 색이 바뀌고(번쩍임), 몸이 밀리고(반동), 피격 그림이 뜬다.
        [UnityTest]
        public IEnumerator 적이_맞으면_눈에_보이게_반응한다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F02");
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;
            for (int i = 0; i < 10; i++) yield return null;

            Enemy target = null;
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                if (enemy.isActiveAndEnabled && enemy.IsAlive) { target = enemy; break; }
            Assert.IsNotNull(target, "F02에 적이 없다.");

            var animator = target.GetComponentInChildren<SpriteAnimator>();
            var art = animator != null ? animator.Renderer : target.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(art, "적에게 보이는 렌더러가 없다.");

            var tint = target.Data.tint;
            var home = art.transform.localPosition;

            target.TakeDamage(1, (Vector2)target.transform.position + Vector2.left * 1.2f);

            float maxColorShift = 0f;
            float maxOffset = 0f;
            string clip = "없음";
            for (int frame = 0; frame < 40; frame++)
            {
                var c = art.color;
                maxColorShift = Mathf.Max(maxColorShift,
                    Mathf.Abs(c.r - tint.r) + Mathf.Abs(c.g - tint.g) + Mathf.Abs(c.b - tint.b));
                maxOffset = Mathf.Max(maxOffset,
                    Vector3.Distance(art.transform.localPosition, home));
                // 피격 클립은 0.25초 뒤 걷기로 돌아간다. 마지막 값을 보면 늘 Walk다 —
                // "한 번이라도 떴는가"를 봐야 한다.
                if (animator != null && animator.CurrentClip != null
                    && animator.CurrentClip.EndsWith("Hit")) clip = animator.CurrentClip;
                yield return null;
            }

            Debug.Log($"[피격 반응] 색 변화 {maxColorShift:F2} · 반동 {maxOffset:F2} · 클립 {clip}");
            Assert.Greater(maxColorShift, 0.25f, "번쩍임이 원래 색과 거의 같다 — 맞은 게 안 보인다.");
            Assert.Greater(maxOffset, 0.05f, "맞았는데 그림이 전혀 움직이지 않는다.");
            Assert.IsTrue(clip.EndsWith("Hit"), $"피격 클립이 재생되지 않았다(지금 {clip}).");
        }

        [UnityTest]
        public IEnumerator 균열_적을_걸어가서_때려본다()
        {
            var report = new StringBuilder("[균열 전투 감사]\n");
            var balance = GameManager.Instance != null ? GameManager.Instance.Balance : null;
            if (balance != null)
                report.AppendLine($"  공격 반경 {balance.player.attackRadius} "
                                  + $"각도 {balance.player.attackAngle} "
                                  + $"피해 {balance.player.attackDamage} "
                                  + $"쿨타임 {balance.player.attackCooldown}");

            foreach (var room in Rooms)
            {
                yield return RoomTestHarness.EnterRoom("Fracture", room);
                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
                Time.timeScale = 1f;
                for (int i = 0; i < 10; i++) yield return null;

                var player = PlayerController.Instance;
                Enemy target = null;
                foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                    if (enemy.isActiveAndEnabled && enemy.IsAlive) { target = enemy; break; }

                if (player == null || target == null)
                {
                    report.AppendLine($"  ── {room}: 때릴 적이 없다");
                    continue;
                }

                // 적이 걸어 다니면 사거리 판정이 흔들린다. 세워 두고 **플레이어만** 걷게 한다.
                var patrol = target.GetComponent<EnemyPatrol>();
                if (patrol != null) patrol.enabled = false;
                foreach (var behavior in target.GetComponents<EnemyBehavior>()) behavior.enabled = false;
                var targetBody = target.GetComponent<Rigidbody2D>();
                if (targetBody != null) targetBody.linearVelocity = Vector2.zero;

                // 적 왼쪽 6유닛에서 시작해 걸어서 접근한다.
                player.TeleportTo((Vector2)target.transform.position + new Vector2(-6f, 0.6f));
                for (int i = 0; i < 20; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

                int before = target.Health;
                float closest = float.MaxValue;
                bool sawAttackState = false;
                bool destroyed = false;

                for (int frame = 0; frame < 300; frame++)
                {
                    if (target == null) { destroyed = true; break; }

                    float gap = Vector2.Distance(player.transform.position, target.transform.position);
                    closest = Mathf.Min(closest, gap);

                    // 사거리 밖이면 걸어가고, 안이면 멈춰서 때린다.
                    bool inRange = gap <= (balance != null ? balance.player.attackRadius : 1.2f);
                    PlayerInput.Injected = new PlayerInput.Frame
                    {
                        horizontal = inRange ? 0.05f : 0.8f,
                        attackPressed = inRange && frame % 25 == 0,
                    };
                    yield return new WaitForFixedUpdate();
                    if (player.State == PlayerState.Attack) sawAttackState = true;
                    if (target != null && target.Health < before) break;
                }

                int after = target == null ? 0 : target.Health;
                var problems = new List<string>();
                if (!destroyed && after >= before) problems.Add("체력이 안 깎임");
                if (!sawAttackState) problems.Add("Attack 상태로 안 바뀜");
                if (closest > (balance != null ? balance.player.attackRadius : 1.2f))
                    problems.Add($"사거리 안으로 못 들어감(최소 거리 {closest:F2})");
                if (target != null && target.GetComponent<IGuard>() != null)
                    problems.Add("방어형(정면 공격이 막힌다)");

                // Unity 오브젝트에 ?. 를 쓰면 안 된다. C#의 null 검사라 파괴된 오브젝트를
                // 통과시켜 .name에서 MissingReferenceException이 난다.
                string targetName = target == null ? "(쓰러짐)" : target.name;
                report.AppendLine(
                    $"  ── {room}: {targetName} 체력 {before}→{after} "
                    + $"최소거리 {closest:F2} 공격상태 {sawAttackState} | "
                    + (problems.Count == 0 ? "정상" : string.Join(", ", problems)));
            }

            Debug.Log(report.ToString());
        }
    }
}
