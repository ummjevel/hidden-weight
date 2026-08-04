using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Emotions;
using HiddenWeight.Player;
using HiddenWeight.UI;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 화면에 실제로 보이는 UI를 그대로 PNG로 남기는 도구.
    //
    // ZoneScreenshotTool은 카메라를 RenderTexture에 렌더해 그림을 받는데, 이 프로젝트의
    // UI 캔버스는 전부 ScreenSpaceOverlay다. Overlay 캔버스는 모든 카메라 렌더가 끝난 뒤
    // 백버퍼에 직접 그려지므로 RenderTexture에는 **한 픽셀도 들어오지 않는다**.
    // 그래서 지금까지 나온 스크린샷에는 HUD도 일시정지 화면도 없었다.
    //
    // 여기서는 찍는 동안만 캔버스를 ScreenSpaceCamera로 옮겨 같은 렌더에 합성한 뒤
    // 원래대로 되돌린다. 게임 코드는 건드리지 않는다.
    //
    // 실행:
    //   Unity -batchmode -runTests -testPlatform PlayMode \
    //         -testFilter "HiddenWeight.Tests.UICaptureTool"
    //   (-nographics를 붙이면 렌더 결과가 비어 있다. 반드시 빼고 실행할 것.)
    //
    // 결과는 저장소 루트의 .unity-logs/ui-shots/에 남는다.
    [Explicit]
    public class UICaptureTool
    {
        static string OutputDirectory =>
            Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,
                         ".unity-logs", "ui-shots");

        static readonly string[] FractureRooms =
            { "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08", "F09", "F10", "F11", "F12" };

        [SetUp]
        public void Setup()
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(OutputDirectory);
        }

        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            PlayerInput.Enabled = true;
            Time.timeScale = 1f;
            UISettings.HighContrast = false;
            UISettings.UiScale = 1f;
        }

        // ── 1. 방마다 실제 플레이 화면 + HUD ──────────────────────────────────
        // 카메라 크기를 건드리지 않는다. 플레이어가 실제로 보는 프레이밍 위에서
        // HUD가 어떻게 얹히는지가 판단 대상이기 때문이다.
        [UnityTest]
        public IEnumerator 균열_방마다_플레이화면을_찍는다()
        {
            yield return EnterFracture("F01");

            foreach (var room in FractureRooms)
            {
                yield return GoToRoom(room);
                yield return Shoot($"room_{room}");
            }
            Debug.Log($"[UICaptureTool] 방 {FractureRooms.Length}개 → {OutputDirectory}");
        }

        // ── 2. 일시정지 계열 화면 전부 ────────────────────────────────────────
        [UnityTest]
        public IEnumerator 균열_일시정지_화면을_찍는다()
        {
            yield return EnterFracture("F01");

            var pause = Object.FindAnyObjectByType<PauseMenu>();
            Assert.IsNotNull(pause, "씬에 PauseMenu가 없다.");

            pause.Open();
            yield return SettleUI();
            yield return Shoot("pause_00_root");

            foreach (PauseSection section in System.Enum.GetValues(typeof(PauseSection)))
            {
                pause.OpenSection(section);
                yield return SettleUI();
                yield return Shoot($"pause_10_{section}");
            }

            // 확인 다이얼로그 두 종류. 되돌릴 수 없는 선택을 묻는 화면이라
            // 문구와 기본 포커스가 특히 중요하다.
            pause.OpenSection(PauseSection.Map);
            yield return SettleUI();
            pause.RequestReturnToCheckpoint();
            yield return SettleUI();
            yield return Shoot("pause_20_confirm_checkpoint");

            pause.RequestGoToTitle();
            yield return SettleUI();
            yield return Shoot("pause_21_confirm_title");
        }

        // ── 3. HUD가 상태에 따라 어떻게 보이는지 ──────────────────────────────
        [UnityTest]
        public IEnumerator 균열_HUD_상태를_찍는다()
        {
            yield return EnterFracture("F01");

            var player = PlayerController.Instance;
            var health = player != null ? player.GetComponent<PlayerHealth>() : null;
            Assert.IsNotNull(health, "플레이어에 PlayerHealth가 없다.");

            health.RestoreFull();
            yield return WaitFrames(4);
            yield return Shoot("hud_00_full");

            // 능력이 있을 때만 감정 표시가 뜬다. 균열의 능력은 예지다.
            var progress = GameManager.Instance != null ? GameManager.Instance.Progress : null;
            if (progress != null)
            {
                progress.UnlockSkill(EmotionId.Foresight);
                if (EmotionSkillController.Instance != null)
                    EmotionSkillController.Instance.RefreshActive();
            }
            yield return SettleUI();
            yield return Shoot("hud_10_skill_ready");

            // 체력 1칸 — 위험 문양이 켜지는 지점.
            while (health.Current > 1)
            {
                health.TakeDamage(1, player.transform.position + Vector3.right * 3f);
                // 무적 시간이 있으므로 실제로 깎일 때까지 돌린다.
                yield return WaitFrames(60);
            }
            yield return WaitFrames(10);
            yield return Shoot("hud_20_critical");

            Debug.Log($"[UICaptureTool] HUD 상태: 체력 {health.Current}/{health.Max}");
        }

        // ── 4. 해상도·접근성 설정별 같은 화면 ─────────────────────────────────
        [UnityTest]
        public IEnumerator 해상도별_UI를_찍는다()
        {
            yield return EnterFracture("F01");

            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.OpenSection(PauseSection.Settings);
            yield return SettleUI();

            // 16:9 기준, 21:9 초광폭, 16:10, 4:3. 앵커가 잘못 잡힌 요소는
            // 비율이 바뀌는 순간 화면 밖으로 나가거나 서로 겹친다.
            (int w, int h, string label)[] sizes =
            {
                (1920, 1080, "16x9"),
                (2560, 1080, "21x9"),
                (1680, 1050, "16x10"),
                (1440, 1080, "4x3"),
            };
            foreach (var size in sizes)
            {
                yield return Shoot($"res_{size.label}", size.w, size.h);
            }

            UISettings.HighContrast = true;
            yield return SettleUI();
            yield return Shoot("access_00_highcontrast");

            UISettings.UiScale = 1.5f;
            yield return SettleUI();
            yield return Shoot("access_10_uiscale150");
        }

        // ── 4-2. 월드에 떠 있는 안내 문구가 실제로 무엇을 그리는지 ─────────────
        // 화면에 정체불명의 "K"가 여러 개 떠 있다는 보고. TextMesh는 폰트에 없는 글자를
        // 자리만 차지한 채 비워 두므로, 한글이 빠지면 ASCII 글자만 띄엄띄엄 남는다.
        [UnityTest]
        public IEnumerator 월드_안내문구를_찍는다()
        {
            yield return EnterFracture("F05");

            var hints = Object.FindObjectsByType<TutorialHint>(FindObjectsInactive.Include);
            Debug.Log($"[안내문구] F05의 TutorialHint {hints.Length}개");

            var player = PlayerController.Instance;
            foreach (var hint in hints)
            {
                // 가까이 가야 떠오른다(showRadius=5).
                player.TeleportTo((Vector2)hint.transform.position + new Vector2(0f, -1f));
                yield return WaitFrames(60);

                foreach (var mesh in hint.GetComponentsInChildren<TextMesh>(true))
                {
                    var chars = new StringBuilder();
                    foreach (char c in mesh.text)
                        chars.Append(c < 128 ? c.ToString() : $"[{(int)c:X4}]");
                    Debug.Log($"[안내문구] 위치 {hint.transform.position:F1} "
                              + $"알파 {mesh.color.a:F2} 글자수 {mesh.text.Length}\n"
                              + $"          원문 \"{mesh.text}\"\n"
                              + $"          코드 {chars}");
                }
                yield return Shoot("hint_00_tutorial");
            }
        }

        // ── 4-1. 전조 발판이 실제로 갈라지고 사라지는지 ───────────────────────
        // 예전에는 판정만 꺼지고 그림은 그대로 남아, 멀쩡해 보이는 발판을 통과해
        // 떨어졌다(OmenPlatform이 꺼져 있는 루트 렌더러를 잡고 있었다).
        [UnityTest]
        public IEnumerator 전조_발판이_보이게_무너진다()
        {
            yield return EnterFracture("F01");

            var omen = Object.FindAnyObjectByType<OmenPlatform>();
            Assert.IsNotNull(omen, "F01에 전조 발판이 없다.");
            var body = omen.GetComponent<Collider2D>();

            // 그림을 실제로 그리는 렌더러. 루트가 아니라 자식 Art에 있다.
            SpriteRenderer art = null;
            foreach (var renderer in omen.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled) { art = renderer; break; }
            Assert.IsNotNull(art, "전조 발판에 보이는 렌더러가 없다.");

            // 플레이어를 알아채는 거리(9) 안으로 옮긴다.
            var player = PlayerController.Instance;
            player.TeleportTo((Vector2)omen.transform.position + new Vector2(-4f, 1.5f));

            float startAlpha = art.color.a;
            bool sawCollapse = false;
            float deadline = Time.time + 8f;
            while (Time.time < deadline)
            {
                if (body != null && !body.enabled)
                {
                    sawCollapse = true;

                    // 화면에 남아 있는 조각을 전부 센다. Art 한 장만 보면 그 위에 얹힌
                    // 표면 타일 6장을 놓친다 — 실제로 그것 때문에 발판이 계속 보였다.
                    int stillVisible = 0;
                    foreach (var renderer in omen.GetComponentsInChildren<SpriteRenderer>(false))
                        if (renderer.enabled && renderer.color.a > 0.15f) stillVisible++;

                    Debug.Log($"[전조 발판] 판정 꺼짐 · 남아 보이는 렌더러 {stillVisible}개 "
                              + $"(처음 알파 {startAlpha:F2}), 재생 클립="
                              + (omen.GetComponentInChildren<SpriteAnimator>()?.CurrentClip ?? "없음"));
                    Assert.Zero(stillVisible,
                        "판정은 꺼졌는데 그림이 그대로 보인다 — 통과해서 떨어지는 함정이 된다.");
                    break;
                }
                yield return null;
            }

            Assert.IsTrue(sawCollapse, "8초를 기다려도 전조 발판이 무너지지 않았다.");
            yield return Shoot("omen_00_collapsed");
        }

        // ── 5. 그림 대신 수치로 — 화면 밖으로 나갔거나 겹친 요소 ──────────────
        // 스크린샷은 "뭔가 이상하다"까지만 알려준다. 어느 요소가 몇 픽셀 나갔는지는
        // RectTransform을 직접 읽어야 알 수 있고, 그쪽이 판정도 확실하다.
        [UnityTest]
        public IEnumerator UI_요소_경계를_적는다()
        {
            yield return EnterFracture("F01");
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            var report = new StringBuilder("[UI 경계 감사]\n");

            pause.Open();
            yield return SettleUI();
            AuditVisibleUI(report, "일시정지 본화면");

            foreach (PauseSection section in System.Enum.GetValues(typeof(PauseSection)))
            {
                pause.OpenSection(section);
                yield return SettleUI();
                AuditVisibleUI(report, "일시정지/" + section);
            }

            pause.Close();
            yield return SettleUI();
            AuditVisibleUI(report, "플레이 중 HUD");

            Debug.Log(report.ToString());
        }

        static void AuditVisibleUI(StringBuilder report, string label)
        {
            report.AppendLine($"  ── {label}");
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy) continue;
                var canvasRect = (RectTransform)canvas.transform;
                var bounds = WorldRect(canvasRect);

                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(false))
                {
                    if (!graphic.enabled) continue;
                    // 색이 완전히 투명한 자리잡기용 판은 화면에 없는 것과 같다.
                    if (graphic.color.a <= 0.01f) continue;
                    if (!IsGroupVisible(graphic)) continue;

                    var rect = WorldRect((RectTransform)graphic.transform);
                    var problems = new List<string>();

                    if (rect.xMin < bounds.xMin - 1f || rect.xMax > bounds.xMax + 1f
                        || rect.yMin < bounds.yMin - 1f || rect.yMax > bounds.yMax + 1f)
                        problems.Add($"화면밖 {Overflow(rect, bounds)}px");

                    if (graphic is Text text)
                    {
                        // 글자 크기는 캔버스 기준 단위(1920x1080 기준)로 계산되므로 칸도 같은
                        // 단위로 재야 한다. 월드 좌표(화면 픽셀)로 비교하면 캔버스 배율만큼
                        // 어긋나 멀쩡한 글자가 전부 잘린 것으로 나온다.
                        var box = ((RectTransform)text.transform).rect;
                        var settings = text.GetGenerationSettings(box.size);
                        float needW = text.cachedTextGenerator.GetPreferredWidth(text.text, settings)
                                      / text.pixelsPerUnit;
                        float needH = text.cachedTextGenerator.GetPreferredHeight(text.text, settings)
                                      / text.pixelsPerUnit;
                        if (needW > box.width + 1f)
                            problems.Add($"가로 넘침 {needW:F0} > 칸 {box.width:F0}");
                        if (needH > box.height + 1f && !text.resizeTextForBestFit)
                            problems.Add($"세로 넘침 {needH:F0} > 칸 {box.height:F0}");
                        if (string.IsNullOrWhiteSpace(text.text))
                            problems.Add("빈 문구");
                    }

                    if (rect.width < 1f || rect.height < 1f)
                        problems.Add($"크기 0 ({rect.width:F1}x{rect.height:F1})");

                    if (problems.Count == 0) continue;
                    report.AppendLine($"    {HierarchyPath(graphic.transform)} → {string.Join(", ", problems)}");
                }
            }
        }

        static bool IsGroupVisible(Graphic graphic)
        {
            for (var t = graphic.transform; t != null; t = t.parent)
            {
                var group = t.GetComponent<CanvasGroup>();
                if (group != null && group.alpha <= 0.01f) return false;
            }
            return true;
        }

        static string Overflow(Rect rect, Rect bounds)
        {
            float left = Mathf.Max(0f, bounds.xMin - rect.xMin);
            float right = Mathf.Max(0f, rect.xMax - bounds.xMax);
            float down = Mathf.Max(0f, bounds.yMin - rect.yMin);
            float up = Mathf.Max(0f, rect.yMax - bounds.yMax);
            return $"←{left:F0} →{right:F0} ↓{down:F0} ↑{up:F0}";
        }

        static readonly Vector3[] _corners = new Vector3[4];
        static Rect WorldRect(RectTransform rt)
        {
            rt.GetWorldCorners(_corners);
            float minX = Mathf.Min(_corners[0].x, _corners[2].x);
            float maxX = Mathf.Max(_corners[0].x, _corners[2].x);
            float minY = Mathf.Min(_corners[0].y, _corners[2].y);
            float maxY = Mathf.Max(_corners[0].y, _corners[2].y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        static string HierarchyPath(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        // ── 공용 ─────────────────────────────────────────────────────────────

        static IEnumerator EnterFracture(string room)
        {
            if (GameManager.Instance != null) GameManager.Instance.Progress.ResetAll();
            yield return RoomTestHarness.EnterRoom("Fracture", room);
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Time.timeScale = 1f;
            yield return WaitFrames(8);
        }

        static IEnumerator GoToRoom(string room)
        {
            if (RoomLoader.Instance.CurrentRoom != room)
            {
                yield return RoomLoader.Instance.LoadRoom(room, null);
                while (RoomLoader.Instance.IsTransitioning) yield return null;
            }
            yield return WaitFrames(8);
        }

        static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        // 일시정지 화면은 0.18초 페이드로 열린다. 배치모드는 한 프레임이 1ms도 안 되므로
        // 프레임 수로 기다리면 알파가 0.03인 상태를 찍는다 — 실제로 그렇게 찍혔다.
        // 실제 경과 시간으로 기다리고, 페이드가 붙은 CanvasGroup이 멈출 때까지 본다.
        static IEnumerator SettleUI()
        {
            float deadline = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < deadline)
            {
                bool settling = false;
                foreach (var group in Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Exclude))
                    if (group.alpha > 0.01f && group.alpha < 0.99f) settling = true;
                if (!settling && Time.realtimeSinceStartup > deadline - 0.9f) break;
                yield return null;
            }
            yield return WaitFrames(4);
        }

        // Overlay 캔버스를 잠시 카메라 공간으로 옮겨 카메라 렌더에 합성한다.
        // 이게 이 도구의 전부다 — 나머지는 ZoneScreenshotTool과 같다.
        static IEnumerator Shoot(string fileName, int width = 1920, int height = 1080)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning($"[UICaptureTool] {fileName}: Camera.main이 없다.");
                yield break;
            }

            var moved = new List<(Canvas canvas, Camera worldCamera, float plane)>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                // 자식 캔버스는 부모를 따라가므로 건드리면 안 된다.
                if (!canvas.isRootCanvas) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                moved.Add((canvas, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.5f, 1f);
            }

            int cullingMask = camera.cullingMask;
            camera.cullingMask = -1;   // UI 레이어가 빠져 있으면 캔버스가 안 그려진다

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            // 캔버스는 targetTexture가 붙은 뒤에야 새 화면 크기를 안다. 스케일러가
            // 다시 계산하고 레이아웃이 자리를 잡을 시간을 준다.
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera, request))
                camera.SubmitRenderRequest(request);
            else
                camera.Render();

            var shot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(OutputDirectory, fileName + ".png"),
                               shot.EncodeToPNG());

            camera.targetTexture = null;
            camera.cullingMask = cullingMask;
            Object.Destroy(shot);
            target.Release();
            Object.Destroy(target);

            foreach (var (canvas, worldCamera, plane) in moved)
            {
                if (canvas == null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = worldCamera;
                canvas.planeDistance = plane;
            }
            Canvas.ForceUpdateCanvases();
            yield return null;

            Debug.Log($"[UICaptureTool] {fileName}.png ({width}x{height}) 저장, 캔버스 {moved.Count}개 합성");
        }
    }
}
