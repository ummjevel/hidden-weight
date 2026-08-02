using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 균열의 정식 진입 경로가 포탈 셸인지 확인한다.
    //
    // 배경: 방을 씬으로 쪼개고 문으로 잇는 구조는 만들어져 있었지만, ZoneData.sceneName이
    // 계속 Zone_Fracture_Full(통로판)을 가리켜 게임은 그 구조를 한 번도 쓰지 않았다.
    // 셸 씬 하나와 방 씬 15개가 통째로 고아였다. 이 검사는 그 상태로 되돌아가는 것을 막는다.
    public class FracturePortalTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        // 이 검사는 방 전환이 끝나기 전에 끝난다(암전이 배치모드에서 느리다). 전환 중에는
        // RoomLoader가 입력을 잠그고 GameManager가 timeScale을 만지므로, 그대로 두면
        // 뒤따르는 검사들의 봇이 시작 지점에서 한 발도 못 움직인다 — 실제로 균열·응시
        // 주파 검사 두 개가 그렇게 함께 무너졌다. 전역 상태를 반드시 되돌려 놓는다.
        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            PlayerInput.Enabled = true;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator 셸에_들어가면_첫_방이_지형과_함께_로드된다()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync("Zone_Fracture", LoadSceneMode.Single);

            // 씬을 직접 로드하면 게임 상태가 Playing이 아니라 Time.timeScale이 0으로 남는다.
            // RoomLoader는 방을 열기 전에 암전을 먼저 하므로 그대로 두면 그 자리에서 멈춘다
            // (정식 흐름에서는 SceneFlow가 Playing으로 바꾼 뒤 들어온다).
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;   // GameManager가 아직 없을 때를 위한 보루

            // ZoneEntryPoint가 Start에서 첫 방을 요청하고, RoomLoader는 암전을 끼고 비동기로 연다.
            // RoomLoader.Instance 자체가 아직 없을 수 있으므로 그것까지 기다린다.
            // 앞선 검사가 남긴 RoomLoader가 아직 살아 있으면 셸의 RoomLoader가 스스로를
            // 파괴하고(싱글턴 가드), ZoneEntryPoint의 첫 방 요청이 사라진다. 한 프레임 뒤에도
            // 방이 없으면 직접 한 번 요청해 순서 의존을 없앤다.
            yield return null;
            if (RoomLoader.Instance != null && RoomLoader.Instance.CurrentRoom == null
                && !RoomLoader.Instance.IsTransitioning)
            {
                RoomLoader.Instance.ConfigureZone("Room_Fracture_");
                RoomLoader.Instance.LoadRoom("F01", null);
            }

            // 판정은 내부 장부(CurrentRoom)가 아니라 실제 결과로 한다 — 방 지형이 화면에
            // 들어왔는가. 장부만 보면 "값은 채워졌는데 아무것도 안 보이는" 상태를 통과시킨다.
            for (int frame = 0; frame < 300; frame++)
            {
                if (Object.FindAnyObjectByType<Room>() != null
                    && Object.FindAnyObjectByType<Tilemap>() != null) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();

            // 정식 진입 경로가 통로판으로 되돌아가면 방 씬 15개와 포탈 문이 다시 고아가 된다.
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZoneData : null;
            Assert.IsNotNull(zone, "균열 지역 데이터가 잡히지 않았다.");
            Assert.AreEqual("Zone_Fracture", zone.sceneName,
                "균열의 정식 씬이 포탈 셸이 아니다.");

            Assert.IsNotNull(Object.FindAnyObjectByType<PlayerController>(), "셸에 플레이어가 없다.");
            Assert.IsNotNull(RoomLoader.Instance, "셸에 RoomLoader가 없다.");
            Debug.Log($"[FracturePortalTests] CurrentRoom={RoomLoader.Instance.CurrentRoom} "
                      + $"IsTransitioning={RoomLoader.Instance.IsTransitioning} "
                      + $"rooms={Object.FindObjectsByType<Room>(FindObjectsInactive.Exclude).Length}");
            Assert.IsNotNull(Object.FindAnyObjectByType<Room>(), "첫 방이 로드되지 않았다.");

            // 셸 자체에는 지형이 없다. 지형은 방 씬이 들고 와야 한다 — 이게 없으면
            // 플레이어는 로드 직후 그대로 낙하한다.
            Assert.IsNotEmpty(Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude),
                "로드된 방에 지형 타일맵이 없다.");

            // 문이 없으면 첫 방에서 나갈 수 없다.
            Assert.IsNotEmpty(Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Exclude),
                "로드된 방에 포탈 문이 없다.");
        }
    }
}
