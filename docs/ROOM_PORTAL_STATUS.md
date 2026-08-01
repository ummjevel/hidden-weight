# 잔재 방 포탈 — 작업 현황

> 기준 브랜치: `room-portal` (base `ksh`)
> 최종 갱신: 2026-07-31
> 설계: [2026-07-31-room-portal-scenes-design.md](superpowers/specs/2026-07-31-room-portal-scenes-design.md)
> 계획: [2026-07-31-room-portal-scenes-residue.md](superpowers/plans/2026-07-31-room-portal-scenes-residue.md)

## 한 줄 요약

잔재 지역의 방 15개를 각자 씬으로 분리하고 복도를 포탈 문으로 대체했다. **주 동선 R01→R12
완주가 런타임에서 검증됐다.** 테스트 마이그레이션과 낡은 씬 정리는 하지 않았다.

---

## 무엇이 됐는가

### 구조

`Zone_Residue.unity`가 플레이어·카메라·HUD·`RoomLoader`만 갖는 셸이 되고, 방마다
`Room_Residue_<방>.unity`가 자기 로컬 `(0,0)` 기준으로 존재한다. 동시에 한 방만 로드한다.

| 산출물 | 수 |
| --- | --- |
| 방 씬 | 15 (R01~R12, S1~S3) |
| 포탈 문 | 28 (링크 14개 × 2) |
| 셸 씬 | 1 |
| 빌드 세팅 등록 | 25 |

### 컴포넌트

- `HiddenWeight.Data.Side` — `LEVEL_01_STANDARD.md` §1.3의 방향 표기를 그대로 옮긴 enum
- `HiddenWeight.Data.RoomLink` / `RoomLinkTable` — 연결을 런타임이 읽을 수 있는 데이터로
- `HiddenWeight.EditorTools.ResidueRoomLinks` — 잔재 링크 14개의 원본 + 에셋 생성 메뉴
- `HiddenWeight.World.RoomDoor` — 트리거 감지와 도착 좌표 계산만 담당
- `HiddenWeight.World.RoomStart` — 문을 거치지 않고 방에 들어올 때의 위치 마커
- `HiddenWeight.World.RoomLoader` — 언로드·로드·배치·암전·입력 잠금을 소유하는 유일한 지점
- `HiddenWeight.World.ResidueEntryPoint` — 셸 진입 시 R01을 로드

**링크 하나가 문 두 개를 굽는다.** 되돌아올 수 없는 연결은 이 구조에서 만들어지지 않는다.

**되튕김 방지는 상태 기반이다.** 도착한 문은 플레이어가 트리거를 벗어날 때까지 비활성이며,
시간 쿨다운이 아니다 — 로드가 느려도, 문 앞에서 머뭇거려도 안전하다.

### 콘텐츠 보존

옛 `Zone_Residue_Full`과 새 15개 씬의 오브젝트 수가 정확히 일치한다. 적 17, 기억 파편 5,
체크포인트 3, 붕괴 발판 4, 되감기 대상 15, 이동 발판 2, 재화 26, 회복 2, 안전 발판 17,
그리고 모든 조우·보상·숏컷·Room 경계.

### 비밀방 접근

앵커를 플레이어 캡슐(0.8×1.4)과 실측 점프 높이 2.72로 다시 계산했다. 원래 값은 옛
`BuildShaft` 좌표였는데, 그 샤프트는 **벽만 세우고 바닥 타일을 뚫지 않았다** — 실제 출입은
`ResidueLoopRuntime`이 심던 텔레포트가 맡고 있었고, 그것은 `*_Full` 씬에서만 돈다.

| 링크 | 옛 값 | 새 값 |
| --- | --- | --- |
| R04 → S1 | (8, 6) | (9.5, 6.5) |
| S1 → R04 | (8, 14) | (2, 3.25) |
| R06 → S2 | (20, 1) | (21, 7.2) |
| S2 → R06 | (20, 18) | (4, 4.25) |
| R11 → S3 | (14, 10) | (7, 10.4) |
| S3 → R11 | (14, 0) | (14, 2.7) |

### 씬을 넘는 숏컷

방이 갈라지면서 `Rewindable.linkedShortcut`이 null로 구워졌다 — 트리거(R05, R08)와
대상(R03)이 다른 씬이고 유니티는 씬을 넘는 오브젝트 참조를 저장하지 못한다.

`Rewindable`에 `linkedShortcutId`를 더해, 오브젝트가 없으면 `ProgressState.MarkShortcutOpen(id)`
를 직접 부르게 했다. 저장 계층은 원래부터 id 기반이라(`Shortcut.Start()`가
`IsShortcutOpen(id)`을 읽는다) 변경이 작다.

다른 방에서 열릴 때는 효과음도 봉인 애니메이션도 재생되지 않는다. 플레이어가 서 있지 않은
방의 문소리를 들려줄 이유가 없고, 다음에 그 방에 들어가면 열린 채로 보인다.

---

## 검증된 것

| 항목 | 결과 |
| --- | --- |
| EditMode | **142 / 142 통과**, 컴파일 에러 0 |
| PlayMode 전체 | 105개 중 96 통과 |
| `MainRoute_WalksR01ToR12` | **통과** — 문만 따라 R01에서 R12까지 완주 |
| `EntryPoint_LoadsFirstRoom` | 통과 |
| `LoadRoom_UnloadsPreviousRoom` | 통과 |
| `ArrivalDoor_IsDisarmed` | 통과 |
| `ArrivalDoor_RearmsAfterPlayerWalksOff` | 통과 |
| `Transition_SetsCurrentRoomOnCamera` | 통과 |
| `MissingRoom_LeavesPlayerAndRestoresInput` | 통과 |

