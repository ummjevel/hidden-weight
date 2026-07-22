using UnityEngine;

namespace HanGame.Data
{
    /// <summary>
    /// 층 하나의 낮 설정. 기획서 5.4/5.5/10.3.
    /// 층별 배수를 적 기본 수치에 곱해 난이도를 올린다(체력보다 생성량 중심).
    /// </summary>
    [CreateAssetMenu(menuName = "HanGame/Floor Config", fileName = "FloorConfig")]
    public class FloorConfig : ScriptableObject
    {
        [Header("식별")]
        public int floor = 1;
        public string displayName = "신입사원 구역";

        [Header("웨이브")]
        public WaveTable waveTable;
        public float dayDuration = 60f;

        [Header("난이도 배수 (기획서 5.5)")]
        public float hpMultiplier = 1f;    // 1층 100% 기준
        public float speedMultiplier = 1f;
        public float spawnMultiplier = 1f; // 생성량 배수

        [Header("상사의 시선 (기획서 10.3)")]
        public bool bossGazeEnabled = true;
        public float bossGazeFirstAt = 30f; // 최초 발동 시각(초)
        public int bossGazeSweeps = 1;      // 시선 이동 횟수
        public float bossGazeWidth = 2f;    // 시선 폭
        public float bossGazeSpeed = 3f;    // 이동 속도

        [Header("CEO 최종 웨이브(4층)")]
        public bool isFinalFloor = false;
    }
}
