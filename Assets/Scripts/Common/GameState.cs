namespace HanGame.Common
{
    /// <summary>게임 전체 상태. GameManager가 이 상태로 씬과 흐름을 제어한다.</summary>
    public enum GameState
    {
        Boot,       // 부팅/타이틀
        Prologue,   // 회귀 프롤로그 연출
        Day,        // 낮 디펜스
        DayLevelUp, // 레벨업 선택(시간 정지)
        Night,      // 밤 잠입
        Fired,      // 해고 연출(회귀)
        Ending,     // CEO 취임 엔딩
        Result      // 결과 화면
    }

    /// <summary>층의 낮/밤 단계.</summary>
    public enum FloorPhase
    {
        Day,
        Night
    }
}
