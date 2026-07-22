using System.Collections.Generic;
using UnityEngine;

namespace HanGame.Data
{
    /// <summary>
    /// 밤 잠입 한 층 설정. 기획서 11장.
    /// 경비 경로·조사 보상·제한 시간을 데이터로 관리(19.4).
    /// </summary>
    [CreateAssetMenu(menuName = "HanGame/Night Config", fileName = "NightConfig")]
    public class NightConfig : ScriptableObject
    {
        [Header("식별")]
        public int floor = 1;

        [Header("제한 시간 (기획서 11.4)")]
        public float timeLimit = 60f;
        public float investigateSeconds = 1.5f;

        [Header("조사 보상 (기획서 8.6)")]
        public string rewardWeaponId = "stapler_rapid";
        public string objectiveName = "비품실의 자동 스테이플러 설계서";

        [Header("소음 (기획서 11.8)")]
        public bool noiseEnabled = true;
        public float noiseRadius = 3f;
    }

    /// <summary>경비/야근자 순찰 경로. 씬 오브젝트로 배치하거나 데이터로 관리.</summary>
    [CreateAssetMenu(menuName = "HanGame/Guard Route", fileName = "GuardRoute")]
    public class GuardRouteData : ScriptableObject
    {
        [Header("경로 (월드 좌표)")]
        public List<Vector2> waypoints = new();
        public bool loop = true;

        [Header("이동/시야 (기획서 11.5)")]
        public float moveSpeed = 2f;
        public float waitAtWaypoint = 0.5f;
        public float startDelay = 3f;   // 시작 후 3초 정지
        public float viewDistance = 4f; // 부채꼴 시야 거리
        public float viewAngle = 60f;   // 부채꼴 각도
    }
}
