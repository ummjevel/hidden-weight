using UnityEngine;

namespace HiddenWeight.World
{
    // 셸 씬에는 지형이 없다. 지역에 들어오면 첫 방을 로드해야 게임이 시작된다.
    //
    // 지역마다 씬 이름 접두사와 첫 방이 다르므로 둘 다 인스펙터 값으로 둔다. 잔재 전용이던
    // ResidueEntryPoint를 지역 공용으로 일반화한 것이다 — 응시·균열이 같은 구조를 쓰면서
    // 같은 코드를 세 벌 두는 것은 관리 지점만 늘린다.
    public class ZoneEntryPoint : MonoBehaviour
    {
        [SerializeField] string scenePrefix = "Room_Residue_";
        [SerializeField] string firstRoom = "R01";

        void Start()
        {
            var loader = RoomLoader.Instance;
            if (loader == null)
            {
                Debug.LogError("[ZoneEntryPoint] RoomLoader가 없다. 첫 방을 로드할 수 없다.");
                return;
            }

            loader.ConfigureZone(scenePrefix);
            if (loader.CurrentRoom == null) loader.LoadRoom(firstRoom, null);
        }
    }
}
