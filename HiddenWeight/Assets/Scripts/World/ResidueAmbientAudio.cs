using System.Collections;
using UnityEngine;
using HiddenWeight.UI;

namespace HiddenWeight.World
{
    // 잔재의 방별 환경음. 음악 소스와 분리되어 개발 중 BGM 음소거 정책을 깨지 않는다.
    //
    // 방마다 다른 소리를 트는 이유: 잔재는 다리 · 매몰된 하층 · 손가락 내부 · 승강축처럼
    // 공간의 성격이 크게 갈리는데, 배경 그림만으로는 "지금 어디 있는지"가 잘 안 읽힌다.
    // 공간이 바뀌는 순간 소리가 같이 바뀌면 지도를 안 봐도 위치가 잡힌다.
    //
    // 소스가 둘인 이유: 방을 넘을 때 끊고 새로 틀면 딸깍 소리가 나고 공간이 잘려 들린다.
    // 두 소스를 겹쳐 교차 페이드하면 이동이 이어진다.
    public sealed class ResidueAmbientAudio : MonoBehaviour
    {
        const float CrossfadeSeconds = 1.4f;
        const float VolumeScale = 0.08f;

        AudioSource _a;
        AudioSource _b;
        AudioSource _active;
        AudioClip _fallback;
        Coroutine _fade;
        string _currentKey;

        // 방 번호 → 환경음. LEVEL_21_RESIDUE_ROOMS.md의 R01~R12 공간 성격을 그대로 따른다.
        // 표에 없는 번호는 전용 음원이 아직 없다는 뜻이라 절차 생성 베드로 떨어진다.
        static string KeyForRoom(int room)
        {
            switch (room)
            {
                case 1:                     // R01 입구 경계
                case 2:                     // R02 애도교
                case 3:                     // R03 손바닥 광장
                case 7: return "EntryBridge";   // R07 갈비 곡선교
                case 4:                     // R04 매몰된 하층 폐허
                case 11: return "LowerRuins";   // R11 후회의 회랑
                case 5: return "SecretRoom";    // R05 되감기 성소
                case 6: return "InsideFingers"; // R06 손가락 내부
                case 8: return "LiftShaft";     // R08 상층 승강축
                case 9:                     // R09 끊어진 상층 고가교
                case 10: return "UpperTower";   // R10 손목 감시탑
                case 12: return "Gallows";      // R12 기억의 교수대
                default: return null;
            }
        }

        public static AudioClip LoadAmbience(int room)
        {
            string key = KeyForRoom(room);
            if (string.IsNullOrEmpty(key)) return null;
            return Resources.Load<AudioClip>("Audio/Ambience/Ambience_" + key + "_Loop");
        }

        // "Room07" 처럼 이름에 섞인 숫자만 뽑는다. 씬마다 접두사가 달라도 견디게 한다.
        public static int RoomNumber(string roomName)
        {
            int number = 0;
            if (string.IsNullOrEmpty(roomName)) return 0;
            foreach (char c in roomName) if (char.IsDigit(c)) number = number * 10 + (c - '0');
            return number;
        }

        void Start()
        {
            _fallback = BuildClip();
            _a = CreateSource();
            _b = CreateSource();
            _active = _a;

            _active.clip = _fallback;
            _active.volume = TargetVolume;
            _active.Play();

            UISettings.Changed += ApplyVolume;
            if (RoomCamera.Instance != null)
            {
                RoomCamera.Instance.RoomChanged += HandleRoomChanged;
                HandleRoomChanged(RoomCamera.Instance.CurrentRoom);
            }
        }

        void OnDestroy()
        {
            UISettings.Changed -= ApplyVolume;
            if (RoomCamera.Instance != null) RoomCamera.Instance.RoomChanged -= HandleRoomChanged;
        }

        AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            return source;
        }

        static float TargetVolume => UISettings.SfxVolume * VolumeScale;

        void ApplyVolume()
        {
            // 교차 페이드 중에는 코루틴이 음량을 쥐고 있으므로 건드리지 않는다.
            if (_fade != null || _active == null) return;
            _active.volume = TargetVolume;
        }

        void HandleRoomChanged(Room room)
        {
            if (room == null || _active == null) return;

            int number = RoomNumber(room.name);
            string key = KeyForRoom(number);
            if (key == _currentKey) return;
            _currentKey = key;

            var clip = LoadAmbience(number);
            var next = clip != null ? clip : _fallback;

            // 전용 음원이 없는 방끼리 오갈 때는 같은 베드를 다시 틀지 않고 음정만 흔든다.
            if (clip == null)
            {
                _active.pitch = 0.88f + (number % 5) * 0.035f;
                if (_active.clip != _fallback) CrossfadeTo(next);
                return;
            }

            _active.pitch = 1f;
            CrossfadeTo(next);
        }

        void CrossfadeTo(AudioClip clip)
        {
            if (_active.clip == clip && _active.isPlaying) return;

            var from = _active;
            var to = from == _a ? _b : _a;
            to.clip = clip;
            to.pitch = 1f;
            to.volume = 0f;
            to.Play();
            _active = to;

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeRoutine(from, to));
        }

        IEnumerator FadeRoutine(AudioSource from, AudioSource to)
        {
            float start = from.volume;
            float target = TargetVolume;
            for (float t = 0f; t < CrossfadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / CrossfadeSeconds;
                from.volume = Mathf.Lerp(start, 0f, k);
                to.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }
            from.volume = 0f;
            from.Stop();
            to.volume = TargetVolume;
            _fade = null;
        }

        // 전용 음원이 없는 방에서 쓰는 저음 베드. 아무 소리도 없는 것보다는 공간감이 남는다.
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
