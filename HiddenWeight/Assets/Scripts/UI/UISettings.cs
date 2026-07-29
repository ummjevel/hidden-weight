using System;
using UnityEngine;

namespace HiddenWeight.UI
{
    // 메뉴에서 즉시 적용되고 다음 실행에도 유지되는 가벼운 사용자 설정 저장소.
    // 게임 진행 저장과 수명 주기가 다르므로 PlayerPrefs 키도 hw.ui.*로 격리한다.
    public static class UISettings
    {
        const string Prefix = "hw.ui.";

        public static event Action Changed;

        public static float MasterVolume { get => GetFloat("master", 1f); set => SetFloat("master", value, 0f, 1f); }
        public static float BgmVolume { get => GetFloat("bgm", 0.8f); set => SetFloat("bgm", value, 0f, 1f); }
        public static float SfxVolume { get => GetFloat("sfx", 1f); set => SetFloat("sfx", value, 0f, 1f); }
        public static float UiScale { get => GetFloat("scale", 1f); set => SetFloat("scale", value, 0.8f, 1.3f); }
        public static float MessageDuration { get => GetFloat("message", 1f); set => SetFloat("message", value, 0.8f, 2f); }
        public static bool ReduceMotion { get => GetBool("reduceMotion", false); set => SetBool("reduceMotion", value); }
        public static bool ReduceFlash { get => GetBool("reduceFlash", false); set => SetBool("reduceFlash", value); }
        public static bool HighContrast { get => GetBool("contrast", false); set => SetBool("contrast", value); }

        static float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(Prefix + key, fallback);
        static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;

        static void SetFloat(string key, float value, float min, float max)
        {
            PlayerPrefs.SetFloat(Prefix + key, Mathf.Clamp(value, min, max));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
