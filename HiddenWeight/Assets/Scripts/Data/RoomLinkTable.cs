using UnityEngine;

namespace HiddenWeight.Data
{
    // 지역 하나의 방 연결 목록. 링크 하나가 문 두 개를 만들기 때문에
    // 되돌아올 수 없는 연결을 만들려면 이 데이터를 일부러 망가뜨려야 한다.
    [CreateAssetMenu(fileName = "RoomLinks", menuName = "HiddenWeight/Room Link Table")]
    public class RoomLinkTable : ScriptableObject
    {
        public ZoneId zone;
        public RoomLink[] links;

        public static string FromDoorId(string doorId)
        {
            if (string.IsNullOrEmpty(doorId)) return null;
            int colon = doorId.LastIndexOf(':');
            return colon <= 0 ? null : doorId.Substring(0, colon);
        }

        public bool TryFind(string linkId, out RoomLink link)
        {
            foreach (var candidate in links)
            {
                if (candidate.linkId != linkId) continue;
                link = candidate;
                return true;
            }

            link = default;
            return false;
        }
    }
}
