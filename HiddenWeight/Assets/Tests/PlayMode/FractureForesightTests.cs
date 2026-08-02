using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;
using HiddenWeight.World;
using HiddenWeight.Emotions;

namespace HiddenWeight.Tests
{
    // 예지를 실제로 발동해 무엇이 만들어지는지 본다.
    //
    // 예전 구현은 대상의 스프라이트와 루트 localScale을 그대로 복제했다. 큰 환경
    // 오브젝트가 하나라도 걸리면 화면 상단을 덮는 거대한 그림이 떠올랐고, 겉모습이
    // 자식으로 옮겨진 적은 루트의 플레이스홀더 사각형이 고스트가 됐다.
    // 눈으로만 확인하면 다음에 조용히 되돌아온다 — 크기와 개수를 수치로 못 박는다.
    public class FractureForesightTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            PlayerInput.Enabled = true;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator 예지는_화면을_덮지_않고_실루엣만_남긴다()
        {
            // F06 시차 온실 — 이동 발판과 선행 그림자가 함께 있어 예지 대상이 가장 많다.
            yield return RoomTestHarness.EnterRoom("Fracture", "F06");
            Time.timeScale = 1f;

            var gm = GameManager.Instance;
            gm.Progress.UnlockSkill(EmotionId.Foresight);
            EmotionSkillController.Instance.RefreshActive();
            yield return null;

            var skill = EmotionSkillController.Instance.Active;
            Assert.IsNotNull(skill, "예지가 활성 스킬이 되지 않았다.");
            Assert.AreEqual(EmotionId.Foresight, skill.Id);

            skill.Begin();
            yield return null;
            yield return null;

            int ghosts = 0, links = 0, breaking = 0;
            float biggest = 0f;
            var report = new StringBuilder("[예지 발동 결과]\n");

            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                var root = sr.transform;
                while (root.parent != null) root = root.parent;
                bool isGhost = sr.name == "ForesightGhost" || root.name == "ForesightGhost";
                bool isDash = root.name == "ForesightBreaking";
                if (!isGhost && !isDash) continue;

                float size = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                if (isGhost) { ghosts++; biggest = Mathf.Max(biggest, size); }
                else breaking++;
                report.AppendLine($"  {root.name}/{sr.name} 크기{sr.bounds.size:F1} "
                                  + $"a={sr.color.a:F2} 스프라이트={(sr.sprite == null ? "-" : sr.sprite.name)}");
            }
            foreach (var line in Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include))
                if (line.name == "ForesightLink") links++;

            report.AppendLine($"  고스트 {ghosts} / 연결선 {links} / 끊긴외곽 {breaking} / 최대 크기 {biggest:F1}");
            Debug.Log(report.ToString());

            Assert.Greater(ghosts + breaking, 0, "예지가 아무것도 만들지 않았다.\n" + report);
            // 화면을 덮던 거대 고스트가 다시 생기면 여기서 걸린다.
            Assert.LessOrEqual(biggest, 8f,
                "예지 고스트가 화면을 덮을 만큼 크다.\n" + report);
            // 강조 대상은 소수여야 한다(설계 7.2 공정성 규칙).
            Assert.LessOrEqual(links, 3, "연결선이 너무 많다.\n" + report);

            skill.End();
            yield return null;
            Assert.IsNull(GameObject.Find("ForesightGhost"), "예지가 끝났는데 고스트가 남았다.");
        }

        // 균열의 핵심 약속이다(설계 7.2 공정성 규칙):
        // "예지 고스트가 보여준 위치와 실제 2초 뒤 위치는 항상 일치한다."
        //
        // 이게 깨지면 플레이어는 정확한 정보를 받았다고 믿고 움직였다가 맞는다 —
        // 어려운 게 아니라 부당해진다. 지역 전체가 이 약속 위에 서 있는데 여태
        // 아무도 확인한 적이 없어, 예측과 실제를 직접 대 본다.
        [UnityTest]
        public IEnumerator 예지가_보여준_위치와_실제_2초_뒤가_일치한다(
            [Values("F04", "F06", "F07", "F09")] string room)
        {
            yield return RoomTestHarness.EnterRoom("Fracture", room);
            Time.timeScale = 1f;
            yield return new WaitForFixedUpdate();

            const float lead = 2f;      // Emotion_Foresight.previewLeadTime
            const float tolerance = 0.35f;

            var targets = new System.Collections.Generic.List<(IForeseeable f, Vector3 predicted, string name)>();
            foreach (var mono in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if (mono is not IForeseeable f) continue;
                if (!f.PredictActive(lead)) continue;
                targets.Add((f, f.PredictPosition(lead), mono.GetType().Name + " @ " + mono.name));
            }

            if (targets.Count == 0) yield break;   // 예지 대상이 없는 방은 검사할 것이 없다

            float waited = 0f;
            while (waited < lead)
            {
                waited += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            var wrong = new StringBuilder();
            foreach (var (f, predicted, name) in targets)
            {
                if (f == null || f.Transform == null) continue;
                float error = Mathf.Abs(f.Transform.position.x - predicted.x);
                if (error > tolerance)
                {
                    var body = f.Transform.GetComponent<Rigidbody2D>();
                    var mb = f as MonoBehaviour;
                    var touching = new System.Collections.Generic.List<Collider2D>();
                    var col = f.Transform.GetComponent<Collider2D>();
                    if (col != null) col.GetContacts(touching);
                    var names = new System.Collections.Generic.List<string>();
                    foreach (var c in touching) if (c != null) names.Add(c.name);

                    wrong.AppendLine($"    {name}: 예측 x={predicted.x:F2}, 실제 x={f.Transform.position.x:F2} "
                                     + $"(어긋남 {error:F2}) 속도={(body != null ? body.linearVelocity.ToString("F2") : "-")} "
                                     + $"컴포넌트켜짐={(mb != null && mb.isActiveAndEnabled)} "
                                     + $"닿은것=[{string.Join(",", names)}]");
                }
            }

            Debug.Log($"[{room}] 예지 대상 {targets.Count}개 검사, 어긋남 "
                      + (wrong.Length == 0 ? "없음" : "있음\n" + wrong));
            Assert.IsEmpty(wrong.ToString(),
                $"{room}: 예지가 보여준 위치와 실제 2초 뒤가 다르다 — 지역의 공정성 규칙이 깨진다.\n" + wrong);
        }
    }
}
