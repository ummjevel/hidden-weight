using UnityEngine;

namespace HiddenWeight.World
{
    // 주기 운동을 하는 것들이 공유하는 지역 시계.
    //
    // 설계는 두 가지를 동시에 요구한다.
    //   - 예지 고스트가 가리킨 위치와 실제 2초 뒤가 항상 일치한다(7.2절)
    //   - 이동·붕괴 발판의 위상은 사망 후 항상 같은 시작값으로 돌아가, 실패에서
    //     배울 수 있게 한다(10절)
    //
    // Time.time을 그대로 쓰면 앞의 것만 만족한다 — 죽고 다시 시도할 때마다 발판이
    // 제각각의 위상에 있어서, 같은 점프가 시도마다 다른 타이밍을 요구한다. 그러면
    // 실패가 학습으로 이어지지 않는다.
    //
    // 그래서 시계를 하나 두고 사망·체크포인트 복귀·방 진입에서 0으로 되돌린다.
    // 예지도 같은 시계를 읽으므로(대상들이 PositionAt(Now + lead)를 쓴다) 앞의 조건은
    // 그대로 유지된다. 리셋은 "지금 이 순간"을 옮기지 않고 원점만 옮기기 때문이다.
    public static class ZoneClock
    {
        static float _origin;

        // 주기 운동이 참조할 시각. Time.time 대신 이것을 쓴다.
        public static float Now => Time.time - _origin;

        // 위상을 처음으로 되돌린다. 되돌린 직후 Now는 0이다.
        public static void Reset() => _origin = Time.time;
    }
}
