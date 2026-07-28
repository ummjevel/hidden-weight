using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 감정 스킬이 "눌러도 아무 일도 안 일어나는" 상태로 돌아가지 않게 막는다.
    //
    // 실제로 있었던 버그: EmotionSkill 베이스가 Awake에서 PlayerController.Instance를 캡처했는데,
    // 같은 GameObject 안의 컴포넌트 Awake 순서가 보장되지 않아 null이 잡혔다. 그 뒤로 영원히
    // null이라 Begin()이 ApplySpeedMultiplier에서 NullReference로 죽었고, 되감기·숨죽이기·예지
    // 세 스킬이 전부 조용히 먹통이었다. 예외는 콘솔에만 찍히고 화면에는 아무 표시도 없었다.
    public class EmotionSkillTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [UnityTest]
        public IEnumerator 감정_스킬_전부가_플레이어_참조를_얻는다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            Assert.IsNotNull(player, "씬에 PlayerController가 없다.");

            var skills = player.GetComponents<EmotionSkill>();
            Assert.IsNotEmpty(skills, "플레이어에 EmotionSkill이 하나도 없다.");

            // 컨트롤러가 끼어들어 스킬을 임의로 켜고 끄지 않게 떼어 둔다.
            EmotionSkillController.Instance.enabled = false;

            foreach (var skill in skills)
            {
                // Begin()은 Player가 null이면 ApplySpeedMultiplier에서 터진다. 그래서 스킬을
                // 실제로 켜 보는 것이 참조가 살아있는지 확인하는 가장 정직한 방법이다.
                skill.Data = GameManager.Instance.Balance.GetEmotion(skill.Id);
                Assert.DoesNotThrow(() => skill.Begin(),
                    skill.GetType().Name + ": Begin()이 예외로 죽는다 — Player 참조가 null이다.");
                Assert.DoesNotThrow(() => skill.End(),
                    skill.GetType().Name + ": End()가 예외로 죽는다 — Player 참조가 null이다.");
            }

            // 다음 테스트가 물려받지 않도록 이 테스트가 건드린 전역 상태를 되돌린다.
            player.MovementLocked = false;
            player.ExternalSpeedMultiplier = 1f;
        }

        [UnityTest]
        public IEnumerator 되감기가_밀려난_대상을_원위치로_되돌린다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var report = new StringBuilder();
            var gm = GameManager.Instance;
            var player = PlayerController.Instance;
            var controller = EmotionSkillController.Instance;

            // 1. 파편을 밟아 되감기를 해금한다(실제 픽업 경로 그대로).
            // id로 집는다. FindFirstObjectByType은 순서를 보장하지 않아서 게이트 뒤의
            // 숨겨진 파편(획득 조건이 걸려 있다)이 잡힐 수 있다.
            var fragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                f => f.FragmentId == "residue_skill");
            Assert.IsNotNull(fragment, "잔재 지역에 스킬 해금용 파편(residue_skill)이 없다.");

            // 트리거가 실제로 들어오게 한 칸 옆에서 파편 위치로 이동시킨다. 텔레포트 직후
            // 곧바로 겹쳐 있으면 Enter가 아니라 Stay로 처리되는 경우가 있어 한 번 떼었다 붙인다.
            player.TeleportTo(fragment.transform.position + new Vector3(3f, 0f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(fragment.transform.position);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(gm.Progress.HasSkill(EmotionId.Rewind),
                "파편을 밟았는데 Rewind가 해금되지 않았다. 파편 활성 상태="
                + fragment.gameObject.activeSelf + ", 파편 위치=" + fragment.transform.position
                + ", 플레이어 위치=" + player.transform.position
                + ", 플레이어 레이어=" + LayerMask.LayerToName(player.gameObject.layer));

            yield return null;
            var skill = controller.Active;
            Assert.IsNotNull(skill, "되감기를 해금했는데 활성 스킬이 지정되지 않았다.");
            Assert.AreEqual(EmotionId.Rewind, skill.Id);

            // 2. 블록이 중력으로 떨어져 되돌릴 거리가 생길 때까지 기다린다.
            var blocks = Object.FindObjectsByType<Rewindable>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(blocks, "잔재 지역에 Rewindable이 하나도 없다.");
            for (int i = 0; i < 150; i++) yield return new WaitForFixedUpdate();

            // 3. 무너진 다리 왼쪽 끝에 서서 되감는다.
            // 밀려난 대상 바로 옆에 선다(방 배치가 바뀌어도 따라간다).
            Rewindable nearest = null;
            foreach (var b in blocks) if (b.CanRewind) { nearest = b; break; }
            if (nearest != null) player.TeleportTo(nearest.transform.position + new Vector3(-1.5f, 0.5f, 0f));
            yield return new WaitForFixedUpdate();

            // 스킬은 "가장 가까운 대상"을 고른다. 테스트도 같은 규칙으로 골라야 한다 —
            // 임의의 블록을 집으면 스킬이 되감은 것과 다른 블록을 검사하게 된다.
            Rewindable target = null;
            float bestDistance = float.MaxValue;
            foreach (var b in blocks)
            {
                if (!b.CanRewind) continue;
                float d = Vector2.Distance(player.transform.position, b.transform.position);
                if (d < bestDistance) { bestDistance = d; target = b; }
            }
            Assert.IsNotNull(target, "블록이 하나도 밀려나지 않아 되감을 대상이 없다.");
            var displaced = target.transform.position;
            report.AppendLine("플레이어 " + player.transform.position.ToString("F2")
                + " / 대상 " + displaced.ToString("F2")
                + " / 거리 " + Vector2.Distance(player.transform.position, displaced).ToString("F2")
                + " / range " + skill.Data.range);

            // 컨트롤러는 매 프레임 "K를 안 누르고 있다"고 보고 채널링을 끊는다. 레거시 Input을
            // 테스트에서 흉내낼 수 없으므로 컨트롤러를 떼고 스킬만 직접 돌린다.
            controller.enabled = false;
            skill.Begin();
            Assert.IsTrue(skill.IsActive, "Begin() 했는데 스킬이 활성화되지 않았다.\n" + report);

            for (float t = 0f; t < skill.Data.channelTime + 0.5f && skill.IsActive; t += Time.fixedDeltaTime)
            {
                skill.Tick(Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            report.AppendLine("되감기 후 대상 " + target.transform.position.ToString("F2")
                + " CanRewind=" + target.CanRewind);

            Assert.IsFalse(target.CanRewind,
                "채널링을 끝냈는데 대상이 원위치로 돌아오지 않았다.\n" + report);
            // 이동량으로 재차 확인하지 않는다. 어떤 블록이 잡히느냐에 따라(굴러떨어진 거리가
            // 제각각이다) 값이 흔들려 오탐만 낸다. "초기 위치로 돌아왔는가"는 위의 CanRewind가
            // 이미 정확히 판정한다.
        }
    }
}
