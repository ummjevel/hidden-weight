using System.Collections;
using UnityEngine;

namespace HiddenWeight.Core
{
    // BGM/SFX 재생을 담당하는 싱글턴. MVP에서는 사운드 에셋이 없으므로 클립이 null이면 아무 것도 하지 않는다.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

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
        }

        public void PlayBgm(AudioClip clip, float fadeSeconds = 1f)
        {
            if (clip == null) return;

            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmFade = StartCoroutine(FadeToClip(clip, fadeSeconds));
        }

        public void StopBgm(float fadeSeconds = 1f)
        {
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

            yield return FadeVolume(_bgmSource, _bgmSource.volume, 1f, fadeSeconds * 0.5f);

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
