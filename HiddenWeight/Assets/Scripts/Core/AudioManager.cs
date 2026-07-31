using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.UI;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // BGM/SFX 재생을 담당하는 싱글턴. Resources의 실제 SFX를 우선하고,
    // 아직 제작되지 않은 시그니처 음향만 짧은 절차 생성음으로 안전하게 대체한다.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // 지금 재생 중이거나 재생하려는 BGM. "같은 곡이면 다시 틀지 않는다" 판단에 쓴다.
        public AudioClip CurrentBgm { get; private set; }

        AudioSource _bgmSource;
        AudioSource _sfxSource;
        AudioSource[] _sfxSources;
        int _sfxSourceCursor;
        AudioSource _loopSource;
        Coroutine _bgmFade;
        readonly Dictionary<SfxCue, AudioClip[]> _sfxClips = new Dictionary<SfxCue, AudioClip[]>();
        readonly Dictionary<SfxCue, int> _lastSfxIndex = new Dictionary<SfxCue, int>();
        SfxCue? _loopCue;
        float _loopVolume = 1f;

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

            // 짧은 효과음이 겹쳐도 개별 피치가 서로 바뀌지 않도록 작은 소스 풀을 쓴다.
            _sfxSources = new AudioSource[4];
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                _sfxSources[i] = gameObject.AddComponent<AudioSource>();
                _sfxSources[i].loop = false;
                _sfxSources[i].playOnAwake = false;
            }
            _sfxSource = _sfxSources[0];

            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.loop = false;
            _loopSource.playOnAwake = false;

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
            if (_sfxSources != null)
                foreach (var source in _sfxSources)
                    if (source != null) source.volume = UISettings.SfxVolume;
            else if (_sfxSource != null)
                _sfxSource.volume = UISettings.SfxVolume;
            if (_loopSource != null) _loopSource.volume = UISettings.SfxVolume * _loopVolume;
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
            PlaySfx(ResolveSfx(cue), volume);
        }

        // Assets/Resources/Audio/SFX/<Cue 이름> 폴더의 WAV를 우선 사용한다.
        // 전용 음원이 아직 없으면 기존 절차 생성음을 남겨, 이벤트 연결 자체는 끊기지 않게 한다.
        public AudioClip ResolveSfx(SfxCue cue)
        {
            var clips = ClipsFor(cue);

            // 벽점프 전용음이 완성되기 전까지 일반 점프음을 재사용한다.
            if (clips.Length == 0 && cue == SfxCue.WallJump)
                clips = ClipsFor(SfxCue.Jump);

            if (clips.Length == 0) return ProceduralSfx.For(cue);
            if (clips.Length == 1)
            {
                _lastSfxIndex[cue] = 0;
                return clips[0];
            }

            int previous = _lastSfxIndex.TryGetValue(cue, out var value) ? value : -1;
            int index = Random.Range(0, clips.Length - 1);
            if (index >= previous) index++;
            _lastSfxIndex[cue] = index;
            return clips[index];
        }

        AudioClip[] ClipsFor(SfxCue cue)
        {
            if (_sfxClips.TryGetValue(cue, out var clips)) return clips;
            clips = Resources.LoadAll<AudioClip>("Audio/SFX/" + cue);
            _sfxClips[cue] = clips;
            return clips;
        }

        public void StartSfxLoop(SfxCue cue, float volume = 1f)
        {
            if (_loopCue == cue && _loopSource.isPlaying) return;

            var clip = ResolveSfx(cue);
            if (clip == null) return;

            _loopCue = cue;
            _loopVolume = Mathf.Clamp01(volume);
            _loopSource.Stop();
            _loopSource.clip = clip;
            _loopSource.loop = true;
            _loopSource.volume = UISettings.SfxVolume * _loopVolume;
            _loopSource.Play();
        }

        public void StopSfxLoop(SfxCue cue)
        {
            if (_loopCue != cue) return;
            _loopSource.Stop();
            _loopSource.clip = null;
            _loopSource.loop = false;
            _loopCue = null;
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

            var source = NextSfxSource();
            source.pitch = Random.Range(0.97f, 1.03f);
            source.PlayOneShot(clip, Mathf.Clamp01(volume * Random.Range(0.94f, 1.06f)));
        }

        AudioSource NextSfxSource()
        {
            for (int offset = 0; offset < _sfxSources.Length; offset++)
            {
                int index = (_sfxSourceCursor + offset) % _sfxSources.Length;
                if (_sfxSources[index].isPlaying) continue;

                _sfxSourceCursor = (index + 1) % _sfxSources.Length;
                return _sfxSources[index];
            }

            var source = _sfxSources[_sfxSourceCursor];
            _sfxSourceCursor = (_sfxSourceCursor + 1) % _sfxSources.Length;
            return source;
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

    public enum SfxCue
    {
        UiConfirm, Checkpoint, Fragment, Ability, Attack, Jump, Dash, Hurt,
        RewindStart, RewindComplete, ShortcutOpen, EnemyHit, EnemyDeath,
        BossTelegraph, BossPhase, BossVictory
    }

    static class ProceduralSfx
    {
        static readonly System.Collections.Generic.Dictionary<SfxCue, AudioClip> Cache =
            new System.Collections.Generic.Dictionary<SfxCue, AudioClip>();

        public static AudioClip For(SfxCue cue)
        {
            if (Cache.TryGetValue(cue, out var clip) && clip != null) return clip;
            const int rate = 22050;
            float duration = cue == SfxCue.RewindStart ? 0.35f
                : cue == SfxCue.RewindComplete ? 0.55f
                : cue == SfxCue.ShortcutOpen ? 0.45f
                : cue == SfxCue.BossPhase || cue == SfxCue.BossVictory ? 0.7f
                : cue == SfxCue.BossTelegraph ? 0.25f : 0.09f;
            int count = Mathf.RoundToInt(rate * duration);
            var data = new float[count];
            float frequency = 220f + (int)cue * 37f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = 1f - (float)i / count;
                float direction = cue == SfxCue.RewindStart ? -1f
                    : cue == SfxCue.RewindComplete ? 1f : 0f;
                float swept = frequency * (1f + direction * 0.55f * ((float)i / count));
                float tone = Mathf.Sin(2f * Mathf.PI * swept * t);
                float metal = Mathf.Sin(2f * Mathf.PI * swept * 2.03f * t) * 0.28f;
                data[i] = (tone + metal) * envelope * 0.10f;
            }
            clip = AudioClip.Create("Sfx_" + cue, count, 1, rate, false);
            clip.SetData(data, 0);
            Cache[cue] = clip;
            return clip;
        }
    }
}
