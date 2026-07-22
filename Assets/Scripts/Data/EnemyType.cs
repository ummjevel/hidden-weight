namespace HanGame.Data
{
    /// <summary>적 종류. 기획서 9.2.</summary>
    public enum EnemyType
    {
        EmailEnvelope,   // 이메일 봉투 — 기본 군집형
        PaperStack,      // 서류 더미 — 탱커형
        UrgentPostit,    // 긴급 수정 포스트잇 — 돌진형
        MeetingCalendar, // 회의 요청 달력 — 디버프형
        ClaimPhone,      // 클레임 전화기 — 원거리형
        CeoDirective     // CEO 최종 지시서 — 최종 웨이브
    }

    /// <summary>적 이동/공격 행동 유형. Enemy AI가 분기에 사용.</summary>
    public enum EnemyBehavior
    {
        Chaser,   // 플레이어에게 직선 이동(이메일 봉투)
        Tank,     // 느리지만 높은 HP(서류 더미)
        Dasher,   // 예고 후 돌진(포스트잇)
        Debuffer, // 근접 시 플레이어 공격속도 감소(달력)
        Ranged,   // 거리 유지 원거리 공격(전화기)
        Boss      // 소환·범위 공격(CEO 지시서)
    }
}
