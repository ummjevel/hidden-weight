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
            ConfigurePassages();
            ConfigureFinalEncounter();
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
                fragment.Configure("residue_core", "가르치려던 목소리가 멎자, 남은 것은 내가 고른 기억뿐이었다.");
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
