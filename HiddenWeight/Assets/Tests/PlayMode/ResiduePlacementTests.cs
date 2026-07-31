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
    // 배치물이 지형 안에 파묻히거나 허공에 뜬 채로 놓이지 않았는지, 그리고 되감기가 새 지역에서
    // 실제로 대상을 되돌리는지 확인한다.
    public class ResiduePlacementTests
    {
        const string SceneName = "Zone_Residue_Full";

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        static void ResetProgress()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
        }

        [UnityTest]
        public IEnumerator 배치물이_지형_안에_파묻혀_있지_않다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            int groundMask = LayerMask.GetMask("Ground", "Wall");
            var buried = new StringBuilder();
            int checkedCount = 0;

            // 플레이어가 상호작용해야 하는 것들. 지형에 박혀 있으면 닿을 수도, 볼 수도 없다.
            foreach (var mono in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool interactive = mono is Rewindable || mono is CurrencyPickup || mono is HealingPickup
                                || mono is StoryFragment || mono is Checkpoint || mono is RewardChest;
                if (!interactive) continue;

                checkedCount++;
                // 자기 자신(되감기 블록은 Ground 레이어다)과 자식 콜라이더는 빼고 본다.
                var hits = Physics2D.OverlapPointAll(mono.transform.position, groundMask);
                Collider2D hit = null;
                foreach (var candidate in hits)
                    if (!candidate.transform.IsChildOf(mono.transform)) { hit = candidate; break; }

                if (hit != null)
                    buried.AppendLine("  " + mono.GetType().Name + " " + mono.name
                        + " @ " + mono.transform.position.ToString("F1")
                        + " ← " + hit.name + "(" + LayerMask.LayerToName(hit.gameObject.layer) + ")"
                        + " bounds=" + hit.bounds.ToString("F1"));
            }

            Debug.Log("===== 배치 검사 ===== 검사 " + checkedCount + "개 / 파묻힘 "
                + (buried.Length == 0 ? "0" : "발견") + "\n" + buried);

            Assert.IsTrue(buried.Length == 0,
                "지형 안에 파묻힌 배치물이 있다 — 플레이어가 닿을 수 없다.\n" + buried);
        }

        [UnityTest]
        public IEnumerator 벽타기_굴뚝의_충돌면이_눈에_보인다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            foreach (string wallName in new[]
                     {
                         "R04_Chimney_L", "R04_Chimney_R",
                         "R08_Chimney_L", "R08_Chimney_R",
                         "R12_Wall_L", "R12_Wall_R",
                     })
            {
                var wall = GameObject.Find(wallName);
                Assert.IsNotNull(wall, wallName + " 벽 충돌체가 없다.");
                var visual = wall.transform.Find("WallClimbSurfaces_Runtime");
                Assert.IsNotNull(visual, wallName + "에 보이는 벽타기 표면이 없다.");
                Assert.IsNotNull(visual.Find("WallClimbSurface"), wallName + "의 세로 면이 없다.");
                Assert.IsTrue(visual.Find("WallClimbEdgeLeft").GetComponent<SpriteRenderer>().enabled,
                    wallName + "의 왼쪽 테두리가 보이지 않는다.");
                Assert.IsTrue(visual.Find("WallClimbEdgeRight").GetComponent<SpriteRenderer>().enabled,
                    wallName + "의 오른쪽 테두리가 보이지 않는다.");
            }
        }

        [UnityTest]
        public IEnumerator K홀드_구간의_타일맵_세로턱이_눈에_보인다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var skillFragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                fragment => fragment.FragmentId == "residue_skill");
            Assert.IsNotNull(skillFragment, "K 홀드 구간의 되감기 파편이 없다.");

            bool foundTallWall = false;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.name != "TraversalWallEdge") continue;
                if (Vector2.Distance(renderer.bounds.center, skillFragment.transform.position) > 8f) continue;
                if (renderer.bounds.size.y >= 2.5f) foundTallWall = true;
            }

            Assert.IsTrue(foundTallWall,
                "K 홀드 안내 옆의 3유닛 턱에 보이는 세로 벽면이 만들어지지 않았다.");
        }

        [UnityTest]
        public IEnumerator 새_지역에서_되감기가_대상을_되돌린다()
        {
            ResetProgress();
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var player = PlayerController.Instance;
            var progress = GameManager.Instance.Progress;

            // R05의 되감기 파편을 밟아 스킬을 연다.
            var fragment = System.Array.Find(
                Object.FindObjectsByType<StoryFragment>(FindObjectsSortMode.None),
                f => f.FragmentId == "residue_skill");
            Assert.IsNotNull(fragment, "R05에 되감기 파편이 없다.");

            player.TeleportTo(fragment.transform.position + new Vector3(3f, 1f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(fragment.transform.position);
            for (int i = 0; i < 30; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(progress.HasSkill(EmotionId.Rewind), "되감기가 해금되지 않았다.");

            var controller = EmotionSkillController.Instance;
            yield return null;
            var skill = controller.Active;
            Assert.IsNotNull(skill, "되감기가 활성 스킬로 잡히지 않았다.");

            // 블록이 중력으로 밀려나 되돌릴 거리가 생길 때까지 기다린다.
            for (int i = 0; i < 150; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            Rewindable target = null;
            float best = float.MaxValue;
            foreach (var rewindable in Object.FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
            {
                if (!rewindable.CanRewind) continue;
                float d = Vector2.Distance(fragment.transform.position, rewindable.transform.position);
                if (d < best) { best = d; target = rewindable; }
            }
            Assert.IsNotNull(target, "밀려난 되감기 대상이 하나도 없다 — 되감을 것이 없다.");

            Transform outline = null;
            Transform marker = null;
            foreach (var child in target.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RewindOutline") outline = child;
                if (child.name == "RewindTargetMarker") marker = child;
            }
            Assert.IsNotNull(outline, "K 홀드 대상에 실제 외형을 따르는 골드 강조가 없다.");
            Assert.IsNotNull(marker, "K 홀드 대상 위에 위치 표식이 없다.");
            Assert.IsTrue(outline.GetComponent<SpriteRenderer>().enabled,
                "되감기 가능한데 골드 강조가 보이지 않는다.");
            Assert.IsTrue(marker.GetComponent<MeshRenderer>().enabled,
                "되감기 가능한데 대상 위치 표식이 보이지 않는다.");

            // 대상 옆에 서서 채널링한다.
            var displaced = target.transform.position;
            player.TeleportTo(displaced + new Vector3(-1.5f, 0.5f, 0f));
            for (int i = 0; i < 10; i++) { PlayerInput.Injected = default; yield return new WaitForFixedUpdate(); }

            controller.enabled = false; // 실제 K 홀드를 흉내낼 수 없으므로 스킬을 직접 돌린다
            skill.Begin();
            Assert.IsTrue(skill.IsActive, "되감기가 시작되지 않았다(대상 없음으로 즉시 취소).");

            for (float t = 0f; t < skill.Data.channelTime + 0.5f && skill.IsActive; t += Time.fixedDeltaTime)
            {
                skill.Tick(Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Debug.Log("===== 새 지역 되감기 ===== 밀려난 위치 " + displaced.ToString("F2")
                + " → 되감기 후 " + target.transform.position.ToString("F2")
                + " CanRewind=" + target.CanRewind);

            Assert.IsFalse(target.CanRewind, "채널링을 마쳤는데 대상이 원위치로 돌아오지 않았다.");
        }
    }
}
