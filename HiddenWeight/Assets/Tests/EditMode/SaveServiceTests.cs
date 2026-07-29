using System.IO;
using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.Tests
{
    public class SaveServiceTests
    {
        string _path;

        [SetUp]
        public void Setup()
        {
            _path = Path.Combine(Path.GetTempPath(), "hidden-weight-save-test.json");
            SaveService.PathOverride = _path;
            SaveService.Delete();
        }

        [TearDown]
        public void Teardown()
        {
            SaveService.Delete();
            SaveService.PathOverride = null;
        }

        [Test]
        public void 모든_영구_진행을_저장하고_복원한다()
        {
            var source = new ProgressState();
            source.CurrentZone = ZoneId.Gaze;
            source.LastCheckpoint = new Vector3(12.5f, -3f, 0f);
            source.UnlockSkill(EmotionId.Rewind);
            source.GrantAwareness();
            source.AddCurrency(17);
            source.AddHealthShard();
            source.CollectFragment("gaze_memory_01", "누군가 보고 있었다.");
            source.VisitRoom("Gaze/Room04");
            source.MarkShortcutOpen("gaze_shortcut_a");
            source.MarkEncounterCleared("gaze_midboss");
            source.MarkRewound("residue_bridge");
            Assert.IsTrue(source.TakeReward("gaze_reward"));

            Assert.IsTrue(SaveService.Save(source));
            var restored = new ProgressState();
            Assert.IsTrue(SaveService.TryLoad(restored));
            Assert.AreEqual(ZoneId.Gaze, restored.CurrentZone);
            Assert.AreEqual(source.LastCheckpoint, restored.LastCheckpoint);
            Assert.AreEqual(17, restored.Currency);
            Assert.AreEqual(1, restored.HealthShards);
            Assert.IsTrue(restored.HasSkill(EmotionId.Rewind));
            Assert.IsTrue(restored.HasAwareness);
            Assert.IsTrue(restored.HasFragment("gaze_memory_01"));
            Assert.IsTrue(restored.HasVisitedRoom("Gaze/Room04"));
            Assert.IsTrue(restored.IsShortcutOpen("gaze_shortcut_a"));
            Assert.IsTrue(restored.IsEncounterCleared("gaze_midboss"));
            Assert.IsTrue(restored.IsRewound("residue_bridge"));
            Assert.IsTrue(restored.IsRewardTaken("gaze_reward"));
        }

        [Test]
        public void 주_파일이_손상되면_백업을_복원한다()
        {
            var first = new ProgressState();
            first.AddCurrency(3);
            Assert.IsTrue(SaveService.Save(first));
            first.AddCurrency(4);
            Assert.IsTrue(SaveService.Save(first));
            File.WriteAllText(_path, "{broken");

            var restored = new ProgressState();
            Assert.IsTrue(SaveService.TryLoad(restored));
            Assert.AreEqual(3, restored.Currency);
        }
    }
}
