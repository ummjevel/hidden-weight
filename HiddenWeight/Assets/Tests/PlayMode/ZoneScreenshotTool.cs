using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 지역 화면을 PNG로 남기는 도구. 테스트가 아니라 눈으로 볼 그림을 만드는 장치라
    // [Explicit]으로 두어 일반 테스트 실행에는 끼지 않는다.
    //
    // 지형·배경 작업은 "박스처럼 보인다" 같은 시각적 판정이 기준이라 수치 단언으로는
    // 확인할 수 없다. 배치모드에서 실제 렌더 결과를 받아야 한다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.ZoneScreenshotTool"
    //   (-nographics를 붙이면 렌더 결과가 비어 있다. 반드시 빼고 실행할 것.)
    //
    // 결과는 저장소 루트의 .unity-logs/screenshots/에 남는다.
    [Explicit]
    public class ZoneScreenshotTool
    {
        static string OutputDirectory =>
            Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,
                         ".unity-logs", "screenshots");

        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 균열_지역을_찍는다()
        {
            yield return Capture("Zone_Fracture_Full", "fracture");
        }

        // 정식 포탈 경로를 그대로 타고 들어가 문을 정면으로 찍는다.
        // "문이 화면에서 보이는가"는 씬 YAML로는 알 수 없고 렌더 결과로만 알 수 있다.
        [UnityTest]
        public IEnumerator 균열_포탈문을_찍는다()
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync("Zone_Fracture", LoadSceneMode.Single);
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;

            yield return null;
            // 앞선 RoomLoader가 남아 있으면 셸의 로더가 스스로를 파괴해 첫 방 요청이 사라진다.
            if (RoomLoader.Instance != null && RoomLoader.Instance.CurrentRoom == null
                && !RoomLoader.Instance.IsTransitioning)
            {
                RoomLoader.Instance.ConfigureZone("Room_Fracture_");
                RoomLoader.Instance.LoadRoom("F01", null);
            }

            for (int frame = 0; frame < 300; frame++)
            {
                if (Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Include).Length > 0) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();

            Directory.CreateDirectory(OutputDirectory);

            var all = Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Include);
            Debug.Log($"[문] 발견 {all.Length}개 (비활성 포함)");
            foreach (var d in all)
                Debug.Log($"[문] {d.name} active={d.gameObject.activeInHierarchy} "
                          + $"pos={d.transform.position:F1} children={d.transform.childCount}");

            int shot = 0;
            foreach (var door in all)
            {
                var renderers = door.GetComponentsInChildren<SpriteRenderer>(true);
                var report = new StringBuilder($"[문] {door.name} @ {door.transform.position:F1} ");
                foreach (var renderer in renderers)
                    report.Append($"| {renderer.name} sprite={(renderer.sprite == null ? "없음" : renderer.sprite.name)} "
                                  + $"enabled={renderer.enabled} a={renderer.color.a:F2} "
                                  + $"size={renderer.bounds.size:F1}");
                Debug.Log(report.ToString());

                yield return Shoot(door.transform.position, $"door_{shot++:00}_{door.name}");
                if (shot >= 3) break;
            }

            Debug.Log($"[ZoneScreenshotTool] 포탈문 {shot}장 저장");
        }

        // 화면 어느 자리에 무엇이 있는지 이름으로 특정한다. 스크린샷만으로는 "오른쪽 위에
        // 뭔가 떠 있다"까지만 알 수 있고 그것이 UI인지 월드 오브젝트인지 알 수 없다.
        [UnityTest]
        public IEnumerator 균열_화면_구성요소를_적는다()
        {
            // 균열 15개 방을 차례로 얹어, 각 방에서 "닿을 수 없는 높이에 그려지는 것"과
            // "바닥 아래에 그려지는 것"을 모은다. 방마다 눈으로 보는 것보다 훨씬 빠르고,
            // 놓치지 않는다. 판정 기준은 실측 점프 높이 2.72다.
            string[] rooms = { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08",
                               "F09", "F10", "F11", "F12", "FS1", "FS2", "FS3" };
            var report = new StringBuilder("[균열 전 방 이상 높이 점검]\n");

            foreach (var room in rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();
                for (int frame = 0; frame < 6; frame++) yield return null;

                // 그 방에서 실제로 밟을 수 있는 가장 높은 면을 찾는다.
                float highestGround = float.MinValue;
                foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
                {
                    if (col.isTrigger) continue;
                    if (col.gameObject.layer != LayerMask.NameToLayer("Ground")) continue;
                    if (col.name.Contains("Boundary")) continue;
                    highestGround = Mathf.Max(highestGround, col.bounds.max.y);
                }

                var odd = new System.Collections.Generic.List<string>();
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
                {
                    if (!sr.enabled || sr.sprite == null) continue;
                    if (sr.sortingOrder < 0) continue;          // 배경·장식은 대상이 아니다
                    var b = sr.bounds;
                    if (b.size.x > 20f || b.size.y > 20f) continue;   // 룸 배경 같은 대형은 제외

                    if (b.center.y > highestGround + 3f)
                        odd.Add($"    닿지않는높이 y={b.center.y:F1} (최고지형 {highestGround:F1}) "
                                + $"{sr.name} [{sr.sprite.name}]");
                }

                report.AppendLine($"  {room}: 최고 지형 y={highestGround:F1}, 이상 {odd.Count}개");
                foreach (var line in odd) report.AppendLine(line);
            }
            Debug.Log(report.ToString());
        }

        // 정식 포탈 경로로 들어가 방을 하나씩 열어 찍는다. 좌표 감사는 "이상한 높이에
        // 뭐가 있나"만 보고, 화면이 실제로 어떻게 보이는지는 그림으로만 알 수 있다.
        [UnityTest]
        public IEnumerator 균열_모든_방을_찍는다()
        {
            yield return RoomTestHarness.EnterRoom("Fracture", "F01");
            Time.timeScale = 1f;
            Directory.CreateDirectory(OutputDirectory);

            string[] rooms = { "F01", "F02", "F03", "F04", "F05", "F06",
                               "F07", "F08", "F09", "F10", "F11", "F12" };
            foreach (var room in rooms)
            {
                if (RoomLoader.Instance.CurrentRoom != room)
                {
                    yield return RoomLoader.Instance.LoadRoom(room, null);
                    while (RoomLoader.Instance.IsTransitioning) yield return null;
                }
                for (int frame = 0; frame < 8; frame++) yield return null;

                // 방 전체가 담기도록 카메라를 방 중심에 두고 넓힌다.
                var anchor = Object.FindAnyObjectByType<Room>();
                var camera = Camera.main;
                if (anchor == null || camera == null) continue;
                camera.orthographicSize = Mathf.Max(anchor.WorldBounds.extents.y,
                                                    anchor.WorldBounds.extents.x / camera.aspect) * 1.05f;
                yield return Shoot(anchor.WorldBounds.center, $"room_{room}");
            }
            Debug.Log($"[ZoneScreenshotTool] 방 {rooms.Length}개 저장 → {OutputDirectory}");
        }

        [UnityTest]
        public IEnumerator 잔재_지역을_찍는다()
        {
            yield return Capture("Zone_Residue_Full", "residue");
        }

        // 화면에 남은 단색 사각형이 무엇인지 이름으로 찾는다. 스크린샷만으로는 "회색 박스가
        // 하나 있다"까지만 알 수 있고 어느 오브젝트인지 알 수 없다.
        [UnityTest]
        public IEnumerator 균열의_플레이스홀더_사각형을_찾는다()
        {
            string[] rooms = { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08",
                               "F09", "F10", "F11", "F12", "FS1", "FS2", "FS3" };
            var report = new StringBuilder("[균열 방별 플레이스홀더 렌더러]\n");

            foreach (var room in rooms)
            {
                yield return SceneManager.LoadSceneAsync("Room_Fracture_" + room, LoadSceneMode.Single);
                yield return null;
                yield return new WaitForFixedUpdate();
                for (int frame = 0; frame < 6; frame++) yield return null;

                var found = new System.Collections.Generic.List<string>();
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
                {
                    if (!sr.enabled || sr.sprite == null) continue;
                    // 지역 아트는 전부 Fracture 접두사를 쓴다. 그 밖의 이름은 공용
                    // 플레이스홀더라 화면에서 단색 사각형으로 보인다.
                    if (sr.sprite.name.StartsWith("Fracture")) continue;
                    if (sr.sprite.name.Contains("Player")) continue;

                    string path = sr.name;
                    for (var t = sr.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                    found.Add($"    {sr.sprite.name} 크기{sr.bounds.size:F1} 정렬{sr.sortingOrder}  {path}");
                }

                report.AppendLine($"  {room}: {found.Count}개");
                foreach (var line in found) report.AppendLine(line);
            }
            Debug.Log(report.ToString());
        }

        static IEnumerator Capture(string sceneName, string label)
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            yield return new WaitForFixedUpdate();

            Directory.CreateDirectory(OutputDirectory);

            // 플레이어가 서 있는 곳을 첫 장으로, 나머지는 방 앵커를 순서대로 돈다.
            var player = Object.FindAnyObjectByType<PlayerController>();
            var rooms = Object.FindObjectsByType<Room>(
                FindObjectsInactive.Exclude);

            int shot = 0;
            if (player != null)
                yield return Shoot(player.transform.position, $"{label}_{shot++:00}_player");

            foreach (var room in rooms)
            {
                if (shot > 6) break;
                yield return Shoot(room.transform.position, $"{label}_{shot++:00}_{room.name}");
            }

            Debug.Log($"[ZoneScreenshotTool] {label}: {shot}장 저장 → {OutputDirectory}");
        }

        static IEnumerator Shoot(Vector3 focus, string fileName)
        {
            var camera = Camera.main;
            if (camera == null) yield break;

            // 카메라를 따라다니는 스크립트가 다시 끌고 가지 않도록 잠시 멈춘다.
            // Camera는 MonoBehaviour가 아니라 이 목록에 들어오지 않는다.
            var followers = camera.GetComponents<MonoBehaviour>();
            foreach (var follower in followers)
                if (follower != null) follower.enabled = false;

            camera.transform.position = new Vector3(focus.x, focus.y + 1.5f,
                                                    camera.transform.position.z);

            // 카메라에 붙은 배경은 LateUpdate에서 자기 위치를 맞추므로 한 프레임 더 돌린다.
            yield return null;
            yield return null;

            // ScreenCapture와 WaitForEndOfFrame은 쓰지 않는다 — 배치모드에서는
            // WaitForEndOfFrame이 영영 재개되지 않아 코루틴이 그대로 멈춘다.
            // URP에서는 렌더 요청으로 직접 그려 받는다.
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest
            {
                destination = target,
            };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera, request))
                camera.SubmitRenderRequest(request);
            else
                camera.Render();

            var shot = new Texture2D(target.width, target.height,
                                     TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(OutputDirectory, fileName + ".png"),
                               shot.EncodeToPNG());

            camera.targetTexture = null;
            Object.Destroy(shot);
            target.Release();
            Object.Destroy(target);

            foreach (var follower in followers)
                if (follower != null) follower.enabled = true;
        }
    }
}
