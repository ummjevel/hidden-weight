using System.Collections.Generic;
using UnityEngine;

namespace HanGame.Common
{
    /// <summary>
    /// 간단한 SFX/BGM 재생기. 기획서 15.4 최소 사운드 목록.
    /// 클립을 id로 등록해 두고 이름으로 재생한다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public struct Clip
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private Clip[] clips;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        private readonly Dictionary<string, Clip> _map = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var c in clips)
                if (!string.IsNullOrEmpty(c.id)) _map[c.id] = c;
        }

        public void PlaySfx(string id)
        {
            if (sfxSource == null || !_map.TryGetValue(id, out var c) || c.clip == null) return;
            sfxSource.PlayOneShot(c.clip, c.volume <= 0f ? 1f : c.volume);
        }

        public void PlayBgm(string id, bool loop = true)
        {
            if (bgmSource == null || !_map.TryGetValue(id, out var c) || c.clip == null) return;
            bgmSource.clip = c.clip;
            bgmSource.loop = loop;
            bgmSource.volume = c.volume <= 0f ? 1f : c.volume;
            bgmSource.Play();
        }
    }

    /// <summary>기획서 15.4 사운드 id 상수.</summary>
    public static class Sfx
    {
        public const string KeyboardHit = "sfx_keyboard";
        public const string StaplerFire = "sfx_stapler";
        public const string ApprovalStamp = "sfx_stamp";
        public const string EmailAlert = "sfx_email";
        public const string PhoneRing = "sfx_phone";
        public const string CoffeeHeal = "sfx_coffee";
        public const string BossGazeWarn = "sfx_boss_gaze";
        public const string GuardSpotted = "sfx_guard";
        public const string ReputationDown = "sfx_rep_down";
        public const string Resignation = "sfx_resignation";
        public const string CeoWaveWarn = "sfx_ceo_wave";
    }
}
