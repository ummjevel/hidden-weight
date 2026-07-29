using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.Tests
{
    // 잘라 둔 플레이어 스프라이트가 실제로 게임까지 도달했는지 본다.
    //
    // ChibiPlayerArtImporterTests는 "시트가 규격대로 잘렸는가"까지만 확인한다. 그런데 잘려
    // 있어도 프리팹의 SpriteAnimator에 붙지 않으면 게임에서는 한 프레임도 보이지 않는다.
    // 실제로 반응 VFX(PlayerHit/Death/Respawn)와 능력 시트 두 장(숨죽이기·자각)이 잘린 채로
    // 아무 데도 연결되지 않아 죽은 아트로 남아 있었다. 그 구멍을 여기서 막는다.
    public class PlayerAnimationWiringTests
    {
        const string PrefabPath = "Assets/Prefabs/Player.prefab";

        // (클립 이름, 프레임 수) — 시트의 행 하나가 클립 하나다.
        static readonly (string clip, int frames)[] Expected =
        {
            // Player_Locomotion_v1 (8x3)
            ("PlayerIdle", 8), ("PlayerWalk", 8), ("PlayerRun", 8),
            // Player_Aerial_v1 (6x4)
            ("PlayerJump", 6), ("PlayerAirMove", 6), ("PlayerFall", 6), ("PlayerLand", 6),
            // Player_Actions_v1 (6x2)
            ("PlayerAttack", 6), ("PlayerDash", 6),
            // Player_Wall_v1 (6x2)
            ("PlayerWallCling", 6), ("PlayerWallJump", 6),
            // PlayerVFX_v1 (6x3)
            ("PlayerHit", 6), ("PlayerDeath", 6), ("PlayerRespawn", 6),
            // Player_Hush_v1 (6x3)
            ("HushBegin", 6), ("HushMove", 6), ("HushEnd", 6),
            // Player_Awareness_v1 (6x3)
            ("AwarenessBegin", 6), ("AwarenessLoop", 6), ("AwarenessUnlock", 6),
        };

        static SerializedProperty LoadClips(out GameObject prefab)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Player 프리팹을 찾지 못했다: " + PrefabPath);

            var animator = prefab.GetComponentInChildren<HiddenWeight.World.SpriteAnimator>(true);
            Assert.IsNotNull(animator, "Player 프리팹에 SpriteAnimator가 없다 — 아트가 하나도 붙지 않았다.");

            var clips = new SerializedObject(animator).FindProperty("clips");
            Assert.IsNotNull(clips, "SpriteAnimator에 clips 배열이 없다.");
            return clips;
        }

        [Test]
        public void 잘라_둔_모든_시트가_프리팹_애니메이터에_붙어_있다()
        {
            var clips = LoadClips(out _);

            var found = new Dictionary<string, int>();
            for (int i = 0; i < clips.arraySize; i++)
            {
                var element = clips.GetArrayElementAtIndex(i);
                found[element.FindPropertyRelative("name").stringValue] =
                    element.FindPropertyRelative("frames").arraySize;
            }

            var report = new StringBuilder();
            var missing = new List<string>();

            foreach (var (clip, frames) in Expected)
            {
                if (!found.TryGetValue(clip, out int actual))
                {
                    report.AppendLine($"  {clip,-18} 없음");
                    missing.Add(clip);
                    continue;
                }

                report.AppendLine($"  {clip,-18} {actual}프레임" + (actual == frames ? "" : $" (기대 {frames})"));
                if (actual != frames) missing.Add($"{clip}({actual}/{frames}프레임)");
            }

            Debug.Log($"===== 플레이어 클립 {found.Count}개 =====\n{report}");

            Assert.IsEmpty(missing,
                "시트는 잘렸는데 게임에 붙지 않은 클립이 있다: " + string.Join(", ", missing) + "\n" + report);
        }

        [Test]
        public void 클립에_빈_프레임이_없다()
        {
            var clips = LoadClips(out _);

            var broken = new List<string>();
            for (int i = 0; i < clips.arraySize; i++)
            {
                var element = clips.GetArrayElementAtIndex(i);
                string name = element.FindPropertyRelative("name").stringValue;
                var frames = element.FindPropertyRelative("frames");

                if (frames.arraySize == 0) { broken.Add(name + "(프레임 0)"); continue; }

                for (int f = 0; f < frames.arraySize; f++)
                    if (frames.GetArrayElementAtIndex(f).objectReferenceValue == null)
                        broken.Add($"{name}[{f}]");
            }

            Assert.IsEmpty(broken,
                "스프라이트가 비어 있는 프레임이 있다(시트를 다시 자르거나 프리팹을 다시 붙여야 한다): "
                + string.Join(", ", broken));
        }

        // PlayerAnimator는 PlayerState를 "Player" + 상태 이름 클립으로 바꿔 재생한다.
        // 상태가 늘었는데 클립이 없으면 그 동작만 이전 그림으로 멈춰 보인다.
        [Test]
        public void 모든_PlayerState에_대응하는_클립이_있다()
        {
            var clips = LoadClips(out _);

            var names = new HashSet<string>();
            for (int i = 0; i < clips.arraySize; i++)
                names.Add(clips.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);

            var missing = new List<string>();
            foreach (PlayerState state in System.Enum.GetValues(typeof(PlayerState)))
            {
                string clip = "Player" + state;
                if (!names.Contains(clip)) missing.Add(clip);
            }

            Assert.IsEmpty(missing, "대응 클립이 없는 상태가 있다: " + string.Join(", ", missing));
        }
    }
}
