using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // 외부 음원이 아직 없는 지역도 침묵으로 출시되지 않게 하는 절제된 임시 앰비언스.
    // 정식 음악이 ZoneData에 연결되면 자동으로 사용되지 않는다.
    public static class AmbientAudioFactory
    {
        const int SampleRate = 22050;
        const float Duration = 16f;
        static readonly Dictionary<ZoneId, AudioClip> Cache = new Dictionary<ZoneId, AudioClip>();

        public static AudioClip For(ZoneId zone)
        {
            if (Cache.TryGetValue(zone, out var cached) && cached != null) return cached;
            float root = zone switch
            {
                ZoneId.Gaze => 130.81f,
                ZoneId.Fracture => 174.61f,
                ZoneId.Residue => 110f,
                _ => 146.83f,
            };
            float tension = zone == ZoneId.Gaze ? 1.498f : zone == ZoneId.Fracture ? 1.337f : 1.25f;
            int count = Mathf.RoundToInt(SampleRate * Duration);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float breath = 0.58f + 0.42f * Mathf.Sin(t * Mathf.PI * 0.125f);
                float baseTone = Mathf.Sin(2f * Mathf.PI * root * t) * 0.022f;
                float overtone = Mathf.Sin(2f * Mathf.PI * root * tension * t + 0.7f) * 0.012f;
                float distant = Mathf.Sin(2f * Mathf.PI * root * 0.5f * t + 1.4f) * 0.018f;
                samples[i] = (baseTone + overtone + distant) * breath;
            }
            var clip = AudioClip.Create("GeneratedAmbient_" + zone, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            Cache[zone] = clip;
            return clip;
        }
    }
}
