# 응시 지역 자연스러운 움직임용 스프라이트 설계

## 목표

응시 지역의 기존 배경·환경 아틀라스와 일치하는 적, 보스, 위험물, 장치, 아이템,
환경 루프, VFX 애니메이션 시트를 제작한다. 플레이어 스프라이트는 기존 치비 세트를
공통 사용하므로 범위에서 제외한다.

## 시각 기준

- 저채도 보라, 청록, 먹색을 기본 팔레트로 사용한다.
- 눈·홍채·눈꺼풀·관객석·철창·고딕 석조 구조를 핵심 모티브로 유지한다.
- 눈은 단순 장식이 아니라 깜박임, 초점 이동, 노출 경고, 감지 상태가 구분되어야 한다.
- 발각과 진짜 공격은 자홍빛 홍채와 날카로운 청록 윤곽으로 읽히게 한다.
- 자각 반응은 청록 이중 윤곽과 역방향 그림자, 숨죽이기는 낮아진 채도와 닫히는 눈꺼풀로 표현한다.
- 다안공포는 사용하지만 모든 프레임을 눈으로 채우지 않는다. 공격 정보가 묻히지 않도록
  보스·위험 상태에서 집중적으로 사용한다.

## 공통 제작 규칙

- 모든 산출물은 투명 RGBA PNG다.
- 기존 파일을 덮어쓰지 않고 `Animation/` 하위에 신규 파일로 저장한다.
- 이동 캐릭터는 `Bottom Center`, 중심 효과는 `Center` 피벗을 기준으로 한다.
- 공격은 `예고 → 유효 → 회수`가 최소 2프레임 간격으로 구분되어야 한다.
- 첫 프레임과 마지막 프레임이 이어지는 행만 루프로 사용한다.
- 좌우 방향은 별도 시트가 아니라 `SpriteRenderer.flipX`로 처리한다.
- 충돌과 피해 판정은 이미지에서 분리한다.
- 텍스트, 숫자, 셀 선, 워터마크, 배경 장면을 넣지 않는다.

## 산출물

### 적

각 파일은 `8열 × 6행`이며 행 순서는 `Idle / Move / Telegraph / Attack / Hit / Death`다.

1. `BlindPilgrim_v1.png`
2. `InformingMouth_v1.png`
3. `HangingAudience_v1.png`
4. `FacelessJudge_v1.png`

### 홍채의 문지기

5. `IrisGatekeeper_Combat_v1.png`

- `8열 × 7행`
- Idle / Iris Sweep / Eyelid Close / Charge / Dual Gaze / Hurt / Death

6. `IrisGatekeeper_Transitions_v1.png`

- `8열 × 3행`
- Entrance / Phase Transition / Shortcut Gate Opening

### 만인의 시선

7. `GazeOfAll_Combat_v1.png`

- `8열 × 7행`
- Idle / Fixed Gaze / Rotating Gaze / Projectile / True Strike / Hurt / Death

8. `GazeOfAll_Deceptions_v1.png`

- `8열 × 4행`
- False Telegraph / True Telegraph Reveal / Delayed Clone / Empty Stage

9. `GazeOfAll_Reactions_v1.png`

- `8열 × 3행`
- Awareness Exposure / Final Confrontation / Audience Turn-Away

### 위험물·장치

10. `EyeHazardTransitions_v1.png`

- `8열 × 5행`
- Fixed Eye / Rotating Iris / Floor Gaze / Ceiling Eye / Alarm Burst

11. `CoverTransitions_v1.png`

- `8열 × 4행`
- Eyelid Pillar / Curtain Cover / Cage Cover / Boss Cover

12. `TransitTransitions_v1.png`

- `8열 × 3행`
- Gaze Lift / Iris Bridge / Hanging Platform

13. `AwarenessObjectTransitions_v1.png`

- `8열 × 4행`
- Hush Shrine / Awareness Mark / Mirror Door / Hidden Inner Eye

14. `GazeArenaTransitions_v1.png`

- `8열 × 3행`
- Arena Lock / Rotating Cover / Fracture Exit

### 아이템·체크포인트

15. `GazeCollectibleTransitions_v1.png`

- `8열 × 4행`
- Currency / Healing / Memory Fragment / Awareness Fragment

16. `GazeCheckpointTransitions_v1.png`

- `8열 × 3행`
- Activate / Heal Pulse / Respawn Release

### 환경·VFX

17. `GazeAmbientMotion_v1.png`

- `8열 × 4행`
- Hanging Cage / Audience Cloth / Teal Fog / Background Eyes

18. `GazeSecondaryVFX_v1.png`

- `8열 × 4행`
- Detection Warning / Gaze Hit / Guard Break / Boss Truth Reveal

## 저장 위치

- 적: `HiddenWeight/Assets/Art/Gaze/Gameplay/Enemies/Animation/`
- 보스: `HiddenWeight/Assets/Art/Gaze/Gameplay/Bosses/Animation/`
- 아이템: `HiddenWeight/Assets/Art/Gaze/Gameplay/Items/Animation/`
- 게임플레이 VFX: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/`
- 위험물: `HiddenWeight/Assets/Art/Gaze/Environment/Hazards/Animation/`
- 장치: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/`
- 환경 루프: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/`

## 투명화

- 기존 응시 자산에 없는 단색 `#00ff00` 배경으로 생성한다.
- 생성 원본을 로컬 크로마키 제거 도구로 RGBA PNG로 변환한다.
- 청록·보라의 반투명 발광 가장자리가 사라지지 않도록 soft matte와 despill을 사용한다.

## 검수 기준

- 18개 파일이 모두 존재한다.
- 각 파일이 명세의 행·열로 정확히 등분된다.
- 네 모서리 알파가 0이다.
- 적과 보스의 같은 행에서 체적·눈 개수·재질·접지점이 유지된다.
- 예고 프레임이 유효 공격보다 먼저 보인다.
- 시선 위험은 안전, 경고, 발각 상태가 색과 눈꺼풀 형태로 구분된다.
- 루프 행은 첫 프레임과 마지막 프레임 사이에 큰 위치 점프가 없다.
- 응시 배경 위에서 캐릭터와 위험물 실루엣이 묻히지 않는다.

