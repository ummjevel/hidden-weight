using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Enemies;

namespace HiddenWeight.World
{
    // 기존 Full 씬과 빌더 결과 양쪽에 잔재 완주용 연결을 보장한다. 씬을 다시 생성하지 않아도
    // 숏컷과 비밀방이 실제 통로가 되고, 지역 출구가 보스 승리를 요구한다.
    public sealed class ResidueLoopRuntime : MonoBehaviour
    {
        public static void Install(Transform parent)
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Residue_Full")) return;
            if (FindFirstObjectByType<ResidueLoopRuntime>() != null) return;
            var go = new GameObject("ResidueLoopRuntime");
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<ResidueLoopRuntime>();
        }

        IEnumerator Start()
        {
            if (GetComponent<ResidueAmbientAudio>() == null) gameObject.AddComponent<ResidueAmbientAudio>();
            // 각 컴포넌트의 Start가 초기 상태를 적용한 다음 연결한다.
            yield return null;
            ConfigureR05PrimaryRestore();
            ConfigureR05OptionalRestoreAndRewards();
            ConfigureR08Pulleys();
            ConfigureR10ArenaEntrance();
            ConfigureR10MidBoss();
            ConfigureOptionalMainPathEncounters();
            ConfigurePassages();
            ConfigureFinalEncounter();
        }

        void ConfigureR10ArenaEntrance()
        {
            // R10의 보스 조우는 자체 Lock_L/Lock_R을 전투 시작 2초 뒤 켠다. 예전 빌드의
            // R10_Wall_L/R까지 상시 활성화되어 있으면 서쪽 입구에서 바닥 진행이 완전히
            // 막히고, 벽 위도 천장 때문에 오를 수 없다. 상시 벽만 제거하고 전투 잠금은 유지한다.
            foreach (string name in new[]
                     {
                         "R10_Wall_L", "R10_Wall_R",
                         "R12_Wall_L", "R12_Wall_R",
                     })
            {
                var wall = GameObject.Find(name);
                if (wall != null) wall.SetActive(false);
            }
        }

        void ConfigureR10MidBoss()
        {
            var room = FindRoom("Room10");
            if (room == null) return;

            // 기존 Full 씬에도 즉시 반영한다. R12 최종 보스는 Room10 밖이므로 건드리지 않는다.
            foreach (var candidate in FindObjectsByType<BossController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!room.WorldBounds.Contains(candidate.transform.position)) continue;

                candidate.transform.localScale = Vector3.one * 1.25f;
                candidate.ConfigureDifficulty(1.6f, 8f, 0.9f, 1.25f, 1.4f, 1.25f, 4.2f);
                candidate.GetComponentInChildren<SpriteAnimator>(true)
                    ?.LockReferenceCenterToLocalX(0f);
            }

            // 저장된 Full 씬의 두 y=9 발판을 재활용해 출구 계단으로 내린다. 별도 충돌체를
            // 겹쳐 만들지 않으므로 보스가 죽은 뒤 계단 모서리에 끼는 문제도 생기지 않는다.
            var platforms = new List<BoxCollider2D>();
            foreach (var platform in FindObjectsByType<BoxCollider2D>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (platform.gameObject.name != "SafePlatform"
                    && !platform.gameObject.name.StartsWith("R10_ExitStep_")) continue;
                if (!room.WorldBounds.Contains(platform.bounds.center)) continue;
                platforms.Add(platform);
            }
            platforms.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            if (platforms.Count < 2) return;

