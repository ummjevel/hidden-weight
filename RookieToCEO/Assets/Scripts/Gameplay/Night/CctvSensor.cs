using UnityEngine;

namespace RookieToCEO.Gameplay.Night
{
    // GDD 10번 "CCTV". 경비원과 판정 로직은 같고(VisionSensorBase), 인스펙터에서
    // 더 넓은 각도/범위를 주는 것으로 "고정 감시" 느낌을 낸다.
    public class CctvSensor : VisionSensorBase
    {
        protected override Vector2 FacingDirection => transform.up;
    }
}
