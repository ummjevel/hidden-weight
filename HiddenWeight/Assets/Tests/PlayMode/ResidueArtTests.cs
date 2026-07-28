using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 잔재 지역이 플레이스홀더가 아니라 실제 잔재 아트를 쓰고 있는지 확인한다.
    // 스프라이트 이름으로 판별한다 — 잔재 시트는 ResidueArtSlicer가 이름을 붙여 뒀고
    // (Terrain_/Platform_/Enemy_/Item_/Shortcut_/Prop_/Rewind_/Watcher_),
    // 플레이스홀더는 파일 이름 그대로(Tile/Player/Enemy/Fragment/Platform)다.
    public class ResidueArtTests
    {
        static readonly string[] ResiduePrefixes =
            { "Terrain_", "Platform_", "Rewind_", "Hazard_", "Prop_", "AmbientVFX_",
              "Player_", "Enemy_", "Item_", "Shortcut_", "Watcher_" };

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        static bool IsResidueArt(Sprite sprite)
        {
            if (sprite == null) return false;
            foreach (var prefix in ResiduePrefixes)
                if (sprite.name.StartsWith(prefix)) return true;
            return sprite.name.Contains("_BG_") || sprite.name.Contains("_FG_"); // 방 배경
        }

        // 스프라이트 애니메이션이 실제로 붙어 재생되는지 확인한다.
        [UnityTest]
        public IEnumerator 플레이어와_적이_프레임_애니메이션을_재생한다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var animators = Object.FindObjectsByType<SpriteAnimator>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNotEmpty(animators, "씬에 SpriteAnimator가 하나도 없다 — 애니메이션이 안 붙었다.");

            var player = HiddenWeight.Player.PlayerController.Instance;
            var playerAnimator = player.GetComponentInChildren<SpriteAnimator>();
            Assert.IsNotNull(playerAnimator, "플레이어에 SpriteAnimator가 없다.");
            Assert.IsTrue(playerAnimator.Has("PlayerIdle"), "플레이어 Idle 클립이 없다.");
            Assert.IsTrue(playerAnimator.Has("PlayerRun"), "플레이어 Run 클립이 없다.");

            // 프레임이 실제로 넘어가는지 — 같은 스프라이트에 머물러 있으면 애니메이션이 아니다.
            // 루트 렌더러는 꺼져 있고 실제로 그리는 것은 Art 자식이다. 그쪽을 봐야 한다.
            var artTransform = player.transform.Find("Art");
            Assert.IsNotNull(artTransform, "플레이어에 Art 자식이 없다.");
            var renderer = artTransform.GetComponent<SpriteRenderer>();
            var first = renderer.sprite;
            bool changed = false;
            for (int i = 0; i < 120 && !changed; i++)
            {
                yield return null;
                if (renderer.sprite != first) changed = true;
            }

            Debug.Log("===== 애니메이션 ===== SpriteAnimator " + animators.Length
                + "개 / 플레이어 현재 클립 " + playerAnimator.CurrentClip
                + " / 프레임 전환 " + changed);

            Assert.IsTrue(changed, "플레이어 스프라이트가 한 프레임에 멈춰 있다 — 재생이 안 된다.");
        }

        // 리뷰에서 나온 세 문제를 회귀로 막는다:
        // 좌우 반전이 안 보이는 렌더러에 걸리는 것, 피격 점멸이 구형 그림을 켜 놓는 것,
        // 동작마다 캐릭터 크기가 널뛰는 것.
        [UnityTest]
        public IEnumerator 캐릭터_크기와_반전이_일정하다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var player = HiddenWeight.Player.PlayerController.Instance;

            // 보이는 렌더러는 하나뿐이어야 한다(루트의 구형 렌더러가 남아 있으면 안 된다).
            var renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
            int visible = 0;
            foreach (var r in renderers) if (r.enabled) visible++;
            Assert.AreEqual(1, visible,
                "플레이어에 보이는 스프라이트가 " + visible + "개다 — 구형 렌더러가 남아 있으면 피격 점멸이 그걸 켜 버린다.");

            var animator = player.GetComponentInChildren<SpriteAnimator>();
            Assert.IsNotNull(animator.Renderer, "애니메이터가 그릴 렌더러를 못 잡았다.");

            // 클립을 바꿔 가며 화면 높이가 일정한지 본다.
            var report = new StringBuilder();
            float first = -1f;
            foreach (var clip in new[] { "PlayerIdle", "PlayerRun", "PlayerAttack", "PlayerWallCling" })
            {
                if (!animator.Has(clip)) continue;
                animator.Play(clip, true);
                yield return null;

                float height = animator.Renderer.bounds.size.y;
                report.AppendLine("  " + clip + " 높이 " + height.ToString("F2"));
                if (first < 0f) first = height;
                Assert.AreEqual(first, height, 0.15f,
                    clip + "에서 캐릭터 높이가 " + height.ToString("F2") + "로 달라진다.\n" + report);
            }
            Debug.Log("===== 캐릭터 크기 =====\n" + report);
            Assert.Greater(first, 1.2f, "캐릭터가 너무 작다(높이 " + first.ToString("F2") + ").");

            // 좌우 반전이 보이는 렌더러에 걸리는지.
            var visibleRenderer = animator.Renderer;
            player.GetComponent<HiddenWeight.Player.PlayerAnimator>().enabled = true;
            for (int i = 0; i < 60; i++)
            {
                PlayerInput.Injected = new PlayerInput.Frame { horizontal = -1f };
                yield return new WaitForFixedUpdate();
                if (i == 30)
                    Debug.Log("  중간: Enabled=" + PlayerInput.Enabled
                        + " Horizontal=" + PlayerInput.Horizontal
                        + " Facing=" + player.Facing
                        + " timeScale=" + Time.timeScale);
            }
            Debug.Log("===== 반전 진단 ===== Facing=" + player.Facing
                + " flipX=" + visibleRenderer.flipX
                + " MovementLocked=" + player.MovementLocked
                + " state=" + player.State
                + " PlayerAnimator enabled=" + player.GetComponent<HiddenWeight.Player.PlayerAnimator>().enabled
                + " 렌더러=" + visibleRenderer.name);
            Assert.IsTrue(visibleRenderer.flipX, "왼쪽으로 이동했는데 보이는 그림이 뒤집히지 않았다.");
            PlayerInput.Injected = null;
        }

        [UnityTest]
        public IEnumerator 잔재_지역이_실제_아트를_쓴다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var counts = new Dictionary<string, int>();
            int residue = 0, placeholder = 0;

            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.sprite == null) continue;

                string name = renderer.sprite.name;
                counts.TryGetValue(name, out int n);
                counts[name] = n + 1;

                if (IsResidueArt(renderer.sprite)) residue++;
                else placeholder++;
            }

            var report = new StringBuilder();
            report.AppendLine("잔재 아트 " + residue + "개 / 플레이스홀더 " + placeholder + "개");
            foreach (var pair in counts)
                report.AppendLine("  " + pair.Key + " x" + pair.Value);
            Debug.Log("===== 잔재 아트 적용 =====\n" + report);

            Assert.Greater(residue, 0, "잔재 아트가 하나도 안 쓰이고 있다.\n" + report);

            // 핵심 항목이 실제 아트로 바뀌었는지 개별 확인.
            foreach (var required in new[] { "Terrain_", "Item_", "Enemy_", "Shortcut_" })
            {
                bool found = false;
                foreach (var key in counts.Keys)
                    if (key.StartsWith(required)) { found = true; break; }
                Assert.IsTrue(found, required + " 계열 아트가 씬에 하나도 없다.\n" + report);
            }
        }
    }
}
