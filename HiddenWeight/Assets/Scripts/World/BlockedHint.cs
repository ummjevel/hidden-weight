using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Data;
using HiddenWeight.Enemies;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.World
{
    // 지금 길을 막고 있는 것이 "왜" 막고 있는지 알려 주는 월드 문구.
    //
    // 막힌 벽은 생김새만으로 이유를 말해 주지 못한다. 전투 잠금벽·능력 게이트·아직 열지 않은
    // 숏컷이 전부 같은 벽으로 보이면, 플레이어는 넘어갈 수 있는 벽인 줄 알고 벽점프를 시도하다
    // 시간을 버린다(실제 QA에서 나온 문제다). 이유를 한 줄로 말해 주면 그 자리에서 판단이 끝난다.
    //
    // TutorialHint와 같은 월드 캔버스 방식이지만, 문구가 고정이 아니라 대상의
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

        Text _text;
        Transform _textRoot;
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
            // 잔재 지역 출구는 ZoneTrigger 자체의 ExitLabel을 사용한다. 예전에 저장되었거나
            // 다른 런타임 경로에서 붙은 BlockedHint도 여기서 막아 중복 문구를 방지한다.
            if (_zoneTrigger != null && gameObject.scene.name.Contains("Residue"))
            {
                enabled = false;
                return;
            }

            _text = UIBuilder.CreateWorldText(null, "BlockedHintText", new Vector2(900f, 150f),
                0.01f, 48, 40);
            _textRoot = _text.transform.parent;
            _textRoot.SetParent(transform, true);
            _textRoot.position = transform.position + Vector3.up * TextHeight;
            _text.color = new Color(1f, 1f, 1f, 0f);
        }

        void Update()
        {
            if (_text == null) return;

            // 잔재의 되감기 대상은 중력으로 쓰러질 수 있지만 안내문까지 함께 눕거나 세로로
            // 돌면 읽을 수 없다. 다른 지역의 기존 월드 문구 동작은 건드리지 않는다.
            if (gameObject.scene.name.Contains("Residue"))
            {
                _textRoot.position = transform.position + Vector3.up * TextHeight;
                _textRoot.rotation = Quaternion.identity;
            }

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
            bool residue = gameObject.scene.name.Contains("Residue");

            if (_encounter != null)
                return _encounter.IsRunning && !_encounter.IsFinished
                    ? (residue ? "적을 모두 처치하면 열립니다." : "적을 모두 물리쳐야 열린다")
                    : null;

            if (_gate != null)
                return _gate.IsOpen ? null : GateMessage(_gate.RequiredSkill, residue);

            if (_shortcut != null)
                // 잔재의 숏컷 장치는 작은 상자처럼 보여 문구가 붙으면 보상 상자로
                // 오해하기 쉽다. 개방 조건은 되감기·보스 흐름에서 이미 안내하므로
                // 잔재에서는 별도의 장치 문구를 띄우지 않는다.
                return _shortcut.IsOpen || gameObject.scene.name.Contains("Residue")
                    ? null
                    : "반대편에서만 열 수 있다";

            // 지역 출구는 조건을 못 채웠을 때만 이유를 말한다. 열려 있으면 굳이 안내하지
            // 않는다 — 지나가면 되는 곳에 문구가 남아 있으면 읽을 것이 늘기만 한다.
            if (_zoneTrigger != null)
                return _zoneTrigger.IsOpen
                    ? null
                    : (residue ? "기억의 교수자를 처치하면 열립니다." : "이 지역을 끝내야 나갈 수 있다");

            // 방 문은 반대다. 막지 않지만 "여기가 다음 방으로 가는 길"이라는 안내가 필요하다.
            // 문 너머는 아직 로드되지 않은 다른 씬이라 눈으로는 길인지 벽인지 알 수 없다.
            //
            // 문구는 "다음 방으로"였다. 방·씬은 만드는 쪽의 단어지 이 세계의 단어가 아니고,
            // 기능 안내로 읽혀 배경의 정서적 무게를 깎았다(브랜드 성격 "몽환적").
            // 무엇을 하면 되는지는 그대로 남기되 세계의 말로 바꾼다.
            if (_door != null)
                return "이 너머로 이어진다";

            // 되감기로 되돌릴 수 있는 것은, 되돌릴 수 있는 상태일 때만 알려 준다.
            if (_rewindable != null)
                return _rewindable.CanRewind
                    ? (residue ? "되감기로 복원할 수 있습니다." : "되감기로 되돌릴 수 있다")
                    : null;

            return null;
        }

        static string GateMessage(EmotionId skill, bool residue)
        {
            if (residue && skill == EmotionId.Rewind)
                return "되감기가 필요합니다.";

            return skill switch
            {
                EmotionId.Rewind => "되감기가 있어야 지나갈 수 있다",
                EmotionId.Hush => "숨죽이기가 있어야 지나갈 수 있다",
                EmotionId.Foresight => "예지가 있어야 지나갈 수 있다",
                _ => "아직 열리지 않았다",
            };
        }
    }
}
