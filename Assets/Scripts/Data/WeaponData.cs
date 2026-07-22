using UnityEngine;

namespace HanGame.Data
{
    public enum WeaponKind
    {
        AutoBasic, // 자동 기본 무기(키보드 샷건, 스테이플러)
        Active,    // 액티브 스킬(업무 떠넘기기)
        Ultimate   // 궁극기(퇴사 통보)
    }

    /// <summary>
    /// 무기/스킬 수치. 기획서 8장.
    /// id는 RunState.WeaponIds 값과 일치시킨다.
    /// </summary>
    [CreateAssetMenu(menuName = "HanGame/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("식별")]
        public string id = "keyboard_shotgun";
        public string displayName = "키보드 샷건";
        public WeaponKind kind = WeaponKind.AutoBasic;

        [Header("자동 무기 공통")]
        public float damage = 8f;
        public float attackInterval = 0.9f; // 낮을수록 빠름
        public float range = 6f;            // 자동 조준 탐색 거리
        public float projectileSpeed = 10f;
        public GameObject projectilePrefab;

        [Header("키보드 샷건(부채꼴)")]
        public int pellets = 5;
        public float spreadAngle = 60f;

        [Header("스테이플러(단일 직선)")]
        public bool pierces = false; // 첫 적 적중 시 소멸(기획서 8.3)

        [Header("업무 떠넘기기(Active)")]
        public float pushRadius = 3f;
        public float pushForce = 8f;
        public float cooldown = 12f; // 권장 쿨타임

        [Header("퇴사 통보(Ultimate)")]
        public float fearDuration = 3f;   // 일반 적 공포/도주 시간
        public float ceoStunDuration = 3f; // CEO 웨이브 정지
        public float eliteSlow = 0.5f;    // 정예 이동속도 감소
        public float gaugePerKill = 0.05f; // 처리당 게이지 충전량(0~1)
    }
}
