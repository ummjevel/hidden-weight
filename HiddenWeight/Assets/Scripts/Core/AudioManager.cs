using System.Collections;
using UnityEngine;
using HiddenWeight.UI;
using HiddenWeight.Data;

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
#if UNITY_EDITOR
            // 개발 중 씬 재생과 자동 테스트에서 음악이 반복 출력되지 않게 한다.
            // 클립과 재생 상태는 유지하므로 BGM 와이어링 테스트는 그대로 유효하다.
            _bgmSource.mute = true;
#endif

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

        public void PlayZoneBgm(ZoneData zone, float fadeSeconds = 1f)
        {
            if (zone == null) return;
            PlayBgm(zone.bgm != null ? zone.bgm : AmbientAudioFactory.For(zone.id), fadeSeconds);
        }

        public void PlaySfx(SfxCue cue, float volume = 1f)
        {
            PlaySfx(ProceduralSfx.For(cue), volume);
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

    public enum SfxCue { UiConfirm, Checkpoint, Fragment, Ability, Attack, Jump, Dash, Hurt }

    static class ProceduralSfx
    {
        static readonly System.Collections.Generic.Dictionary<SfxCue, AudioClip> Cache =
            new System.Collections.Generic.Dictionary<SfxCue, AudioClip>();

        public static AudioClip For(SfxCue cue)
        {
            if (Cache.TryGetValue(cue, out var clip) && clip != null) return clip;
            const int rate = 22050;
            int count = Mathf.RoundToInt(rate * 0.09f);
            var data = new float[count];
            float frequency = 220f + (int)cue * 37f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = 1f - (float)i / count;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.12f;
            }
            clip = AudioClip.Create("Sfx_" + cue, count, 1, rate, false);
            clip.SetData(data, 0);
            Cache[cue] = clip;
            return clip;
        }
    }
}
