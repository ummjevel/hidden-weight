namespace HiddenWeight.Enemies
{
    // "이 방향에서 들어온 공격은 막는다"는 계약. Enemy.TakeDamage가 피해를 적용하기 전에
    // 이 인터페이스를 물어본다.
    //
    // 원래 Enemy가 GuardBehavior 구현 타입을 직접 알고 있었는데, 응시 지역의 정예
    // "얼굴 없는 재판관"(JudgeBehavior)도 같은 판정이 필요해지면서 인터페이스로 뺐다.
    // 방어 판정을 가진 적이 늘어나도 Enemy는 손대지 않는다.
    public interface IGuard
    {
        bool BlocksFrom(UnityEngine.Vector2 sourcePosition);
    }
}
