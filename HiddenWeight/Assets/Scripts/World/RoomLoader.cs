using UnityEngine;

namespace HiddenWeight.World
{
    // Task 4의 실제 전환 구현이 채우기 전까지, RoomDoor가 컴파일되도록 두는 골격이다.
    public class RoomLoader : MonoBehaviour
    {
        public static RoomLoader Instance { get; private set; }
        void Awake() => Instance = this;
        public void RequestTransition(RoomDoor from) { }
    }
}
