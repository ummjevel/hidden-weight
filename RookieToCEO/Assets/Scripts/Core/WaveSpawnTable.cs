using System;
using System.Collections.Generic;

namespace RookieToCEO.Core
{
    // GDD 5번(층별 낮 디펜스 구조)의 "0~15/15~30/30~45/45~60초" 공통 시간 구성과
    // 1~4층별 등장 적 규칙. 정확한 스폰 간격(초당 개체 수)은 TBD(M9 플레이테스트에서 확정,
    // docs/DEVELOPMENT_PLAN.md 참고)이라 여기서는 "이 시점에 어떤 적 타입이 등장 가능한가"와
    // "생성 속도 배율"만 계산한다. SpawnManager(MonoBehaviour)가 이 값을 써서 실제 스폰을 결정한다.
    public static class WaveSpawnTable
    {
        // GDD 5번 공통 시간 구성: 30초부터 상사의 시선, 30~45초부터 생성량 증가.
        public static float GetSpawnRateMultiplier(float elapsedSeconds)
        {
            return elapsedSeconds >= 30f ? 1.5f : 1f;
        }

        // GDD 5번 "1층: 적 생성량이 적음" ~ "3층: 생성량이 크게 증가"의 층별 기준 스폰 간격(초).
        // 값이 작을수록 더 자주 스폰된다. M9 밸런싱에서 처음 확정한 값이며, 플레이테스트로
        // 더 조정될 수 있다(docs/DEVELOPMENT_PLAN.md 스폰 테이블 참고).
        // 이전에는 모든 층이 같은 기준 간격(2초)을 썼는데, 그러면 GDD가 요구하는 층별 난이도
        // 곡선이 전혀 반영되지 않고 1층에서부터 과도하게 많은 적이 나와 레벨업이 목표(층당
        // 2~3회)보다 훨씬 자주 발생하는 문제가 있었다.
        public static float GetBaseSpawnIntervalSeconds(int floor)
        {
            switch (floor)
            {
                case 1: return 2.5f;
                case 2: return 2.0f;
                case 3: return 1.5f;
                case 4: return 1.2f; // 보스 웨이브 - 가장 밀집
                default:
                    throw new ArgumentOutOfRangeException(nameof(floor), floor, "층은 1~4만 유효하다");
            }
        }

        public static HashSet<EnemyType> GetActiveEnemyTypes(int floor, float elapsedSeconds)
        {
            var types = new HashSet<EnemyType>();

            switch (floor)
            {
                case 1:
                    // GDD: 1층은 이메일 봉투 + 서류 더미만, 키보드 샷건만 사용하는 조작 학습 구간.
                    types.Add(EnemyType.EmailEnvelope);
                    if (elapsedSeconds >= 15f) types.Add(EnemyType.DocumentStack);
                    break;

                case 2:
                    // GDD: 빠른 업무(포스트잇) 추가.
                    types.Add(EnemyType.EmailEnvelope);
                    types.Add(EnemyType.DocumentStack);
                    if (elapsedSeconds >= 15f) types.Add(EnemyType.PostItRush);
                    break;

                case 3:
                    // GDD: 방해형 업무(회의 요청 달력, 클레임 전화기) 추가.
                    types.Add(EnemyType.EmailEnvelope);
                    types.Add(EnemyType.DocumentStack);
                    types.Add(EnemyType.PostItRush);
                    if (elapsedSeconds >= 15f)
                    {
                        types.Add(EnemyType.MeetingCalendar);
                        types.Add(EnemyType.ClaimPhone);
                    }
                    break;

                case 4:
                    // GDD 13번: 0~20초 업무 폭탄, 20~40초 전면 수정, 40~60초 퇴근 취소(보스 웨이브).
                    types.Add(EnemyType.EmailEnvelope);
                    types.Add(EnemyType.DocumentStack);
                    if (elapsedSeconds >= 20f)
                    {
                        types.Add(EnemyType.PostItRush);
                        types.Add(EnemyType.MeetingCalendar);
                    }
                    if (elapsedSeconds >= 40f) types.Add(EnemyType.CeoFinalOrder);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(floor), floor, "층은 1~4만 유효하다");
            }

            return types;
        }
    }
}
