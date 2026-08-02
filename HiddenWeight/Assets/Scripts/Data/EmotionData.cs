using UnityEngine;

namespace HiddenWeight.Data
{
    // 감정 스킬(되감기/숨죽이기/예지) 1종의 수치를 담는 ScriptableObject.
    // 감정마다 값이 크게 달라 클래스 차원의 공용 기본값은 없다.
    // 각 필드에 실제로 들어갈 감정별 수치는 브리프 표를 참고해 인스턴스별로 설정한다.
    [CreateAssetMenu(fileName = "EmotionData", menuName = "HiddenWeight/Emotion Data")]
    public class EmotionData : ScriptableObject
    {
        public EmotionId id;
        public string displayName; // "되감기" / "숨죽이기" / "예지"
        public SkillInput inputMode; // Rewind·Hush = Hold, Foresight = Tap

        public float channelTime; // 되감기 채널링 1.0. 나머지 0
        public float cooldown; // 되감기 2, 예지 3, 숨죽이기 0
        public float range; // 되감기 6, 예지 8
        public float moveSpeedMultiplier; // 되감기 0(이동 불가), 숨죽이기 0.65, 예지 1
        public float effectDuration; // 예지 고스트 표시 시간 1.5
        public float previewLeadTime; // 예지가 내다보는 미래 초 2.0
        public float hushScale; // 숨죽이기 스케일 배수 0.55
    }
}
