using System.Collections;
using UnityEngine;
using HiddenWeight.UI;

namespace HiddenWeight.Core
{
    // BGM/SFX 재생을 담당하는 싱글턴. MVP에서는 사운드 에셋이 없으므로 클립이 null이면 아무 것도 하지 않는다.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // 지금 재생 중이거나 재생하려는 BGM. "같은 곡이면 다시 틀지 않는다" 판단에 쓴다.
        public AudioClip CurrentBgm { get; private set; }

        AudioSource _bgmSource;
        AudioSource _sfxSource;
        Coroutine _bgmFade;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            ApplySettings();
            UISettings.Changed += ApplySettings;
        }

        void OnDestroy()
        {
            if (Instance == this) UISettings.Changed -= ApplySettings;
        }

        void ApplySettings()
        {
            AudioListener.volume = UISettings.MasterVolume;
            if (_bgmSource != null) _bgmSource.volume = UISettings.BgmVolume;
            if (_sfxSource != null) _sfxSource.volume = UISettings.SfxVolume;
        }

        public void PlayBgm(AudioClip clip, float fadeSeconds = 1f)
        {
            if (clip == null || clip == CurrentBgm) return;

            CurrentBgm = clip;
            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmFade = StartCoroutine(FadeToClip(clip, fadeSeconds));
        }

        public void StopBgm(float fadeSeconds = 1f)
        {
            CurrentBgm = null;
            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmFade = StartCoroutine(FadeOutAndStop(fadeSeconds));
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, volume);
        }

        IEnumerator FadeToClip(AudioClip clip, float fadeSeconds)
        {
            yield return FadeVolume(_bgmSource, _bgmSource.volume, 0f, fadeSeconds * 0.5f);

            _bgmSource.clip = clip;
            _bgmSource.Play();

            yield return FadeVolume(_bgmSource, _bgmSource.volume, UISettings.BgmVolume, fadeSeconds * 0.5f);

            _bgmFade = null;
        }

        IEnumerator FadeOutAndStop(float fadeSeconds)
        {
            yield return FadeVolume(_bgmSource, _bgmSource.volume, 0f, fadeSeconds);

            _bgmSource.Stop();
            _bgmSource.clip = null;
            _bgmFade = null;
        }

        static IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                source.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            source.volume = to;
        }
    }
}
