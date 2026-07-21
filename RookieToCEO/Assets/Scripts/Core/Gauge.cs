using UnityEngine;

namespace RookieToCEO.Core
{
    // 퇴사 통보(궁극기) 게이지처럼 "적을 처리하면 충전되고, 다 차면 소모하는" 값을 표현하는 순수 클래스.
    public class Gauge
    {
        public float Max { get; }
        public float Value { get; private set; }
        public bool IsFull => Value >= Max;

        public Gauge(float max)
        {
            Max = max;
            Value = 0f;
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            Value = Mathf.Min(Max, Value + amount);
        }

        // 가득 찼으면 0으로 비우고 true를 반환한다. 안 찼으면 false.
        public bool TryConsume()
        {
            if (!IsFull) return false;

            Value = 0f;
            return true;
        }
    }
}
