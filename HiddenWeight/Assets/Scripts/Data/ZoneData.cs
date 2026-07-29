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

        // 지역별 HUD 상태 문양(잔재 StatusRewind… / 응시 GazeStatusTruth…).
        // HUD 프리팹은 하나뿐이라 지역이 바뀌면 여기서 프레임을 갈아끼운다.
        // 비어 있으면 HUD가 들고 있는 기본 프레임을 그대로 쓴다.
        public Sprite[] statusRewindFrames;
        public Sprite[] statusDangerFrames;
        public Sprite[] statusProgressFrames;

        // UI 아이콘 시트의 4행: 미발견/발견/완료/재방문/현재/목표/보스 격파/지역 완료.
        // 지도는 실제 방을 축소하지 않고 이 실루엣으로 기억 상태를 표현한다.
        public Sprite[] mapStateIcons;
    }
}
