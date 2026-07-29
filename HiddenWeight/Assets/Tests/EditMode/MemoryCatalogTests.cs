using NUnit.Framework;
using HiddenWeight.UI;

namespace HiddenWeight.Tests
{
    public class MemoryCatalogTests
    {
        [TestCase("residue_s1")]
        [TestCase("residue_skill")]
        [TestCase("residue_r11")]
        [TestCase("residue_final")]
        [TestCase("gaze_g04")]
        [TestCase("gaze_gs1")]
        [TestCase("gaze_skill")]
        [TestCase("gaze_g11")]
        [TestCase("gaze_final")]
        [TestCase("gaze_core")]
        [TestCase("fracture_f04")]
        [TestCase("fracture_fs1")]
        [TestCase("fracture_skill")]
        [TestCase("fracture_f11")]
        [TestCase("fracture_final")]
        [TestCase("fracture_core")]
        public void 모든_제작_기억에는_화면용_제목이_있다(string id)
        {
            Assert.IsTrue(MemoryCatalog.Has(id));
            Assert.AreNotEqual(id, MemoryCatalog.TitleFor(id));
        }
    }
}
