namespace RookieToCEO.Core
{
    // GDD 6번(적 구성) 5종 + 4층 보스(GDD 13번).
    public enum EnemyType
    {
        EmailEnvelope,   // 이메일 봉투: 기본 적, 직선 이동
        DocumentStack,   // 서류 더미: 탱커, 느리지만 높은 HP
        PostItRush,      // 긴급 수정 포스트잇: 돌진형
        MeetingCalendar, // 회의 요청 달력: 디버프형(공격속도 감소)
        ClaimPhone,      // 클레임 전화기: 원거리형
        CeoFinalOrder,   // CEO 최종 지시서: 보스형 (4층)
    }
}
