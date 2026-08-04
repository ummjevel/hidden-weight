using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.Tests
{
    // UI-0 회귀: 메뉴 안전성, 기본 포커스, 공통 해상도, 확인 전 진행도 보존을 검증한다.
    public class UIZeroTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown()
        {
            PlayerInput.Injected = null;
            PlayerInput.Enabled = true;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused)
                GameManager.Instance.SetState(GameState.Playing);
        }

        [UnityTest]
        public IEnumerator 타이틀은_새게임에_기본_포커스를_주고_공통_해상도를_쓴다()
        {
            yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var canvas = GameObject.Find("TitleCanvas");
            Assert.IsNotNull(canvas, "TitleCanvas가 없다.");

            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "TitleCanvas에 CanvasScaler가 없다.");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(UIBuilder.ReferenceResolution / UISettings.UiScale, scaler.referenceResolution);
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.001f);

            var newGame = GameObject.Find("Button_새 게임");
            Assert.IsNotNull(newGame, "새 게임 버튼이 없다.");
            Assert.IsNotNull(GameObject.Find("Button_제작진"), "제작진 버튼이 없다.");
            Assert.IsNotNull(GameObject.Find("Button_1단계 · 잔재"), "1단계 테스트 버튼이 없다.");
            Assert.IsNotNull(GameObject.Find("Button_2단계 · 응시"), "2단계 테스트 버튼이 없다.");
            Assert.IsNotNull(GameObject.Find("Button_3단계 · 균열"), "3단계 테스트 버튼이 없다.");
            Assert.IsNull(GameObject.Find("Button_잔재 지역 (작업 중)"),
                "정식 타이틀에 예전 개발 버튼 문구가 남아 있다.");

            Assert.IsNotNull(EventSystem.current, "타이틀에 EventSystem이 없다.");
            Assert.AreEqual(newGame, EventSystem.current.currentSelectedGameObject,
                "타이틀 진입 시 새 게임 버튼에 기본 포커스가 가야 한다.");
        }

        [UnityTest]
        public IEnumerator 지도는_현재_지역의_열다섯_방_전체를_표시한다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Gaze_Full", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            gm.Progress.ResetAll();
            gm.EnterZone(HiddenWeight.Data.ZoneId.Gaze);
            gm.Progress.VisitRoom("Gaze/GazeRoom01");
            gm.SetState(GameState.Playing);

            var pause = Object.FindFirstObjectByType<PauseMenu>();
            Assert.IsNotNull(pause);
            pause.OpenSection(PauseSection.Map);
            yield return null;

            int nodes = 0;
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (transform.name.StartsWith("MapNode_Gaze/")) nodes++;

            Assert.AreEqual(15, nodes, "전체 지도에는 메인 12룸과 비밀 3룸이 모두 보여야 한다.");
        }

        [UnityTest]
        public IEnumerator 타이틀에서_세_단계의_전체_연결_맵으로_직접_진입할_수_있다()
        {
            yield return AssertStageButtonLoads("Button_1단계 · 잔재", "Zone_Residue_Full");
            yield return AssertStageButtonLoads("Button_2단계 · 응시", "Zone_Gaze_Full");
            yield return AssertStageButtonLoads("Button_3단계 · 균열", "Zone_Fracture_Full");
        }

        static IEnumerator AssertStageButtonLoads(string buttonName, string sceneName)
        {
            yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            yield return null;

            var buttonObject = GameObject.Find(buttonName);
            Assert.IsNotNull(buttonObject, buttonName + " 버튼이 없다.");
            buttonObject.GetComponent<Button>().onClick.Invoke();

            float deadline = Time.realtimeSinceStartup + 3f;
            while (SceneManager.GetActiveScene().name != sceneName
                   && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name,
                buttonName + "을 눌러도 해당 단계로 이동하지 않았다.");
            bool residue = sceneName.Contains("Residue");
            Assert.AreEqual(!residue,
                GameManager.Instance.Progress.HasSkill(HiddenWeight.Data.EmotionId.Rewind),
                "잔재는 R05 전까지 되감기가 잠겨 있어야 한다.");
            Assert.AreEqual(!residue,
                GameManager.Instance.Progress.HasSkill(HiddenWeight.Data.EmotionId.Hush));
            Assert.AreEqual(!residue,
                GameManager.Instance.Progress.HasSkill(HiddenWeight.Data.EmotionId.Foresight));
            Assert.AreEqual(!residue, GameManager.Instance.Progress.HasAwareness);
        }

        [UnityTest]
        public IEnumerator 타이틀_확인창을_열기_전에는_진행도를_지우지_않는다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            gm.Progress.ResetAll();
            gm.Progress.AddCurrency(7);
            gm.SetState(GameState.Playing);

            var pause = Object.FindFirstObjectByType<PauseMenu>();
            Assert.IsNotNull(pause, "프롤로그에 PauseMenu가 없다.");

            pause.Open();
            yield return null;
            pause.RequestGoToTitle();
            yield return null;

            Assert.AreEqual(7, gm.Progress.Currency,
                "확인도 하기 전에 타이틀 이동이 진행도를 지웠다.");
            Assert.AreEqual(GameState.Paused, gm.State);
            Assert.IsTrue(pause.IsConfirming, "타이틀 이동 확인창이 열리지 않았다.");

            var dialog = Object.FindFirstObjectByType<ConfirmDialog>();
            Assert.IsNotNull(dialog);
            Assert.AreEqual("Button_취소", EventSystem.current.currentSelectedGameObject.name,
                "파괴적 행동 확인창의 기본 포커스는 취소여야 한다.");

            dialog.Cancel();
            Assert.AreEqual(7, gm.Progress.Currency, "취소했는데 진행도가 바뀌었다.");
            Assert.AreEqual(GameState.Paused, gm.State, "확인창 취소는 일시정지 메뉴를 닫지 않아야 한다.");

            pause.Close();
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.IsTrue(PlayerInput.Enabled);
        }

        [UnityTest]
        public IEnumerator 체크포인트_복귀는_확인한_뒤에만_플레이어를_옮긴다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            gm.Progress.ResetAll();
            gm.SetState(GameState.Playing);

            var player = PlayerController.Instance;
            var pause = Object.FindFirstObjectByType<PauseMenu>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(pause);

            var checkpoint = new Vector3(6f, 1f, 0f);
            gm.Progress.LastCheckpoint = checkpoint;
            player.TeleportTo(new Vector3(20f, 5f, 0f));
            var before = player.transform.position;

            pause.Open();
            yield return null;
            pause.RequestReturnToCheckpoint();
            yield return null;

            Assert.AreEqual(before, player.transform.position,
                "확인 전인데 플레이어가 체크포인트로 이동했다.");

            var dialog = Object.FindFirstObjectByType<ConfirmDialog>();
            dialog.Confirm();
            yield return null;

            Assert.That(player.transform.position.x, Is.EqualTo(checkpoint.x).Within(0.01f));
            Assert.That(player.transform.position.y, Is.EqualTo(checkpoint.y).Within(0.01f));
            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.IsTrue(PlayerInput.Enabled);
        }

        [UnityTest]
        public IEnumerator 지도와_기억기록은_발견한_진행정보를_표시한다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            gm.Progress.ResetAll();
            gm.Progress.VisitRoom("기억의 입구");
            gm.Progress.CollectFragment("fragment-test", "돌아오라는 목소리");
            gm.SetState(GameState.Playing);

            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.OpenSection(PauseSection.Map);
            yield return null;
            Assert.AreEqual(GameState.Paused, gm.State);
            Assert.AreEqual(PauseSection.Map, pause.CurrentSection);
            Assert.That(GameObject.Find("SectionBody").GetComponent<Text>().text, Does.Contain("기억의 입구"));
            Assert.IsNotNull(GameObject.Find("SectionViewport").GetComponent<ScrollRect>(),
                "지도와 기억 기록은 긴 내용을 위한 스크롤 영역을 사용해야 한다.");

            pause.OpenSection(PauseSection.Journal);
            yield return null;
            Assert.That(GameObject.Find("SectionBody").GetComponent<Text>().text, Does.Contain("돌아오라는 목소리"));
            pause.Close();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        [UnityTest]
        public IEnumerator 설정을_바꾼_뒤에도_같은_항목에_포커스가_남는다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            gm.SetState(GameState.Playing);
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.OpenSection(PauseSection.Settings);
            yield return null;

            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected);
            Assert.That(selected.name, Does.StartWith("Button_전체 음량"));
            selected.GetComponent<Button>().onClick.Invoke();
            yield return null;

            selected = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected, "설정 목록을 다시 만든 뒤 포커스가 사라졌다.");
            Assert.That(selected.name, Does.StartWith("Button_전체 음량"));
            pause.Close();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        [UnityTest]
        public IEnumerator 연속_토스트는_앞_메시지를_취소하지_않고_대기열에_쌓인다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;

            var log = Object.FindAnyObjectByType<FragmentLog>();
            Assert.IsNotNull(log);
            log.Show("첫 기억", 0.1f);
            log.Show("두 번째 기억", 0.1f);
            Assert.GreaterOrEqual(log.PendingCount, 2);
        }

        [UnityTest]
        public IEnumerator 보스_체력바는_현재체력_비율만큼_실제폭이_줄어든다()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Residue_Full", LoadSceneMode.Single);
            yield return null;

            var hud = Object.FindAnyObjectByType<HUD>();
            Assert.IsNotNull(hud);
            var update = typeof(HUD).GetMethod("HandleBossHealthChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(update);
            update.Invoke(hud, new object[] { 4, 8 });

            var fillField = typeof(HUD).GetField("_bossHealthFill",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var fill = fillField?.GetValue(hud) as Image;
            Assert.IsNotNull(fill);
            Assert.That(fill.rectTransform.anchorMax.x, Is.EqualTo(0.5f).Within(0.001f),
                "보스 체력이 절반인데 하단 게이지 실제 폭이 줄지 않는다.");
        }
    }
}
