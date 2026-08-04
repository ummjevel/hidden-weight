using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using HiddenWeight.Core;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.UI;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class PrologueLayoutTests
    {
        [SetUp]
        public void Setup() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Teardown() => PlayerInput.Injected = null;

        [UnityTest]
        public IEnumerator 문서대로_T01부터_T04까지_네_방이_있다()
        {
            yield return LoadPrologue();

            var names = new HashSet<string>();
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                names.Add(room.name);

            CollectionAssert.IsSubsetOf(new[] { "T01", "T02", "T03", "T04" }, names);
            Assert.AreEqual(4, names.Count, "프롤로그는 T01~T04 네 방만 사용한다.");
        }

        [UnityTest]
        public IEnumerator T01부터_T04까지_각자_승인된_배경을_사용한다()
        {
            yield return LoadPrologue();

            foreach (string roomName in new[] { "T01", "T02", "T03", "T04" })
            {
                var roomObject = GameObject.Find(roomName);
                Assert.IsNotNull(roomObject, $"{roomName} 방을 찾을 수 없다.");

                var background = roomObject.transform.Find("Art/RoomBackground");
                Assert.IsNotNull(background, $"{roomName}에 RoomBackground가 없다.");

                var renderer = background.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer != null ? renderer.sprite : null,
                    $"{roomName} 배경 스프라이트가 연결되지 않았다.");
                Assert.AreEqual(roomName, renderer.sprite.name,
                    $"{roomName}이 다른 방의 배경을 참조한다.");
                Assert.GreaterOrEqual(renderer.sprite.texture.width, 1600,
                    $"{roomName} 배경 가로 해상도가 검수 시안보다 작다.");
                Assert.IsNotNull(background.GetComponent<CameraLockedRoomBackground>(),
                    $"{roomName} 배경이 카메라 화면을 채우도록 설정되지 않았다.");
            }
        }

        [UnityTest]
        public IEnumerator 우주_배경은_밝게_보이고_회색_충돌타일은_앞을_가리지_않는다()
        {
            yield return LoadPrologue();

            foreach (var background in Object.FindObjectsByType<CameraLockedRoomBackground>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var renderer = background.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer != null ? renderer.sprite : null, "튜토리얼 우주 배경이 없다.");
                Assert.IsTrue(renderer.enabled,
                    $"{background.transform.parent.parent.name} 우주 배경 렌더러가 꺼졌다.");
                Assert.GreaterOrEqual(renderer.color.a, 0.85f,
                    $"{background.transform.parent.parent.name} 우주 배경이 너무 어둡다.");
                Assert.GreaterOrEqual(renderer.color.b, 0.9f,
                    $"{background.transform.parent.parent.name} 우주색이 회색으로 눌렸다.");
            }

            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.LessOrEqual(tilemap.color.a, 0.1f,
                    $"{tilemap.name} 회색 충돌 타일이 우주 배경을 가린다.");
        }

        [UnityTest]
        public IEnumerator 튜토리얼_길은_잔재가_아닌_전용_우주_지형을_사용한다()
        {
            yield return LoadPrologue();

            var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
            Assert.IsNotNull(palette, "보행 바닥 팔레트를 불러오지 못했다.");

            var prologueSurface = palette.SurfaceFor("Zone_Prologue");
            Assert.IsNotNull(prologueSurface, "튜토리얼 전용 바닥 스프라이트가 연결되지 않았다.");
            Assert.AreEqual("Prologue_TraversalSurface_v2", prologueSurface.name);
            Assert.AreNotSame(palette.SurfaceFor("Zone_Residue_Full"), prologueSurface,
                "튜토리얼이 잔재 바닥으로 대체되면 안 된다.");
            Assert.IsNotNull(palette.prologueFill, "튜토리얼 일반 지형용 채움 텍스처가 없다.");
            Assert.AreEqual("Prologue_TraversalFill_v1", palette.prologueFill.name);
        }

        [UnityTest]
        public IEnumerator T01_일반_단차는_벽타기_기둥으로_보이지_않는다()
        {
            yield return LoadPrologue();

            var room = GameObject.Find("T01").GetComponent<Room>();
            int masses = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!room.WorldBounds.Contains(renderer.bounds.center)) continue;
                Assert.AreNotEqual("PrologueWallFace", renderer.name,
                    "T01의 일반 턱에 구형 벽 기둥 이미지가 남아 있다.");
                if (renderer.name != "PrologueGroundMass") continue;
                Assert.AreEqual(SpriteDrawMode.Tiled, renderer.drawMode,
                    "T01 바닥 채움 이미지가 늘어나 보인다.");
                masses++;
            }
            Assert.Greater(masses, 0, "T01의 평평한 지형 채움 이미지가 없다.");
        }

        [UnityTest]
        public IEnumerator 튜토리얼_바닥_위에_충돌확인용_굵은_회색선이_남지_않는다()
        {
            yield return LoadPrologue();

            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                Assert.AreNotEqual("TraversalEdge", renderer.name,
                    "튜토리얼 일반 바닥 위에 굵은 충돌선이 남아 있다.");
                Assert.IsFalse(renderer.name.StartsWith("PlatformEdge"),
                    "튜토리얼 독립 발판에 굵은 회색 테두리가 남아 있다.");
            }
        }

        [UnityTest]
        public IEnumerator 튜토리얼_바닥_이미지의_윗면은_실제_충돌면과_일치한다()
        {
            yield return LoadPrologue();
            int groundMask = LayerMask.GetMask("Ground");
            int checkedCount = 0;

            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.name != "TraversalSurface" && renderer.name != "PlatformSurface")
                    continue;

                float artTop = renderer.bounds.max.y;
                var hit = Physics2D.Raycast(
                    new Vector2(renderer.bounds.center.x, artTop + 0.25f),
                    Vector2.down, 0.5f, groundMask);
                Assert.IsNotNull(hit.collider, renderer.name + " 위에 실제 바닥 충돌이 없다.");
                Assert.That(hit.point.y, Is.EqualTo(artTop).Within(0.06f),
                    renderer.name + " 이미지와 충돌면 사이에 떠 보이는 간격이 있다.");
                checkedCount++;
            }

            Assert.Greater(checkedCount, 3, "검사할 튜토리얼 바닥 이미지가 부족하다.");
        }

        [UnityTest]
        public IEnumerator 생성한_궤도_손_성운_파편이_각_방에_하나씩_배치된다()
        {
            yield return LoadPrologue();

            var expected = new Dictionary<string, string>
            {
                { "T01", "Prologue_OrbitRing" },
                { "T02", "Prologue_ConstellationHand" },
                { "T03", "Prologue_NebulaMist" },
                { "T04", "Prologue_FragmentShard" },
            };
            foreach (var pair in expected)
            {
                var decor = GameObject.Find(pair.Key).transform.Find("Art/" + pair.Value);
                Assert.IsNotNull(decor, $"{pair.Key}에 {pair.Value} 장식이 없다.");
                Assert.IsNotNull(decor.GetComponent<SpriteRenderer>().sprite);
                Assert.IsNull(decor.GetComponent<Collider2D>(),
                    "튜토리얼 장식은 보이지 않는 충돌을 만들면 안 된다.");
            }
        }

        [UnityTest]
        public IEnumerator 다섯_기본_행동_안내와_첫_전투가_배치돼_있다()
        {
            yield return LoadPrologue();

            var actions = new HashSet<PrologueActionHint.RequiredAction>();
            foreach (var hint in Object.FindObjectsByType<PrologueActionHint>(FindObjectsSortMode.None))
                actions.Add(hint.Action);

            Assert.AreEqual(5, actions.Count, "이동·점프·벽점프·대시·공격 안내가 각각 하나씩 필요하다.");
            Assert.AreEqual(2, Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length,
                "T03 학습 적과 T04 종합 적이 한 마리씩 필요하다.");
        }

        [UnityTest]
        public IEnumerator 조작_안내는_그_지점에_도착하면_실제로_화면에_보인다()
        {
            yield return LoadPrologue();
            var player = PlayerController.Instance;

            foreach (var hint in Object.FindObjectsByType<PrologueActionHint>(FindObjectsSortMode.None))
            {
                player.TeleportTo(hint.transform.position);
                PlayerInput.Injected = default;
                yield return new WaitForSecondsRealtime(0.65f);

                var text = hint.GetComponentInChildren<TextMesh>();
                Assert.IsNotNull(text, hint.Action + " 안내 글자가 생성되지 않았다.");
                Assert.Greater(text.color.a, 0.25f,
                    hint.Action + " 안내가 범위 안에서도 투명하다.");
                Assert.IsFalse(hint.IsCompleted,
                    hint.Action + " 안내가 해당 행동 전에 완료 처리됐다.");
            }
        }

        [UnityTest]
        public IEnumerator 달려서_표시_지점을_지나도_안내가_충분히_오래_남는다()
        {
            yield return LoadPrologue();
            var moveHint = System.Array.Find(
                Object.FindObjectsByType<PrologueActionHint>(FindObjectsSortMode.None),
                hint => hint.Action == PrologueActionHint.RequiredAction.Move);
            Assert.IsNotNull(moveHint);

            PlayerInput.Injected = default;
            yield return new WaitForSecondsRealtime(0.65f);
            PlayerController.Instance.TeleportTo(new Vector3(14f, 1f, 0f));
            yield return new WaitForSecondsRealtime(3f);

            var text = moveHint.GetComponentInChildren<TextMesh>();
            Assert.IsNotNull(text);
            Assert.Greater(text.color.a, 0.25f,
                "달리는 속도보다 조작 안내 표시 시간이 짧다.");
            Assert.Less(Mathf.Abs(text.transform.position.x - Camera.main.transform.position.x), 0.1f,
                "조작 안내가 카메라를 따라오지 않아 화면 밖으로 사라졌다.");
        }

        [UnityTest]
        public IEnumerator 다음_조작_안내가_나오면_이전_안내는_겹치지_않는다()
        {
            yield return LoadPrologue();
            var hints = Object.FindObjectsByType<PrologueActionHint>(FindObjectsSortMode.None);
            var move = System.Array.Find(hints,
                hint => hint.Action == PrologueActionHint.RequiredAction.Move);
            var jump = System.Array.Find(hints,
                hint => hint.Action == PrologueActionHint.RequiredAction.Jump);

            PlayerController.Instance.TeleportTo(move.transform.position);
            PlayerInput.Injected = default;
            yield return new WaitForSecondsRealtime(0.65f);
            PlayerController.Instance.TeleportTo(jump.transform.position);
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.Less(move.GetComponentInChildren<TextMesh>().color.a, 0.01f,
                "이전 이동 안내가 다음 점프 안내와 겹친다.");
            Assert.Greater(jump.GetComponentInChildren<TextMesh>().color.a, 0.25f,
                "다음 점프 안내가 표시되지 않는다.");
        }

        [UnityTest]
        public IEnumerator 네_안전_구간에서_꿈의_정체와_목표를_안내한다()
        {
            yield return LoadPrologue();

            var hints = Object.FindObjectsByType<PrologueConceptHint>(FindObjectsSortMode.None);
            Assert.AreEqual(4, hints.Length, "T01~T04에 세계·목표 안내가 하나씩 필요하다.");

            var messages = new HashSet<string>();
            foreach (var hint in hints) messages.Add(hint.Message.Replace("\n", " "));

            CollectionAssert.IsSubsetOf(new[]
            {
                "이곳은 기억과 감정이 공간이 된 꿈이다.",
                "지나간 일, 지금의 시선, 아직 오지 않은 걱정이 세 공간을 만들었다.",
                "세 공간의 기억을 모으면 이 꿈에서 깨어날 수 있다.",
                "첫 번째 공간 · 잔재 지나간 기억이 남은 곳",
            }, messages);
        }

        [UnityTest]
        public IEnumerator 안내_문구는_나눔명조와_확대된_크기를_사용한다()
        {
            yield return LoadPrologue();

            var actionFont = Resources.Load<Font>("Fonts/NanumMyeongjo-Bold");
            var conceptFont = Resources.Load<Font>("Fonts/NanumMyeongjo-Regular");
            Assert.IsNotNull(actionFont, "조작 안내용 나눔명조 Bold가 Resources에 없다.");
            Assert.IsNotNull(conceptFont, "세계 설명용 나눔명조 Regular가 Resources에 없다.");

            foreach (var hint in Object.FindObjectsByType<PrologueActionHint>(FindObjectsSortMode.None))
            {
                var text = hint.GetComponentInChildren<TextMesh>();
                Assert.AreSame(actionFont, text.font, hint.Action + " 안내가 나눔명조 Bold를 사용하지 않는다.");
                Assert.GreaterOrEqual(text.fontSize, 64);
                Assert.GreaterOrEqual(text.characterSize, 0.07f);
            }

            foreach (var hint in Object.FindObjectsByType<PrologueConceptHint>(FindObjectsSortMode.None))
            {
                var text = hint.GetComponentInChildren<TextMesh>();
                Assert.AreSame(conceptFont, text.font, "세계 설명이 나눔명조 Regular를 사용하지 않는다.");
                Assert.GreaterOrEqual(text.fontSize, 60);
                Assert.GreaterOrEqual(text.characterSize, 0.06f);
            }
        }

        [UnityTest]
        public IEnumerator 시작과_출구_앞에_체크포인트가_있고_끝에서_잔재로_간다()
        {
            yield return LoadPrologue();

            Assert.AreEqual(2, Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length);

            ZoneTrigger exit = null;
            foreach (var trigger in Object.FindObjectsByType<ZoneTrigger>(FindObjectsSortMode.None))
                if (trigger.transform.position.x > 110f) exit = trigger;

            Assert.IsNotNull(exit, "T04 동쪽 끝에 잔재 진입 트리거가 필요하다.");
            Assert.IsTrue(exit.GetComponent<BoxCollider2D>().isTrigger);
        }

        [UnityTest]
        public IEnumerator T04_굴뚝_입구와_대시_실패_우회로가_막히지_않는다()
        {
            yield return LoadPrologue();

            var leftWall = GameObject.Find("T04_Wall_Left").GetComponent<BoxCollider2D>();
            var rightWall = GameObject.Find("T04_Wall_Right").GetComponent<BoxCollider2D>();
            Assert.GreaterOrEqual(leftWall.bounds.min.y, 11.15f,
                "T04 굴뚝 왼쪽 벽 아래로 플레이어가 들어갈 틈이 없다.");
            Assert.GreaterOrEqual(rightWall.bounds.min.y, 11.15f,
                "T04 굴뚝 오른쪽 벽 아래로 플레이어가 들어갈 틈이 없다.");

            int groundMask = LayerMask.GetMask("Ground");
            float[] xs = { 102f, 103.5f, 104.5f, 105.5f, 107f };
            float[] expectedTops = { 14f, 15f, 16f, 17f, 18f };
            for (int i = 0; i < xs.Length; i++)
            {
                var hit = Physics2D.Raycast(new Vector2(xs[i], 25f), Vector2.down, 20f, groundMask);
                Assert.IsNotNull(hit.collider, $"T04 우회로 x={xs[i]}에 바닥이 없다.");
                Assert.That(hit.point.y, Is.EqualTo(expectedTops[i]).Within(0.1f),
                    $"T04 우회로 x={xs[i]}의 계단 높이가 1유닛씩 오르지 않는다.");
            }
        }

        [UnityTest]
        public IEnumerator T04_출구에_닿으면_잔재_전체맵으로_전환한다()
        {
            yield return LoadPrologue();

            var player = PlayerController.Instance;
            player.TeleportTo(new Vector3(110f, 19f, 0f));
            yield return new WaitForFixedUpdate();
            player.TeleportTo(new Vector3(113f, 19f, 0f));

            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name == "Zone_Prologue"
                && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.AreEqual("Zone_Residue_Full", SceneManager.GetActiveScene().name);
        }

        static IEnumerator LoadPrologue()
        {
            yield return SceneManager.LoadSceneAsync("Zone_Prologue", LoadSceneMode.Single);
            yield return null;
        }
    }
}
