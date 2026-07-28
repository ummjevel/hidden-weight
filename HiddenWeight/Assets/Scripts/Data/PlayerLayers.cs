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
            Cache();
            return go.layer == _player || go.layer == _hushed;
        }

        // 지금 숨죽이기 중인가. 응시 지역의 적 3종이 전부 "숨죽인 플레이어에게는 다르게
        // 반응한다"(GAZE_LEVEL_DESIGN.md 6.1절)라서, 레이어 비교를 각 행동 모듈이 따로
        // 하지 않도록 여기 한 곳에 둔다.
        public static bool IsHushed(GameObject go)
        {
            Cache();
            return go.layer == _hushed;
        }

        static void Cache()
        {
            if (_player >= 0) return;
            _player = LayerMask.NameToLayer("Player");
            _hushed = LayerMask.NameToLayer("PlayerHushed");
        }
    }
}
