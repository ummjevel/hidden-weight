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

        // 비어 있으면 항상 사용 가능. 값이 있으면 ProgressState.IsShortcutOpen(id)가
        // true일 때만 문이 반응한다 — 잔재/응시의 물리적 숏컷 A/B/C처럼, 보스를 잡거나
        // 되감기 대상을 복원해야 열리는 지름길에 쓴다. 양쪽 문 모두 같은 id를 본다.
        public string requiredShortcutId;

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
