using System;
using UnityEngine;

namespace HanGame.Day
{
    /// <summary>
    /// 낮 전투 60초 카운트다운. 기획서 5.1/19.2.
    /// timeScale=0(레벨업)이면 자동으로 멈춘다(unscaled 미사용).
    /// </summary>
    public class WaveTimer : MonoBehaviour
    {
        public float Duration { get; private set; } = 60f;
        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, Duration - Elapsed);
        public bool Running { get; private set; }

        public event Action Completed;
        public event Action<float> Ticked; // 남은 시간

        public void Begin(float duration)
        {
            Duration = duration;
            Elapsed = 0f;
            Running = true;
        }

        public void Stop() => Running = false;

        private void Update()
        {
            if (!Running) return;
            Elapsed += Time.deltaTime; // 레벨업 시 timeScale=0이면 정지
            Ticked?.Invoke(Remaining);

            if (Elapsed >= Duration)
            {
                Running = false;
                Completed?.Invoke();
            }
        }
    }
}
