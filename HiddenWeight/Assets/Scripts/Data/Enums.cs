namespace HiddenWeight.Data
{
    // 감정 스킬 식별자. 명시적 숫자를 붙여 인스펙터에 저장된 값이
    // enum 순서가 바뀌어도 깨지지 않도록 한다.
    public enum EmotionId { None = 0, Rewind = 1, Hush = 2, Foresight = 3 }

    // 지역 식별자. 위와 동일한 이유로 명시적 숫자를 붙인다.
    public enum ZoneId { Prologue = 0, Residue = 1, Gaze = 2, Fracture = 3 }

    // 스킬 입력 방식. Hold(홀드) / Tap(탭).
    public enum SkillInput { Hold = 0, Tap = 1 }
}
