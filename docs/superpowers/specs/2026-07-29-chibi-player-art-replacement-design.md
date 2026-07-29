# 치비 메인 플레이어 아트 교체 설계

## 1. 목적

`docs/chibi.png`의 캐릭터를 게임의 새 메인 플레이어로 고정한다. 기존 플레이어의 이동·공격
판정과 충돌 크기는 유지하고, 플레이어가 등장하는 모든 런타임 스프라이트와 플레이어 포함
VFX만 새 외형으로 교체한다.

## 2. 시각 기준

- 기준 이미지: `docs/chibi.png`
- 유지할 특징:
  - 흰색 단발머리와 위로 솟은 한 가닥 머리
  - 크고 밝은 청록색 눈
  - 크림색 터틀넥 원피스
  - 짙은 회색 레깅스와 흰색 부츠
  - 머리·목걸이·옷·부츠의 작은 청록 보석과 식물 장식
- 게임용 조정:
  - 흰 머리와 크림색 의상이 밝은 배경에서도 분리되도록 먹색·회보라 외곽선을 강화한다.
  - 옷의 그림자는 차가운 회보라색, 보석 발광은 제한적인 청록색으로 처리한다.
  - 장신구는 축소 화면에서 노이즈가 되지 않도록 개수와 위치는 유지하되 선을 단순화한다.
  - 얼굴 비율과 의상 길이는 프레임마다 바꾸지 않는다.
- 공격은 별도 무기를 추가하지 않고 기존처럼 밝은 초승달형 에너지 베기를 사용한다.

## 3. 공통 제작 규칙

- 2D 횡스크롤 측면 시점, 캐릭터는 기본적으로 화면 오른쪽을 본다.
- 모든 시트는 균일 격자와 투명 RGBA PNG를 사용한다.
- 생성 원본은 완전히 균일한 `#00FF00` 크로마키 배경으로 만들고 로컬에서 알파로 변환한다.
- 머리 높이, 몸통 길이, 발바닥 기준선, 카메라 각도와 조명 방향을 전 프레임에서 고정한다.
- 셀마다 캐릭터 한 명만 배치하며 프레임 간 겹침, 잘림, 배경, 글자, 라벨, 워터마크를 금지한다.
- 실제 이동은 Unity 물리가 담당하므로 루트 이동을 프레임 안에 과도하게 그리지 않는다.
- Pivot은 모든 플레이어 프레임에서 `Bottom Center`로 통일한다.

## 4. 교체 대상

### 4.1 핵심 포즈

경로:
`HiddenWeight/Assets/Art/Residue/Gameplay/Player/Player_KeyPoses_v1.png`

- 4×2, 8포즈
- Idle, Walk, Run, Jump, Fall, Land, Attack, Dash
- 기존 1536×1024 캔버스와 런타임 이름을 유지한다.

### 4.2 지상 이동

경로:
`HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Locomotion_v1.png`

- 8×3, 24프레임
- 1행 Idle, 2행 Walk, 3행 Run
- 2048×768, 셀 256×256
- 대기 호흡은 작게, 걷기와 달리기는 머리의 상하 진폭을 제한한다.

### 4.3 공중 동작

경로:
`HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Aerial_v1.png`

- 6×4, 24프레임
- Jump, AirMove, Fall, Land
- 1536×1024, 셀 256×256
- 착지 마지막 프레임은 Idle 첫 프레임과 자연스럽게 이어진다.

### 4.4 공격·대시

경로:
`HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Actions_v1.png`

- 6×2, 12프레임
- Attack, Dash
- 캔버스 2172×724, 셀 362×362로 다시 정확히 분할한다.
- 베기 VFX는 공격 행에만 포함하고 캐릭터 몸과 분리되어 읽히게 한다.

### 4.5 벽 동작

경로:
`HiddenWeight/Assets/Art/Residue/Gameplay/Player/Animation/Player_Wall_v1.png`

- 6×2, 12프레임
- WallCling, WallJump
- 캔버스 2172×724, 셀 362×362로 다시 정확히 분할한다.
- 벽은 그리지 않고 캐릭터의 손·발 자세로 접촉 방향만 표현한다.

### 4.6 플레이어 VFX

경로:
`HiddenWeight/Assets/Art/Residue/Gameplay/VFX/PlayerVFX_v1.png`

- 6×3
- Hit, Death, Respawn
- 캐릭터가 포함된 프레임을 새 치비 외형으로 교체한다.
- 섬광·연기·입자는 기존 세계관의 흰색·회보라·청록 범위로 제한한다.

## 5. 신규 능력 시트

### 5.1 숨죽이기

신규 경로:
`HiddenWeight/Assets/Art/Player/Abilities/Player_Hush_v1.png`

- 6×3, 18프레임
- Hush Begin, Hush Move, Hush End
- 몸을 단순 축소하지 않고 어깨를 움츠리고 옷자락을 안으로 모으는 전용 자세를 사용한다.
- 현재 콜라이더 축소 로직과 맞도록 전체 실루엣은 평상시 높이의 약 60% 안에 들어온다.

### 5.2 자각

신규 경로:
`HiddenWeight/Assets/Art/Player/Abilities/Player_Awareness_v1.png`

- 6×3, 18프레임
- Awareness Begin, Awareness Loop, Awareness Unlock
- 눈과 보석의 청록 발광, 한 박자 늦는 이중 윤곽을 사용한다.
- G11 해금 연출에 필요한 정면 응시 마지막 포즈를 포함한다.

## 6. 생성·교체 순서

1. `chibi.png`와 기존 핵심 포즈 시트를 참조해 새 `Player_KeyPoses`를 만든다.
2. 핵심 포즈에서 외형 일관성을 확인한 뒤 나머지 네 애니메이션 시트를 각각 생성한다.
3. 플레이어 VFX, 숨죽이기, 자각 시트를 생성한다.
4. 모든 크로마키 원본을 보존하고 적용본만 기존 런타임 PNG에 덮어쓴다.
5. Unity 메타데이터의 Sprite 이름은 유지하고 잘못된 Actions·Wall 셀 크기만 362×362로 교정한다.

## 7. 검증 기준

- 기준 이미지의 머리·눈·의상·레깅스·부츠·청록 장식이 모든 기본 포즈에서 식별된다.
- 모든 적용본이 RGBA이며 녹색 불투명 픽셀이 없다.
- 프레임마다 발바닥 기준선과 캐릭터 전체 크기가 안정적이다.
- 반복 클립의 첫 프레임과 마지막 프레임이 튀지 않는다.
- 공격·피격·사망 프레임에서도 다른 캐릭터로 보이는 얼굴·의상 변화가 없다.
- 기존 Sprite 이름과 애니메이션 클립 참조가 유지된다.
- 현재 플레이어 충돌 크기와 공격 판정을 변경하지 않는다.

## 8. 비범위

- 적·보스·NPC 외형 변경
- 배경·환경·UI 교체
- 이동 수치, 충돌 크기, 공격 범위 변경
- 스켈레탈 애니메이션 도입