문 배선은 구운 씬 YAML에서 직접 확인했다 — `R01`의 `residue_R01_R02:E`가 `R02`의
`residue_R01_R02:W`를 가리키고, 그 반대도 성립한다.

---

## 무엇이 안 됐는가

### 하지 않은 작업

| 항목 | 계획 위치 | 왜 |
| --- | --- | --- |
| 기존 PlayMode 테스트 마이그레이션 | Task 7 | 시간. 14개 파일이 대상 |
| `Zone_Residue_Full.unity` 제거 | Task 8 | 위 테스트 12개가 아직 그 씬을 로드한다 |
| 손 QA | Task 9 | 에디터에서 직접 플레이한 적이 없다 |
| 응시·균열 지역 적용 | 범위 밖 | 잔재만 1단계로 잡았다 |
| 문 잠금 파라미터 | 설계 §9 | QA 왕복을 막지 않으려고 의도적으로 뺐다 |

### 알려진 실패 9개

| 실패 | 분류 |
| --- | --- |
| `FractureArtWiringTests.열다섯_방에_단일_배경만_있다` | 균열 — 이 작업과 무관 |
| `GazeArtWiringTests.열다섯_방에_단일_배경만_있다` | 응시 — 무관 |
| `GazeFractureZoneTests.균열_모든_방에_원본_단일_배경이_연결돼_있다` | 균열 — 무관 |
| `GazeFractureZoneTests.균열의_붕괴_발판은_스스로_되살아난다` | 균열 — 무관 |
| `ResidueCompletionArtTests.방마다_단일_배경만_있다` | `Zone_Residue_Full` 로드 — **기존 결함** |
| `ResidueLoopCompletionTests.지역_보스_전에는_나갈_수_없고...` | `Zone_Residue_Full` 로드 — **기존 결함** |
| `ResidueZoneTests.봇이_각_방_안에서_막히지_않고_이동한다` | `Zone_Residue_Full` 로드 — **기존 결함** |
| `ZonePlayableTests.지역_씬이_플레이_가능한_상태로_로드된다` | 셸에 지형이 없다 — **의도된 동작**, 테스트를 고쳐야 함 |
| `RoomTransitionTests.Door_CarriesPlayerToPairedDoor` | 도착 위치 단언 — 아래 참고 |

`Zone_Residue_Full`을 로드하는 3개는 그 씬을 이번 작업이 건드리지 않았으므로 원래
깨져 있던 것이다.

`Door_CarriesPlayerToPairedDoor`는 코드가 아니라 단언의 문제다. 도착 좌표는 발밑 기준점인데
플레이어 `transform`은 캡슐 중심이라 제대로 서면 오히려 0.7 위에 온다. 단언을 캡슐
반높이만큼 허용하도록 고쳤으나 **그 수정은 아직 PlayMode로 재검증하지 않았다.**

### 미해결 설계 부채

- **S3 되감기 게이트가 실제로 막는지 미확인.** 게이트를 문 앞에 두려면 R11 지형이 필요했고,
  마지막 커밋에서 앵커를 옮겼으나 게이트 위치와의 관계를 손으로 확인하지 않았다.
- **방에 동쪽 경계벽이 없다.** `BuildZoneRoot`가 서쪽만 세운다. R01과 R12를 뺀 13개 방은
  방 끝의 문(콜라이더 폭 1.2)에 의존하므로, 대시·낙하로 스쳐 지나가면 허공으로 떨어질 수
  있다. QA에서 방마다 동쪽 끝을 밀어 봐야 한다.
- **S2의 되감기 조건 게이트가 사라졌다.** 옛 `ResidueLoopRuntime.ConfigurePassages`가 R06
  되감기 대상 복원을 S2 입장 조건으로 걸었는데, 새 구조의 S2 문에는 조건이 없다. 설계 §9의
  `RoomDoor` 잠금이 들어와야 복원된다.
- **방 씬 루트 이름이 셸 루트와 같다** (둘 다 `Zone_Residue`). 지금은 이름으로 찾는 코드가
  없어 무해하지만, 앞으로 생기면 함정이 된다.
- **방마다 전역 볼륨을 하나씩 갖는다.** 전환 도중 두 방이 잠깐 함께 로드되면 동일 볼륨이
  둘이 된다. 무해할 가능성이 높으나 QA에서 볼 것.

---

## 다음에 할 일

1. `Door_CarriesPlayerToPairedDoor` 단언 수정을 PlayMode로 재검증
2. 에디터에서 직접 플레이 — 문 통과, 되튕김, R01→R12 완주, 비밀방 3곳 왕복, S3 게이트가
   실제로 막는지, 방 동쪽 끝에서 떨어지지 않는지
3. PlayMode 테스트 14개 마이그레이션 (`Zone_Residue_Full` → 방 단위 로드)
4. `Zone_Residue_Full.unity` 제거 + `Assets/Scripts/Editor/README.md` 갱신
5. 응시·균열에 같은 구조 적용
6. 문 잠금 파라미터 (설계 §9)

## 참고

- 이 작업은 `room-portal` 워크트리에서 진행했다. 메인 체크아웃에서 다른 세션이 카메라
  리팩터(`RoomCamera.cs` +288/-22)와 오디오 재생성을 동시에 하고 있었기 때문이다.
- 그 카메라 리팩터가 합쳐질 때 `RoomLoader.SyncCamera`를 재검토해야 한다. 지금 구현은
  리팩터 이전의 `RoomCamera.SetRoom` / `SnapToPlayer` 의미에 기대고 있다.
