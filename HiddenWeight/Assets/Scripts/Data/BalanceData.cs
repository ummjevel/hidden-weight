using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenWeight.Data
{
    // 전체 밸런스 데이터의 진입점. 플레이어/감정/적/지역 데이터를 한데 모아
    // 이름 또는 씬 이름으로 조회하는 기능을 제공한다.
    [CreateAssetMenu(fileName = "BalanceData", menuName = "HiddenWeight/Balance Data")]
    public class BalanceData : ScriptableObject
    {
        public PlayerData player;
        public EmotionData[] emotions;
        public EnemyData[] enemies;
        public ZoneData[] zones;
        public VolumeProfile awarenessProfile;

        // 배열 원소가 최대 4개이므로 선형 탐색으로 충분하다.
        public EmotionData GetEmotion(EmotionId id)
        {
            if (emotions == null)
            {
                return null;
            }

            foreach (var emotion in emotions)
            {
                if (emotion != null && emotion.id == id)
                {
                    return emotion;
                }
            }

            return null;
        }

        public ZoneData GetZone(ZoneId id)
        {
            if (zones == null)
            {
                return null;
            }

            foreach (var zone in zones)
            {
                if (zone != null && zone.id == id)
                {
                    return zone;
                }
            }

            return null;
        }

        public ZoneData GetZoneByScene(string sceneName)
        {
            if (zones == null)
            {
                return null;
            }

            foreach (var zone in zones)
            {
                if (zone != null && zone.sceneName == sceneName)
                {
                    return zone;
                }
            }

            return null;
        }
    }
}
