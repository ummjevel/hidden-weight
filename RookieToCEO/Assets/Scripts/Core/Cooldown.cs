namespace RookieToCEO.Core
{
    // 액티브 스킬/궁극기/무기 공격속도에 공통으로 쓰는 쿨타임 타이머.
    // MonoBehaviour의 Update에 얽매이지 않고 Tick(deltaTime)을 직접 호출하는 방식이라
    // EditMode 테스트에서 시간을 임의로 흘려보내며 검증할 수 있다.
    public class Cooldown
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;

        public Cooldown(float duration)
        {
            Duration = duration;
            Remaining = 0f; // 처음엔 바로 사용 가능
        }

        public void SetDuration(float duration)
        {
            Duration = duration;
        }

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f)
            {
                Remaining -= deltaTime;
                if (Remaining < 0f) Remaining = 0f;
            }
        }

        // 준비됐으면 즉시 쿨타임을 다시 걸고 true를 반환한다. 준비 안 됐으면 아무것도 안 하고 false.
        public bool TryUse()
        {
            if (!IsReady) return false;

            Remaining = Duration;
            return true;
        }

        public void ResetReady()
        {
            Remaining = 0f;
        }
    }
}
