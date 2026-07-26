using UnityEngine;

namespace HiddenWeight.Data
{
    // 플레이어 감지 판정 공용 헬퍼. 숨죽이기(HushSkill)가 홀드 중 플레이어 레이어를
    // Player → PlayerHushed로 바꿔치기하는 것과의 호환을 위해, "플레이어인지" 판정은
    // 항상 이 헬퍼를 통해야 한다 — Player 레이어와의 단순 동등 비교(==)는 숨죽이기 중
    // 플레이어를 놓친다(존 전환/체크포인트/파편/룸 카메라 전환/발판이 전부 먹통이 되는
    // 버그의 원인이었다). Data는 모든 모듈이 참조하는 유일한 리프 모듈이라, Core(World를
    // 참조할 수 없음)와 World가 동시에 안전하게 참조할 수 있는 곳은 여기뿐이다.
    // GazeHazard만 예외: playerMask에 Player만 넣어 숨죽이기 중 "안 보이게" 하는 것이
    // 정확히 의도된 동작이라 이 헬퍼를 쓰지 않는다(LayerMask 오버랩 방식이라 영향도 없다).
    public static class PlayerLayers
    {
        static int _player = -1, _hushed = -1;

        public static bool IsPlayer(GameObject go)
        {
            if (_player < 0)
            {
                _player = LayerMask.NameToLayer("Player");
                _hushed = LayerMask.NameToLayer("PlayerHushed");
            }
            return go.layer == _player || go.layer == _hushed;
        }
    }
}
