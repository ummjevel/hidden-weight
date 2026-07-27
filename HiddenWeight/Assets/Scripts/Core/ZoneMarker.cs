using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // "이 씬은 어느 지역인가"를 씬이 직접 선언한다.
    //
    // 원래는 GameManager가 씬 이름을 ZoneData.sceneName과 대조해 알아냈는데, 한 지역을 여러
    // 씬으로 쪼개거나(방 단위 작업) 작업용 씬 이름을 따로 쓰면 그 매칭이 깨진다. 실제로
    // Zone_Residue_Full에서 CurrentZoneData가 null이 되어 되감기 해금이 활성 스킬로 잡히지
    // 않았다. 이름 규칙에 기대는 대신 씬이 스스로 밝히게 한다.
    public class ZoneMarker : MonoBehaviour
    {
        [SerializeField] ZoneId zone;

        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.EnterZone(zone);
        }
    }
}
