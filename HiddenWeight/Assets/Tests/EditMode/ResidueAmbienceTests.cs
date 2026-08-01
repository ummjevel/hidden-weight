using NUnit.Framework;
using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 방별 환경음은 Resources 경로와 파일 이름이 어긋나도 조용히 절차 생성 베드로 떨어진다.
    // 소리는 계속 나기 때문에 플레이로는 눈치채기 어려워서 여기서 잡는다.
    public class ResidueAmbienceTests
    {
        // LEVEL_21_RESIDUE_ROOMS.md의 R01~R12. 잔재 씬의 Room01~Room12와 1:1로 대응한다.
        static readonly int[] ResidueRooms = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        [Test]
        public void LoadAmbience_ResolvesClipForEveryResidueRoom([ValueSource(nameof(ResidueRooms))] int room)
        {
            var clip = ResidueAmbientAudio.LoadAmbience(room);

            Assert.That(clip, Is.Not.Null, "Room" + room.ToString("00") + " 환경음이 없다.");
            Assert.That(clip.length, Is.GreaterThan(5f),
                "환경음이 너무 짧아 루프가 티 난다: " + clip.name);
        }

        [Test]
        public void RoomNumber_ReadsDigitsFromRoomName()
        {
            Assert.That(ResidueAmbientAudio.RoomNumber("Room07"), Is.EqualTo(7));
            Assert.That(ResidueAmbientAudio.RoomNumber("ResidueRoom12"), Is.EqualTo(12));
            Assert.That(ResidueAmbientAudio.RoomNumber("Lobby"), Is.EqualTo(0));
        }

        [Test]
        public void LoadAmbience_FallsBackWhenRoomHasNoDedicatedClip()
        {
            Assert.That(ResidueAmbientAudio.LoadAmbience(99), Is.Null);
        }
    }
}
