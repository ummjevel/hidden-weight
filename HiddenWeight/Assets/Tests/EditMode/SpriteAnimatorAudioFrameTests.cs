using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class SpriteAnimatorAudioFrameTests
    {
        [Test]
        public void Play_PublishesFirstDisplayedFrame()
        {
            var root = new GameObject("SpriteAnimatorAudioFrameTests");
            var renderer = root.AddComponent<SpriteRenderer>();
            var animator = root.AddComponent<SpriteAnimator>();
            var frames = new[]
            {
                Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero),
                Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero)
            };
            var clip = new SpriteAnimator.Clip
            {
                name = "PlayerWalk",
                frames = frames,
                fps = 12f,
                loop = true
            };

            SetPrivate(animator, "target", renderer);
            SetPrivate(animator, "clips", new[] { clip });

            string displayedClip = null;
            int displayedFrame = -1;
            animator.FrameDisplayed += (name, frame) =>
            {
                displayedClip = name;
                displayedFrame = frame;
            };

            animator.Play("PlayerWalk", true);

            Assert.That(displayedClip, Is.EqualTo("PlayerWalk"));
            Assert.That(displayedFrame, Is.Zero);

            Object.DestroyImmediate(frames[0]);
            Object.DestroyImmediate(frames[1]);
            Object.DestroyImmediate(root);
        }

        static void SetPrivate(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
