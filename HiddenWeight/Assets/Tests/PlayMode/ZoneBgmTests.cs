using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;

namespace HiddenWeight.Tests
{
    // 지역 BGM이 실제로 걸리는지 확인한다. 오디오 파일을 프로젝트에 넣어 두는 것만으로는
    // 소리가 나지 않는다 — ZoneData.bgm에 물리고, 지역 진입 시 AudioManager가 틀어야 한다.
    public class ZoneBgmTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [UnityTest]
        public IEnumerator 잔재_지역에_들어가면_BGM이_걸린다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var zone = GameManager.Instance.CurrentZoneData;
            Assert.IsNotNull(zone, "지역 데이터가 잡히지 않았다(ZoneMarker 확인).");
            Assert.IsNotNull(zone.bgm, "잔재 ZoneData에 BGM이 연결돼 있지 않다.");

            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio, "AudioManager가 없다.");

            // 페이드는 실시간(unscaled) 기준이다. 배치모드는 프레임이 매우 빨라서 "프레임 수"로
            // 기다리면 실제로는 0.2초도 안 지나 클립이 아직 안 물려 있다.
            yield return new WaitForSecondsRealtime(2.5f);

            AudioSource playing = null;
            foreach (var source in audio.GetComponents<AudioSource>())
                if (source.clip != null) { playing = source; break; }

            Debug.Log("===== BGM ===== 지역=" + zone.displayName
                + " 클립=" + zone.bgm.name + " (" + zone.bgm.length.ToString("F0") + "초)"
                + " / 재생 소스 클립=" + (playing == null ? "없음" : playing.clip.name)
                + " loop=" + (playing != null && playing.loop));

            Assert.IsNotNull(playing, "AudioManager가 BGM 클립을 물지 않았다 — 지역 진입 시 재생이 안 걸린다.");
            Assert.AreEqual(zone.bgm, playing.clip, "재생 중인 클립이 지역 BGM과 다르다.");
            Assert.IsTrue(playing.loop, "BGM이 반복 재생으로 설정되지 않았다.");
        }

        [UnityTest]
        public IEnumerator 정식_음원이_없는_지역은_앰비언스_폴백을_쓴다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Gaze_Full", LoadSceneMode.Single);
            yield return null;
            var audio = AudioManager.Instance;
            Assert.IsNotNull(audio);
            Assert.IsNotNull(audio.CurrentBgm);
            Assert.That(audio.CurrentBgm.name, Does.StartWith("GeneratedAmbient_Gaze"));
#if UNITY_EDITOR
            foreach (var source in audio.GetComponents<AudioSource>())
                if (source.loop) Assert.IsTrue(source.mute, "개발 중에는 BGM 소스가 음소거돼야 한다.");
#endif
        }
    }
}
