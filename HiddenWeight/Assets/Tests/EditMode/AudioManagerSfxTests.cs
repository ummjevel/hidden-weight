using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Core;

namespace HiddenWeight.Tests
{
    public class AudioManagerSfxTests
    {
        GameObject _root;
        AudioManager _audio;

        [SetUp]
        public void SetUp()
        {
            if (AudioManager.Instance != null)
                Object.DestroyImmediate(AudioManager.Instance.gameObject);

            // 변형 선택은 전역 Random을 쓰므로, 시드를 고정하지 않으면
            // 앞선 테스트가 남긴 상태에 따라 뽑히는 조합이 달라진다.
            Random.InitState(20260731);

            _root = new GameObject("AudioManagerSfxTests");
            _audio = _root.AddComponent<AudioManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (AudioManager.Instance != null)
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
        }

        [Test]
        public void ResolveSfx_UsesImportedDashClip()
        {
            var clip = _audio.ResolveSfx(SfxCue.Dash);

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.name, Does.StartWith("Player_Dash_"));
        }

        [Test]
        public void ResolveSfx_AvoidsImmediateRepeatWhenCueHasVariations()
        {
            var first = _audio.ResolveSfx(SfxCue.ItemPickup);
            var second = _audio.ResolveSfx(SfxCue.ItemPickup);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void ResolveSfx_UsesThreeImportedAttackSwingVariations()
        {
            var resolved = new HashSet<AudioClip>();
            for (int i = 0; i < 6; i++)
                resolved.Add(_audio.ResolveSfx(SfxCue.Attack));

            Assert.That(resolved.Count, Is.EqualTo(3));
            foreach (var clip in resolved)
                Assert.That(clip.name, Does.StartWith("Player_Attack_Swing_"));
        }

        // 폴더가 사라지거나 큐 이름이 바뀌면 조용히 절차 생성음으로 퇴화한다.
        // 전용 음원이 배정된 큐는 그 퇴화를 실패로 잡는다.
        static readonly SfxCue[] MappedCues =
        {
            SfxCue.UiConfirm, SfxCue.Checkpoint, SfxCue.Fragment,
            SfxCue.Attack, SfxCue.AttackHit, SfxCue.Jump, SfxCue.WallJump,
            SfxCue.Dash, SfxCue.Land, SfxCue.FootstepWalk, SfxCue.FootstepRun,
            SfxCue.WallGrab, SfxCue.WallSlide, SfxCue.Hurt, SfxCue.Death,
            SfxCue.Respawn, SfxCue.Heal, SfxCue.ItemPickup, SfxCue.Reward,
            SfxCue.ShortcutOpen, SfxCue.EnemyHit, SfxCue.EnemyDeath,
            SfxCue.BossTelegraph, SfxCue.BossPhase, SfxCue.BossVictory,
            SfxCue.RewindStart, SfxCue.RewindComplete,
            SfxCue.EnemyTelegraph, SfxCue.EnemyBlock,
            SfxCue.PlatformCrack, SfxCue.PlatformCollapse,
            SfxCue.GateOpen, SfxCue.GateClose, SfxCue.LiftStart, SfxCue.LiftStop,
            SfxCue.SecretReveal, SfxCue.UiCancel, SfxCue.UiPause, SfxCue.UiUnpause,
            SfxCue.UiMapOpen, SfxCue.UiMapClose
        };

        [Test]
        public void ResolveSfx_UsesImportedClipForEveryMappedCue([ValueSource(nameof(MappedCues))] SfxCue cue)
        {
            var clip = _audio.ResolveSfx(cue);

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.name, Is.Not.EqualTo("Sfx_" + cue),
                cue + " 큐가 전용 음원 대신 절차 생성음으로 돌아갔다.");
        }

        [Test]
        public void ResolveSfx_FallsBackWhenNoImportedClipExists()
        {
            var clip = _audio.ResolveSfx(SfxCue.Ability);

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.name, Is.EqualTo("Sfx_Ability"));
        }
    }
}
