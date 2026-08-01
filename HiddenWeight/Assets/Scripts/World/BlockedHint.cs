using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 지금 길을 막고 있는 것이 "왜" 막고 있는지 알려 주는 월드 문구.
    //
    // 막힌 벽은 생김새만으로 이유를 말해 주지 못한다. 전투 잠금벽·능력 게이트·아직 열지 않은
    // 숏컷이 전부 같은 벽으로 보이면, 플레이어는 넘어갈 수 있는 벽인 줄 알고 벽점프를 시도하다
    // 시간을 버린다(실제 QA에서 나온 문제다). 이유를 한 줄로 말해 주면 그 자리에서 판단이 끝난다.
    //
    // TutorialHint와 같은 방식(캔버스 없는 월드 TextMesh)이지만, 문구가 고정이 아니라 대상의
    // 현재 상태에서 나온다는 점이 다르다. 열리면 문구도 함께 사라진다.
    public class BlockedHint : MonoBehaviour
    {
        // 안내는 막힌 것을 알아차릴 만큼 가까울 때만 띄운다. 너무 넓으면 방을 지날 때마다
        // 상관없는 문구가 떠서 오히려 읽지 않게 된다.
        const float ShowRadius = 4.5f;
        const float FadeSpeed = 5f;
        const float TextHeight = 1.6f;

        Encounter _encounter;
        Gate _gate;
        Shortcut _shortcut;
        ZoneTrigger _zoneTrigger;
        RoomDoor _door;
        Rewindable _rewindable;

        TextMesh _text;
        float _alpha;

        public static BlockedHint AttachTo(GameObject target, Encounter encounter = null,
                                           Gate gate = null, Shortcut shortcut = null,
                                           ZoneTrigger zoneTrigger = null, RoomDoor door = null,
                                           Rewindable rewindable = null)
        {
            var hint = target.AddComponent<BlockedHint>();
            hint._encounter = encounter;
            hint._gate = gate;
            hint._shortcut = shortcut;
            hint._zoneTrigger = zoneTrigger;
            hint._door = door;
            hint._rewindable = rewindable;
            return hint;
        }

        void Start()
        {
            var go = new GameObject("BlockedHintText");
            go.transform.SetParent(transform, false);

            // 부모(잠금벽)가 크기를 localScale에 싣고 있으면 글자까지 늘어난다. 월드 기준으로
            // 붙여 두고 크기를 직접 정한다.
            go.transform.SetParent(transform, true);
            go.transform.position = transform.position + Vector3.up * TextHeight;
            go.transform.localScale = Vector3.one;

            _text = go.AddComponent<TextMesh>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 48;
            _text.characterSize = 0.055f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = new Color(1f, 1f, 1f, 0f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = _text.font.material;
            renderer.sortingOrder = 40; // TutorialHint와 같은 층 — 항상 지형 위
        }

        void Update()
        {
            if (_text == null) return;

            string message = CurrentMessage();
            var player = PlayerController.Instance;

            bool show = message != null && player != null
                && Vector2.Distance(player.transform.position, transform.position) <= ShowRadius;

            if (show) _text.text = message;

            _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f, FadeSpeed * Time.deltaTime);
            _text.color = new Color(1f, 0.86f, 0.55f, _alpha * 0.95f);
        }

        // 막고 있지 않으면 null. 그래야 열린 뒤에는 문구가 저절로 사라진다.
        string CurrentMessage()
        {
            if (_encounter != null)
                return _encounter.IsRunning && !_encounter.IsFinished
                    ? "적을 모두 물리쳐야 열린다"
                    : null;

            if (_gate != null)
                return _gate.IsOpen ? null : GateMessage(_gate.RequiredSkill);

            if (_shortcut != null)
                return _shortcut.IsOpen ? null : "반대편에서만 열 수 있다";

            // 지역 출구는 조건을 못 채웠을 때만 이유를 말한다. 열려 있으면 굳이 안내하지
            // 않는다 — 지나가면 되는 곳에 문구가 남아 있으면 읽을 것이 늘기만 한다.
            if (_zoneTrigger != null)
                return _zoneTrigger.IsOpen ? null : "이 지역을 끝내야 나갈 수 있다";

            // 방 문은 반대다. 막지 않지만 "여기가 다음 방으로 가는 길"이라는 안내가 필요하다.
            // 문 너머는 아직 로드되지 않은 다른 씬이라 눈으로는 길인지 벽인지 알 수 없다.
            if (_door != null)
                return "다음 방으로";

            // 되감기로 되돌릴 수 있는 것은, 되돌릴 수 있는 상태일 때만 알려 준다.
            if (_rewindable != null)
                return _rewindable.CanRewind ? "되감기로 되돌릴 수 있다" : null;

            return null;
        }

        static string GateMessage(EmotionId skill) => skill switch
        {
            EmotionId.Rewind => "되감기가 있어야 지나갈 수 있다",
            EmotionId.Hush => "숨죽이기가 있어야 지나갈 수 있다",
            EmotionId.Foresight => "예지가 있어야 지나갈 수 있다",
            _ => "아직 열리지 않았다",
        };
    }
}