            PlaceExitStep(platforms[0], "R10_ExitStep_Low", room, 19.25f, 5f);
            PlaceExitStep(platforms[platforms.Count - 1], "R10_ExitStep_High", room, 22f, 7f);
        }

        static void PlaceExitStep(BoxCollider2D platform, string name, Room room, float x, float y)
        {
            platform.gameObject.name = name;
            platform.transform.position = (Vector2)room.WorldBounds.min + new Vector2(x, y);
        }

        void ConfigureOptionalMainPathEncounters()
        {
            // R09의 일반·정예는 선택 전투다. 놓친 적을 찾아 역주행하지 않아도 R10에
            // 도착하면 중간 보스가 시작된다. R10 중간 보스와 R12 최종 보스는 보스전
            // 자체의 완결을 위해 기존처럼 처치할 때까지 전장을 잠근다.
            var optionalIds = new HashSet<string>
            {
                "residue_r09_main",
                "residue_r09_elite",
            };

            foreach (var encounter in FindObjectsByType<Encounter>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (optionalIds.Contains(encounter.Id))
                    encounter.ConfigureTraversalLock(false);
        }

        void ConfigureR08Pulleys()
        {
            foreach (string name in new[] { "R08_PulleySafe", "R08_PulleyFast" })
            {
                var pulley = GameObject.Find(name);
                if (pulley == null) continue;
                var body = pulley.GetComponent<Rigidbody2D>();
                if (body == null) continue;

                // 파손 위치로 떨어지는 것은 유지하되 옆으로 구르지는 않게 한다. 장치와 K 표식이
                // 누워 보이면 무엇을 복원하는지 읽히지 않는다.
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
                body.angularVelocity = 0f;
                pulley.transform.rotation = Quaternion.identity;
            }
        }

        void ConfigureR05PrimaryRestore()
        {
            var primary = GameObject.Find("R05_PrimaryRestore")?.GetComponent<Rewindable>();
            var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
            Sprite platform = palette != null ? palette.ResiduePlatformFor(3f) : null;
            if (primary == null || platform == null) return;

            // 기획대로 오른쪽 턱까지 끊김 없이 잇는 폭 3을 유지한다. 복원 순간 겹침은
            // Rewindable이 실제 플레이어 몸통만 판별해 발판 바로 위로 빼낸다.
            // 바닥 표면(local y=2)과 발판 밑면 사이를 1.5유닛 확보한다. 기존 1유닛은
            // 플레이어 몸통보다 낮아 채널링 직후 아래에 끼었다.
            // Short 발판 원화 상단 약 20%는 난간 장식이다. 돌 보행면을 실제 충돌면에 맞춘다.
            primary.ConfigureRestoredPlatform(platform, new Vector2(3f, 1f),
                new Vector2(0f, 0.5f), 0.20f);
        }

        void ConfigureR05OptionalRestoreAndRewards()
        {
            var room = FindRoom("Room05");
            var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
            Sprite platform = palette != null ? palette.ResiduePlatformFor(3f) : null;
            if (room == null || platform == null) return;

            // 두 번째 대상은 복원 뒤 폭 3의 선택 발판이 된다. 잔해/장치 그림을 그대로
            // 고정하면 판정과 그림의 윗면이 달라 공중에 서는 것처럼 보인다.
            Vector2 optionalPoint = (Vector2)room.WorldBounds.min + new Vector2(20f, 6.5f);
            Rewindable optional = null;
            float bestDistance = float.MaxValue;
            foreach (var rewindable in FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
            {
                if (!room.WorldBounds.Contains(rewindable.transform.position)
                    || rewindable.name == "R05_PrimaryRestore"
                    || rewindable.name == "R05_ChainDevice") continue;
                float distance = Vector2.Distance(rewindable.transform.position, optionalPoint);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                optional = rewindable;
            }
            if (optional != null && bestDistance < 1f)
                optional.ConfigureRestoredPlatform(platform, new Vector2(3f, 1f),
                    default, 0.20f);

            // 보상 4개를 좁은 직선으로 겹치면 하나의 충돌 없는 바닥처럼 보인다.
            // 실제 선택 발판 위에 성긴 호로 배치해 "따라가서 줍는 물체"로 읽히게 한다.
            var rewards = new List<CurrencyPickup>();
            foreach (var pickup in FindObjectsByType<CurrencyPickup>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (!room.WorldBounds.Contains(pickup.transform.position)) continue;
                Vector2 local = pickup.transform.position - room.WorldBounds.min;
                if (local.x >= 18f && local.x <= 23f && local.y >= 7f)
                    rewards.Add(pickup);
            }
            rewards.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            Vector2[] arc =
            {
                new Vector2(18.3f, 8.1f), new Vector2(19.8f, 8.7f),
                new Vector2(21.3f, 8.7f), new Vector2(22.8f, 8.1f),
            };
            for (int i = 0; i < rewards.Count && i < arc.Length; i++)
                rewards[i].transform.position = (Vector2)room.WorldBounds.min + arc[i];
        }

        void ConfigurePassages()
        {
            var shortcuts = new Dictionary<string, Shortcut>();
            foreach (var shortcut in FindObjectsByType<Shortcut>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                shortcuts[shortcut.Id] = shortcut;

            Pair("ShortcutPassage_A", RoomPoint("Room05", 2f, 3f), RoomPoint("Room03", 6f, 3f),
                Get(shortcuts, "residue_shortcut_a"));
            Pair("ShortcutPassage_B", RoomPoint("Room08", 21f, 26.5f), RoomPoint("Room03", 23f, 2f),
                Get(shortcuts, "residue_shortcut_b"));
            Pair("ShortcutPassage_C", RoomPoint("Room10", 3.5f, 4f), RoomPoint("Room07", 25f, 9f),
                Get(shortcuts, "residue_shortcut_c"));

            // S1은 첫 방문부터 발견 가능한 무조건 열린 바닥 틈이다.
            Pair("SecretPassage_S1", RoomPoint("Room04", 7.5f, 6.5f), RoomPoint("Secret01", 2f, 2.5f), null);

            // S2는 R06 선택 되감기 대상을 복원해야만 열린다.
            var secret = new GameObject("Shortcut_residue_secret_s2").AddComponent<Shortcut>();
            secret.transform.SetParent(transform, false);
            var cover = BuildSecretCover();
            secret.Configure("residue_secret_s2", cover);
            Pair("SecretPassage_S2", RoomPoint("Room06", 21f, 6f), RoomPoint("Secret02", 4f, 11f), secret);

            // S2는 지도상의 A/B/C 물리 숏컷이 아니라 선택 되감기의 논리 게이트다.
            // 비활성 상태 객체로 두면 기존 숏컷 3개의 봉쇄 애니메이션 계약을 흐리지 않으면서
            // 통로는 IsOpen 상태를 계속 참조할 수 있다. Rewindable.Open 호출도 정상 동작한다.
            secret.gameObject.SetActive(false);

            var r06 = FindRoom("Room06");
            Rewindable selected = null;
            float best = float.MaxValue;
            if (r06 != null)
            {
                var desired = (Vector2)r06.WorldBounds.min + new Vector2(21f, 6f);
                foreach (var rewindable in FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
                {
                    if (!r06.WorldBounds.Contains(rewindable.transform.position)) continue;
                    float distance = Vector2.Distance(desired, rewindable.transform.position);
                    if (distance < best) { best = distance; selected = rewindable; }
                }
            }
            if (selected != null) selected.ConfigureLinkedShortcut(secret);
        }

        GameObject BuildSecretCover()
        {
            // 상단은 R06 바닥 표면(local y=5)에 맞추고 아래쪽 샤프트 5유닛을 채운다.
            // 얇은 1유닛 판은 대시 중 연속 충돌이 아니면 관통할 수 있다.
            var position = RoomPoint("Room06", 20f, 2.5f);
            if (position == Vector2.negativeInfinity) return null;

            var cover = new GameObject("SecretPassage_S2_ClosedFloor");
            cover.transform.SetParent(transform, false);
            cover.transform.position = position;
            cover.layer = LayerMask.NameToLayer("Ground");
            var collider = cover.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(5f, 5f);
            return cover;
        }

        void ConfigureFinalEncounter()
        {
            var r12 = FindRoom("Room12");
            if (r12 == null) return;

            Encounter encounter = null;
            foreach (var candidate in FindObjectsByType<Encounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.Id == "residue_r12_boss") { encounter = candidate; break; }
            if (encounter == null) return;

            foreach (var trigger in FindObjectsByType<ZoneTrigger>(FindObjectsSortMode.None))
                if (r12.WorldBounds.Contains(trigger.transform.position))
                    trigger.RequireEncounter(encounter.Id);

            BossController boss = null;
            var arena = new List<Rewindable>();
            foreach (var candidate in FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r12.WorldBounds.Contains(candidate.transform.position)) { boss = candidate; break; }
            foreach (var rewindable in FindObjectsByType<Rewindable>(FindObjectsSortMode.None))
                if (r12.WorldBounds.Contains(rewindable.transform.position)) arena.Add(rewindable);
            if (boss != null)
            {
                boss.ConfigureArena(arena.ToArray());
                boss.ConfigurePresentation("InstructorRecover", "InstructorBlade", "InstructorHook",
                    "InstructorSlam", "InstructorHook", "InstructorPhase");
            }

            // 지역 보스의 핵심 기억은 승리한 뒤에만 나타난다.
            StoryFragment template = null;
            StoryFragment existingCore = null;
            foreach (var fragment in FindObjectsByType<StoryFragment>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (fragment.FragmentId == "residue_r11") template = fragment;
                if (fragment.FragmentId == "residue_core") existingCore = fragment;
            }
            if (existingCore != null)
            {
                encounter.RegisterVictoryObject(existingCore.gameObject);
            }
            else if (template != null)
            {
                var core = Instantiate(template.gameObject, RoomPoint("Room12", 24f, 4.5f), Quaternion.identity, transform);
                core.name = "StoryFragment_residue_core";
                var fragment = core.GetComponent<StoryFragment>();
                fragment.Configure("residue_core", "기억의 교수대에서 발견한 파편.");
                core.SetActive(false);
                encounter.RegisterVictoryObject(core);
            }
        }

        static Shortcut Get(Dictionary<string, Shortcut> values, string id)
            => values.TryGetValue(id, out var found) ? found : null;

        void Pair(string name, Vector2 a, Vector2 b, Shortcut shortcut)
        {
            if (a == Vector2.negativeInfinity || b == Vector2.negativeInfinity) return;
            var anchorA = Passage(name + "_A", a);
            var anchorB = Passage(name + "_B", b);
            AddPassageVisual(anchorA, shortcut);
            AddPassageVisual(anchorB, shortcut);
            anchorA.GetComponent<ShortcutPassage>().Configure(shortcut, anchorB.transform, new Vector2(1.1f, 0.8f));
            anchorB.GetComponent<ShortcutPassage>().Configure(shortcut, anchorA.transform, new Vector2(1.1f, 0.8f));
        }

        void AddPassageVisual(GameObject passage, Shortcut shortcut)
        {
            Sprite sprite = null;
            if (shortcut != null)
            {
                var source = shortcut.GetComponentInChildren<SpriteRenderer>(true);
                if (source != null) sprite = source.sprite;
            }
            if (sprite == null)
            {
                var fragment = FindFirstObjectByType<StoryFragment>();
                var source = fragment != null ? fragment.GetComponentInChildren<SpriteRenderer>() : null;
                if (source != null) sprite = source.sprite;
            }
            if (sprite == null) return;

            var visual = new GameObject("PassageVisual");
            visual.transform.SetParent(passage.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.86f, 0.72f, 0.42f, 0.72f);
            renderer.sortingOrder = 6;
            float height = sprite.bounds.size.y;
            if (height > 0f) visual.transform.localScale = Vector3.one * (1.4f / height);
        }

        GameObject Passage(string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.8f;
            go.AddComponent<ShortcutPassage>();
            return go;
        }

        static Room FindRoom(string name)
        {
            foreach (var room in FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (room.name == name) return room;
            return null;
        }

        static Vector2 RoomPoint(string roomName, float x, float y)
        {
            var room = FindRoom(roomName);
            return room == null ? Vector2.negativeInfinity : (Vector2)room.WorldBounds.min + new Vector2(x, y);
        }
    }
}
