using System.Collections.Generic;
using UnityEngine;

namespace HanGame.Data
{
    /// <summary>
    /// 한 층의 60초 적 생성표. 기획서 5.3/24(WAVE_TABLE.md 대체).
    /// EnemySpawner가 시간 구간별 항목을 읽어 스폰한다.
    /// </summary>
    [CreateAssetMenu(menuName = "HanGame/Wave Table", fileName = "WaveTable")]
    public class WaveTable : ScriptableObject
    {
        [System.Serializable]
        public struct SpawnEntry
        {
            public EnemyData enemy;
            public float startTime;    // 등장 시작(초)
            public float endTime;      // 등장 종료(초)
            public float interval;     // 스폰 간격(초)
            public int countPerSpawn;  // 한 번에 스폰 수
        }

        [Tooltip("웨이브 총 길이(초). 낮 전투는 60초.")]
        public float duration = 60f;

        [Tooltip("동시 최대 개체 수. 초과 시 스폰 보류.")]
        public int maxAlive = 60;

        public List<SpawnEntry> entries = new();
    }
}
