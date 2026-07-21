namespace RookieToCEO.Core
{
    // 무기/스킬이 데미지를 주는 대상이 구현할 인터페이스. M5에서 EnemyBase가 구현한다.
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}
