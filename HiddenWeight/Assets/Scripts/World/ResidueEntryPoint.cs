using UnityEngine;

namespace HiddenWeight.World
{
    // 셸 씬에는 지형이 없다. 지역에 들어오면 첫 방을 로드해야 게임이 시작된다.
    public class ResidueEntryPoint : MonoBehaviour
    {
        [SerializeField] string firstRoom = "R01";

        void Start()
        {
            var loader = RoomLoader.Instance;
            if (loader == null)
            {
                Debug.LogError("[ResidueEntryPoint] RoomLoader가 없다. 첫 방을 로드할 수 없다.");
                return;
            }

            if (loader.CurrentRoom == null) loader.LoadRoom(firstRoom, null);
        }
    }
}
