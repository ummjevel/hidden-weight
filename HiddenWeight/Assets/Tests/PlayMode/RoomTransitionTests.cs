using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class RoomTransitionTests
    {
        [UnityTest]
        public IEnumerator EntryPoint_LoadsFirstRoom()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R01"));
            Assert.That(SceneManager.GetSceneByName("Room_Residue_R01").isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator LoadRoom_UnloadsPreviousRoom()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");
            yield return RoomTestHarness.EnterRoom("Residue", "R02");

            Assert.That(SceneManager.GetSceneByName("Room_Residue_R02").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Room_Residue_R01").isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator Door_CarriesPlayerToPairedDoor()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            var door = FindDoor("residue_R01_R02:E");
            Assert.That(door, Is.Not.Null, "R01에 동쪽 문이 없다.");

            RoomLoader.Instance.RequestTransition(door);
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R02"));

            var arrival = FindDoor("residue_R01_R02:W");
            Assert.That(arrival, Is.Not.Null, "R02에 서쪽 문이 없다.");

            // 도착 좌표는 발밑 기준점인데 플레이어 transform은 캡슐(0.8x1.4) 중심이라,
            // 제대로 서 있으면 오히려 0.7쯤 위에 온다. 가로는 정확히 맞아야 하고,
            // 세로는 캡슐 반높이만큼 위 / 발판을 놓쳤다고 볼 만큼 아래를 벗어나면 실패다.
            var player = (Vector2)PlayerController.Instance.transform.position;
            Assert.That(Mathf.Abs(player.x - arrival.ArrivalPosition.x), Is.LessThan(0.1f),
                "가로 도착 위치가 짝 문과 어긋난다.");
            Assert.That(player.y - arrival.ArrivalPosition.y, Is.InRange(-1f, 1f),
                "세로 도착 위치가 짝 문에서 캡슐 반높이 이상 벗어났다.");
        }

        // 도착한 문 위에 서 있으므로 무장이 풀려 있어야 즉시 되돌아가지 않는다.
        [UnityTest]
        public IEnumerator ArrivalDoor_IsDisarmed()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            RoomLoader.Instance.RequestTransition(FindDoor("residue_R01_R02:E"));
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            Assert.That(FindDoor("residue_R01_R02:W").Armed, Is.False);
        }

        [UnityTest]
        public IEnumerator Transition_SetsCurrentRoomOnCamera()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R02");

            Assert.That(RoomCamera.Instance.CurrentRoom, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Camera_FollowsPlayerToRoomEdgeAndRevealsNextPath()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            var roomCamera = RoomCamera.Instance;
            var room = roomCamera.CurrentRoom;
            var camera = roomCamera.GetComponent<Camera>();
            Assert.That(room, Is.Not.Null);

            // 문 트리거를 직접 밟으면 다른 씬으로 넘어가므로, 문 앞에서 카메라가 다음
            // 통로를 이미 보여 주는지를 확인한다.
            var edge = new Vector3(room.WorldBounds.max.x - 2f, 4f, 0f);
            PlayerController.Instance.TeleportTo(edge);
            roomCamera.SnapToPlayer();
            yield return null;

            float cameraRight = roomCamera.transform.position.x
                + camera.orthographicSize * camera.aspect;
            Assert.That(cameraRight, Is.GreaterThan(room.WorldBounds.max.x + 2f),
                "방 끝에서 다음 통로를 미리 보여 주지 않아 플레이어만 화면 밖으로 나간다.");
            Assert.That(Mathf.Abs(roomCamera.transform.position.x - edge.x), Is.LessThan(1f),
                "방 경계에서 카메라가 플레이어를 따라오지 않는다.");
        }

        [UnityTest]
        public IEnumerator MissingRoom_LeavesPlayerAndRestoresInput()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("찾을 수 없다"));
            yield return RoomLoader.Instance.LoadRoom("R99", null);
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R01"));
            Assert.That(PlayerInput.Enabled, Is.True);
        }

        // 주 동선을 문만 따라 끝까지 갈 수 있어야 QA가 성립한다.
        [UnityTest]
        public IEnumerator MainRoute_WalksR01ToR12()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            for (int i = 1; i < 12; i++)
            {
                string linkId = $"residue_R{i:00}_R{i + 1:00}";
                var door = FindDoor(linkId + ":E");
                Assert.That(door, Is.Not.Null, linkId + " 의 동쪽 문이 없다.");

                RoomLoader.Instance.RequestTransition(door);
                while (RoomLoader.Instance.IsTransitioning) yield return null;
            }

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R12"));
        }

        [UnityTest]
        public IEnumerator PortalShell_PreservesFullMapShortcutsAndSecretRoute()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R03");
            Assert.That(FindDoor("residue_shortcut_A:S"), Is.Not.Null,
                "_Full의 숏컷 A가 룸형 잔재에 이식되지 않았다.");
            Assert.That(FindDoor("residue_shortcut_B:S"), Is.Not.Null,
                "_Full의 숏컷 B가 룸형 잔재에 이식되지 않았다.");

            yield return RoomLoader.Instance.LoadRoom("R07", null);
            while (RoomLoader.Instance.IsTransitioning) yield return null;
            yield return null;
            Assert.That(FindDoor("residue_shortcut_C:S"), Is.Not.Null,
                "_Full의 숏컷 C가 룸형 잔재에 이식되지 않았다.");

            yield return RoomLoader.Instance.LoadRoom("R06", null);
            while (RoomLoader.Instance.IsTransitioning) yield return null;
            yield return null;
            Assert.That(FindDoor("residue_R06_S2:D"), Is.Not.Null,
                "R06 선택 되감기 뒤 열리는 S2 포탈이 없다.");
        }

        [UnityTest]
        public IEnumerator PortalR06_RestoreDoesNotTrapPlayerInsideRequiredStep()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R06");
            yield return null; // ResidueLoopRuntime의 방별 보정 완료

            var step = GameObject.Find("R06_RequiredStep");
            Assert.That(step, Is.Not.Null);
            var rewindable = step.GetComponent<Rewindable>();
            var stepCollider = step.GetComponent<Collider2D>();
            var player = PlayerController.Instance;
            var playerCollider = player.GetComponent<Collider2D>();
            Vector3 restoredPosition = step.transform.position;

            step.transform.position = restoredPosition + Vector3.down * 3f;
            player.TeleportTo(restoredPosition);
            Physics2D.SyncTransforms();
            rewindable.Rewind();
            yield return new WaitForFixedUpdate();

            Assert.That(stepCollider.bounds.Intersects(playerCollider.bounds), Is.False,
                "R06 필수 복원 계단 내부에 플레이어가 끼었다.");
            Assert.That(player.transform.position.x, Is.LessThan(stepCollider.bounds.min.x),
                "R06 복원 시 플레이어가 왼쪽 안전 바닥으로 빠져나오지 못했다.");
        }

        // Task 3의 문 무장/해제 수명주기(OnTriggerEnter2D가 실제로 전환을 발동시키는지,
        // 무장 해제 중엔 눌러도 안 튕기는지, OnTriggerExit2D가 재무장하는지)는 EditMode에
        // 물리가 없어 한 번도 실행된 적이 없다. 위의 테스트들은 전부 RequestTransition을
        // 직접 호출해 이 수명주기를 우회한다 — 그래서 여기서는 플레이어를 실제로 문
        // 콜라이더에 겹치게 해 트리거 콜백 자체를 검증한다.
        [UnityTest]
        public IEnumerator ArrivalDoor_RearmsAfterPlayerWalksOff()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            var exitDoor = FindDoor("residue_R01_R02:E");
            Assert.That(exitDoor, Is.Not.Null, "R01에 동쪽 문이 없다.");

            // 문 콜라이더 위로 순간이동시켜, OnTriggerEnter2D가 스스로 전환을 발동하는지 확인한다.
            PlayerController.Instance.TeleportTo(exitDoor.transform.position);
            for (int i = 0; i < 30 && !RoomLoader.Instance.IsTransitioning; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(RoomLoader.Instance.IsTransitioning, Is.True, "문 트리거가 전환을 발동시키지 않았다.");

            while (RoomLoader.Instance.IsTransitioning) yield return null;
            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R02"));

            var arrival = FindDoor("residue_R01_R02:W");
            Assert.That(arrival, Is.Not.Null, "R02에 서쪽 문이 없다.");
            Assert.That(arrival.Armed, Is.False, "도착 직후에는 무장 해제 상태여야 한다.");

            // 도착 문 위로 되돌아가도(호기심에 다시 밟아도) 무장 해제 상태라 되튕기면 안 된다.
            PlayerController.Instance.TeleportTo(arrival.transform.position);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(RoomLoader.Instance.IsTransitioning, Is.False, "무장 해제된 문인데도 전환이 시작됐다.");
            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R02"), "무장 해제된 문이 되튕겨 보냈다.");

            // 문 밖으로 완전히 벗어나야 OnTriggerExit2D가 재무장해, 다음에 이 문으로 돌아갈 수 있다.
            PlayerController.Instance.TeleportTo(arrival.ArrivalPosition);
            for (int i = 0; i < 30 && !arrival.Armed; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(arrival.Armed, Is.True, "문을 벗어났는데 재무장되지 않았다 — 돌아가는 길이 막힌다.");
        }

        static RoomDoor FindDoor(string doorId)
        {
            foreach (var door in Object.FindObjectsByType<RoomDoor>(FindObjectsSortMode.None))
                if (door.DoorId == doorId) return door;

            return null;
        }
    }
}
