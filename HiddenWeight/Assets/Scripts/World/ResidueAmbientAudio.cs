using UnityEngine;
using HiddenWeight.UI;

namespace HiddenWeight.World
{
    // 최종 룸별 환경음 에셋이 들어오기 전까지 사용하는 잔재 전용 저음 환경 베드.
    // 음악 소스와 분리되어 개발 중 BGM 음소거 정책을 깨지 않는다.
    public sealed class ResidueAmbientAudio : MonoBehaviour
    {
        AudioSource _source;

        void Start()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.clip = BuildClip();
            ApplyVolume();
            _source.Play();
            UISettings.Changed += ApplyVolume;
            if (RoomCamera.Instance != null) RoomCamera.Instance.RoomChanged += HandleRoomChanged;
        }

        void OnDestroy()
        {
            UISettings.Changed -= ApplyVolume;
            if (RoomCamera.Instance != null) RoomCamera.Instance.RoomChanged -= HandleRoomChanged;
        }

        void ApplyVolume()
        {
            if (_source != null) _source.volume = UISettings.SfxVolume * 0.08f;
        }

        void HandleRoomChanged(Room room)
        {
            if (_source == null || room == null) return;
            int number = 0;
            foreach (char c in room.name) if (char.IsDigit(c)) number = number * 10 + (c - '0');
            _source.pitch = 0.88f + (number % 5) * 0.035f;
        }

        static AudioClip BuildClip()
        {
            const int rate = 11025;
            const int seconds = 6;
            var samples = new float[rate * seconds];
            uint state = 0x51A7u;
            float filtered = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) / 16777215f) * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.012f);
                float chain = Mathf.Sin(2f * Mathf.PI * 47f * i / rate) * 0.035f;
                samples[i] = filtered * 0.12f + chain;
            }
            var clip = AudioClip.Create("Residue_Ambient_Bed", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
