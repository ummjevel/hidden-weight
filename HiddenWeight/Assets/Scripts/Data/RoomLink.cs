using System;
using UnityEngine;

namespace HiddenWeight.Data
{
    // 방 출입구 방향. LEVEL_01_STANDARD.md 1.3의 표기를 그대로 옮긴 것이라
    // 맵 문서에 E라고 적힌 출구는 코드에서도 Side.E다. 이름을 바꾸면 문서와 어긋난다.
    public enum Side { W, E, NW, NE, SW, SE, U, D, S }

    // 방 두 개를 잇는 연결 하나. 빌더가 이걸 읽어 양쪽 방에 문을 하나씩 굽는다.
    [Serializable]
    public struct RoomLink
    {
        public string linkId;
        public string fromRoom;
        public string toRoom;
        public Side fromSide;
        public Side toSide;

        // 문의 중심 좌표(LEVEL_01_STANDARD.md 1.1). 각 방의 로컬 좌표다.
        public Vector2 fromAnchor;
        public Vector2 toAnchor;

        public static string DoorId(string linkId, Side side) => linkId + ":" + side;

        public string FromDoorId => DoorId(linkId, fromSide);
        public string ToDoorId => DoorId(linkId, toSide);

        public static Side Opposite(Side side) => side switch
        {
            Side.W => Side.E,
            Side.E => Side.W,
            Side.U => Side.D,
            Side.D => Side.U,
            Side.NW => Side.SE,
            Side.SE => Side.NW,
            Side.NE => Side.SW,
            Side.SW => Side.NE,
            _ => Side.S, // 비밀 연결은 마주 보는 방향이 없다
        };
    }
}
