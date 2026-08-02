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
    }
}
