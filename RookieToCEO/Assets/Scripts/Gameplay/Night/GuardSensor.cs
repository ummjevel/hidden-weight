using UnityEngine;

namespace RookieToCEO.Gameplay.Night
{
    // GDD 10번 "경비원". 시야 방향은 씬에서 오브젝트를 회전시켜 배치하는 것으로 결정한다
    // (프로토타입 범위 - 순찰 이동은 GDD에도 없어 보류).
    public class GuardSensor : VisionSensorBase
    {
        protected override Vector2 FacingDirection => transform.up;
    }
}
