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

        [Test]
        public void ResolveSfx_FallsBackWhenNoImportedClipExists()
        {
            var clip = _audio.ResolveSfx(SfxCue.Ability);

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.name, Is.EqualTo("Sfx_Ability"));
        }
    }
}
