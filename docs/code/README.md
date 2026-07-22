# HAN GAME (Rookie to CEO) — 코드 문서 (Code Docs)

이 디렉터리는 `Assets/Scripts` 이하 **전체 소스 코드의 파일별 기능 문서**다.
게임 기획은 [../../GAME_DESIGN.md](../../GAME_DESIGN.md), 프로젝트 구조/모듈 개요는
[../../PROJECT_STRUCTURE.md](../../PROJECT_STRUCTURE.md)를 참고한다.

## 게임 한 줄 요약

과거의 실패 후 신입사원 시절로 **회귀**한 주인공이, 낮에는 사방에서 몰려오는 업무(이메일·서류·회의 요청 등
사무용품 형태의 적)를 처리하고, 밤에는 경비와 야근자를 피해 회사 기밀과 무기를 확보하며 4개 층을 올라
CEO가 되는 **2D 탑뷰 서바이버형 회사 디펜스 게임**. HP(멘탈)가 0이 되면 평판이 감소하고, 평판을 모두
잃으면 해고 후 1층 첫 출근으로 회귀한다.

```
GameManager(Common)
  ├─ 낮 진입 → DayCombatManager(Day)  ── 생존 → 층++ → 밤(1~3층) / 최종 클리어(4층)
  │                                     └ 평판 0 → 해고 → RunState 리셋 → 1층 재시작
  └─ 밤 진입 → NightStealthManager(Night) ── 무기 획득·탈출 → 다음 층 낮
                                          └ 발각·시간초과 → RunState 리셋 → 1층 재시작
```

## 문서 구성 (모듈별)

| 문서 | 대상 폴더 | 내용 | 기획서 대응 |
|---|---|---|---|
| [Common.md](Common.md) | `Assets/Scripts/Common` | 공통 시스템 (게임 상태, 이동, HP·평판, 층 진행, 오디오, 런 상태) | 19.1 / 3·6장 |
| [Data.md](Data.md) | `Assets/Scripts/Data` | ScriptableObject 데이터 테이블 (플레이어·무기·적·웨이브·층·경비) | 19.4 |
| [Day.md](Day.md) | `Assets/Scripts/Day` | 낮 전투 (적 생성·AI, 자동 공격, 경험치·레벨업, 강화, 상사의 시선, 웨이브 타이머) | 19.2 / 5·7·10장 |
| [Weapons.md](Weapons.md) | `Assets/Scripts/Weapons` | 무기·스킬 (키보드 샷건, 스테이플러, 업무 떠넘기기, 퇴사 통보) | 8장 |
| [Night.md](Night.md) | `Assets/Scripts/Night` | 밤 잠입 (경비 순찰, 시야 판정, CCTV, 조사, 무기 획득, 탈출, 소음) | 19.3 / 11장 |
| [UI.md](UI.md) | `Assets/Scripts/UI` | HUD 및 화면 (부트 메뉴, 낮 HUD, 밤 HUD, 레벨업, 결과) | 14장 |

각 모듈 폴더에도 기능 개요를 담은 `README.md`가 함께 있다. 이 `docs/code/` 문서는 그보다 상세한
**파일 단위** 설명(역할 / 상속·의존 / 주요 멤버 / 동작)을 제공한다.

## 아키텍처 원칙

- **모듈 분리**: `Common / Data / Day / Weapons / Night / UI` 6개 모듈로 책임을 나눈다.
- **데이터 외부화**: 수치는 `Data`의 ScriptableObject 에셋으로 관리해 코드 재컴파일 없이 밸런싱한다.
- **런 상태 중심**: `RunState`가 현재 층·스탯·보유 무기를 들고 낮/밤 씬을 오간다.

## 조작 요약

| 입력 | 기능 |
|---|---|
| WASD / 방향키 | 이동 |
| 자동 | 최근접 적을 기본 무기로 공격 |
| Space | 업무 떠넘기기 (액티브) |
| R | 퇴사 통보 (궁극기) |
| E | 밤 잠입 상호작용(조사) |
| Shift | 밤 잠입 달리기 |
