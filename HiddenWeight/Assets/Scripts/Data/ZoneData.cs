using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenWeight.Data
{
    // 지역(프롤로그/잔재/응시/균열) 1개의 설정을 담는 ScriptableObject.
    [CreateAssetMenu(fileName = "ZoneData", menuName = "HiddenWeight/Zone Data")]
    public class ZoneData : ScriptableObject
    {
        public ZoneId id;
        public string displayName; // "몽환의 우주" / "잔재" / "응시" / "균열"
        public string sceneName; // Zone_Prologue 등
        public string nextSceneName; // 클리어 시 넘어갈 씬
        public EmotionId grantedSkill; // 이 지역에서 얻는 스킬. 프롤로그는 None
        public bool grantsAwareness; // 응시만 true
        public bool awarenessStable = true; // 균열만 false
        public VolumeProfile volumeProfile; // 지역 색보정
        public AudioClip bgm; // MVP에서는 비워둔다
    }
}
