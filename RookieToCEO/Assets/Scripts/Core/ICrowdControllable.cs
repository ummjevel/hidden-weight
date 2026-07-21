namespace RookieToCEO.Core
{
    // 퇴사 통보(궁극기)와 업무 떠넘기기(액티브)가 적에게 거는 군중 제어 효과.
    // M5에서 EnemyBase가 이 인터페이스를 구현해 실제 이동/속도에 반영한다.
    public interface ICrowdControllable
    {
        // 일반 업무: 화면 반대 방향으로 도망(공포)
        void ApplyFear(float duration);

        // 정예 업무: 이동속도 감소
        void ApplySlow(float duration, float slowMultiplier);

        // 밀려남(업무 떠넘기기 액티브) - 방향과 세기를 받아 즉시 밀어낸다.
        void ApplyKnockback(UnityEngine.Vector2 direction, float force);
    }

    // CEO 최종 지시서(보스)는 공포/슬로우 대신 "3초 정지"만 적용된다 (GDD 3번/13번).
    public interface IBossPausable
    {
        void ApplyPause(float duration);
    }
}
