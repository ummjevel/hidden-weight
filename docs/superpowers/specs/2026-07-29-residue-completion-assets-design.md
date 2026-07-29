# 잔재 이미지 제작 100% 보완 세트 설계

## 목표

기존 잔재 지역의 배경, 적, 보스, 플레이어, 아이템, 환경 장치 이미지에는 손대지 않고 실제 플레이 화면을 마감하는 데 부족한 이미지 9종을 추가한다. 이 세트를 완료하면 Unity 연결 여부와 무관하게 1맵 `잔재`의 이미지 제작 범위를 100%로 판정한다.

## 범위

- 지역: 1맵 `잔재`
- 포함: 적·보스 공격체, 발판 상태, 충돌 VFX, 전경·배경 모션, 방 전환, 지역 전용 UI, 상태 UI
- 제외: 기존 플레이어·적·보스 본체 스프라이트 재생성, Unity Sprite 분할, AnimationClip 및 Animator 연결
- 기존 PNG는 덮어쓰지 않고 신규 파일만 추가한다.

## 시각 기준

- 팔레트: 회갈색 석재, 먹색 철골, 탁한 앰버 발광, 아주 제한적인 남색 그림자
- 재질: 녹슨 철, 거친 석재, 말라붙은 조직, 사슬, 천, 기억성 먼지
- 공포 방향: 거대한 손과 사람 형상이 건축물처럼 굳어 있는 잔재 지역의 기괴함
- 가독성: 공격 예고는 앰버, 위험 활성은 밝은 황백색, 되감기 가능한 상태는 옅은 남색·청백색으로 구분한다.
- 배경과 전경 효과는 플레이어와 공격 판정보다 명도가 낮아야 한다.

## 납품 파일

### 1. `ResidueEnemyProjectiles_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/`
- 격자: 8열 × 4행
- 행:
  1. 잔재 보행자 석재 파편
  2. 매달린 손가락의 길게 뻗는 손톱 궤적
  3. 애도 운반자의 앰버 돌진 잔상
  4. 굳은 잔재의 압축 충격파

### 2. `ResidueBossProjectiles_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/`
- 격자: 8열 × 5행
- 행:
  1. 손목 감시자 횡방향 감시 파동
  2. 손목 감시자 낙하 충격 고리
  3. 기억 교관 기억침
  4. 기억 교관 되감기 구체
  5. 보스 페이즈 전환 파열

### 3. `ResiduePlatformStates_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Environment/Terrain/Animation/`
- 격자: 8열 × 4행
- 행:
  1. 정상 발판의 미세 흔들림과 균열
  2. 붕괴 진행과 파편 낙하
  3. 완전히 부서진 상태
  4. 되감기로 역조립되는 상태

### 4. `ResidueImpactVFX_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/Animation/`
- 격자: 8열 × 4행
- 행:
  1. 기본 근접 타격
  2. 벽·석재 충돌
  3. 일반 착지 먼지
  4. 강한 낙하·대형 물체 충돌

### 5. `ResidueForegroundMotion_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Environment/VFX/Animation/`
- 격자: 8열 × 4행
- 행:
  1. 가까운 사슬 흔들림
  2. 철창과 매달린 천 흔들림
  3. 화면 가장자리의 거대한 손가락 실루엣
  4. 낮은 전경 먼지와 파편

### 6. `ResidueBackgroundMotion_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Environment/VFX/Animation/`
- 격자: 8열 × 4행
- 행:
  1. 먼 연기와 재
  2. 폐허 창문의 불규칙 앰버 점멸
  3. 먼 거대 손의 느린 위치 변화
  4. 굳은 사람 형상 군집의 미세한 동조 움직임

### 7. `ResidueRoomTransitions_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/`
- 격자: 8열 × 4행
- 행:
  1. 철문·사슬 방 봉쇄
  2. 봉쇄 해제
  3. 지름길 개방
  4. 비밀 벽과 손가락 틈 개방

### 8. `ResidueUIIcons_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/UI/`
- 격자: 8열 × 4행, 총 32개 정적 아이콘
- 구성:
  - 입구, 출구, 위·아래 연결, 일반 문, 잠긴 문, 지름길, 비밀 통로, 엘리베이터
  - 체크포인트, 회복, 화폐, 기억 조각, 체력 조각, 되감기 오브젝트, 위험물, 붕괴 발판
  - 일반 적 4종, 중간 보스, 지역 보스, NPC, 기록물
  - 미발견, 발견, 완료, 재방문, 현재 위치, 목표, 보스 격파, 지역 완료
- 텍스트와 숫자를 넣지 않고 실루엣으로 구분한다.

### 9. `ResidueStatusUI_v1.png`

- 위치: `HiddenWeight/Assets/Art/Residue/UI/Animation/`
- 격자: 8열 × 3행
- 행:
  1. 되감기 충전·사용·고갈
  2. 피격·위험 누적·위험 해제
  3. 기억 획득·보스 경고·지역 완료

## 생성 및 후처리

- 각 파일은 별도의 ImageGen 호출로 생성한다.
- 생성 원본은 균일한 `#00ff00` 크로마키 배경을 사용한다.
- 로컬 크로마키 제거 후 RGBA PNG로 저장한다.
- 모든 애니메이션 시트는 가로 8프레임으로 정규화한다.
- UI 아이콘은 각 셀 중심과 외곽 여백을 동일하게 유지한다.
- 공격체·VFX는 Center 피벗, 발판·문은 Bottom Center 피벗을 전제로 구성한다.

## 검수 기준

- 9개 파일이 모두 신규 경로에 존재한다.
- 모든 결과가 RGBA PNG이며 네 모서리의 알파가 0이다.
- 각 파일이 명세 격자로 정확히 등분된다.
- 애니메이션 행은 프레임이 왼쪽에서 오른쪽으로 자연스럽게 진행된다.
- 공격체에는 예고·유효·소멸 상태가 구분된다.
- 전경·배경 루프는 첫 프레임과 마지막 프레임 사이에 큰 위치 점프가 없다.
- 32개 지도 아이콘은 축소 상태에서도 서로 구분된다.
- 기존 잔재 이미지와 색·재질·명암이 충돌하지 않는다.

## 완료 판정

이 9종의 생성·투명화·격자 검사가 끝나면 잔재 지역의 이미지 제작을 100%로 판정한다. 이후 남는 작업은 Unity Sprite 분할, AnimationClip, Animator, VFX 재생 및 UI 연결이다.
