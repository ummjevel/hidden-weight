using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.Tests
{
    // 균열에서 지도를 열었을 때 실제로 무엇이 적히는지 본다.
    //
    // 예전 지도는 지역과 무관하게 "R01 → R12", "R04 분기", 그리고 잔재의 방 이름
    // ("입구 경계", "애도교" …)을 그대로 찍었다. 균열을 걷고 있는데 화면에는 잔재가 적혀
    // 있었던 것이다. 노드 개수만 세는 검사로는 이 결함이 잡히지 않아, 여기서는 문구를 읽는다.
    public class FractureMapUiTests
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
        public IEnumerator 균열_지도는_균열의_방을_보여준다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F01");

            var gm = GameManager.Instance;
            gm.EnterZone(ZoneId.Fracture);
            gm.Progress.VisitRoom("Fracture/FractureRoom01");
            gm.SetState(GameState.Playing);
            yield return null;

            var pause = Object.FindAnyObjectByType<PauseMenu>();
            Assert.IsNotNull(pause, "일시정지 메뉴가 없다.");
            pause.OpenSection(PauseSection.Map);
            yield return null;
            yield return null;

            var seen = new StringBuilder("[균열 지도 문구]\n");
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude))
            {
                if (!text.enabled || string.IsNullOrWhiteSpace(text.text)) continue;
                if (text.text.Length > 80) continue;      // 본문 목록은 건너뛴다
                seen.AppendLine("  " + text.text.Replace("\n", " / "));
            }
            Debug.Log(seen.ToString());

            string all = seen.ToString();
            Assert.IsTrue(all.Contains("F01"), "지도에 균열 방 기호(F01)가 없다.\n" + all);
            Assert.IsTrue(all.Contains("유리 정원"), "지도에 균열 방 이름이 없다.\n" + all);
            Assert.IsFalse(all.Contains("애도교"), "지도에 잔재 방 이름이 섞여 있다.\n" + all);
            Assert.IsFalse(all.Contains("R01 → R12"), "지도 제목이 아직 잔재 기준이다.\n" + all);
        }
    }
}
