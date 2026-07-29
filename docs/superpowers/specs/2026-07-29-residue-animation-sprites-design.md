# 잔재 지역 자연스러운 움직임용 스프라이트 설계

## 목표

잔재 지역의 기존 디자인과 색조를 유지하면서, 실제 플레이 중 끊겨 보이는 적·보스·위험물·장치·아이템·환경 효과의 중간 프레임과 누락 상태를 보강한다.

플레이어 스프라이트는 이미 별도 치비 플레이어 세트로 교체되었으므로 이번 범위에서 제외한다.

## 제작 원칙

- 기존 잔재 지역의 검은 철골, 회갈색 석재, 앰버 발광, 낮은 남보라 하이라이트를 유지한다.
- 기존 파일을 덮어쓰지 않고 `_v2` 또는 신규 역할 이름으로 저장한다.
- 좌우 이동은 별도 시트를 만들지 않고 Unity `SpriteRenderer.flipX`로 처리한다.
- 모든 프레임은 동일한 셀 크기와 기준점을 사용한다.
- 적과 보스의 발 또는 접지점은 `Bottom Center`, 중심 확산 VFX는 `Center`를 기준으로 한다.
- 캐릭터 이동은 스프라이트 내부 이동이 아니라 Rigidbody2D가 담당한다.
- 공격은 `예고 → 유효 → 회수`가 실루엣만으로 구분되어야 한다.
- 투명 PNG가 최종 포맷이며 충돌·피해 판정은 이미지와 분리한다.

## 제작 방식

전체 재생성 대신 기존 디자인을 유지하는 확장 방식을 사용한다.

- 4프레임 일반 적은 8프레임으로 확장한다.
- 기존 6프레임 보스 시트는 8프레임 구조로 보강한다.
- 이미 8프레임인 환경·아이템·장치는 유지하고, 누락된 전환과 반응 시트만 추가한다.
- 기억의 교수자는 기존 분리 파츠를 유지하고, 회전·사슬·핵 효과와 공격 전환만 프레임 시트로 제작한다.

## 산출물

### 일반 적

각 파일은 `8열 × 6행`이며 행 순서는 동일하다.

1. `ResidueWalker_v2.png`
2. `HangingFinger_v2.png`
3. `MourningCarrier_v2.png`
4. `HardenedResidue_v2.png`

행:

1. Idle
2. Move
3. Telegraph
4. Attack
5. Hit
6. Death

### 손목 감시자

5. `WristWatcher_Combat_v2.png`

- `8열 × 7행`
- Idle / Sweep / Charge / Impact-Stun / Drop / Hurt / Death

6. `WristWatcher_Transitions_v1.png`

- `8열 × 3행`
- Entrance / Phase Transition / Arena Device Interaction

### 기억의 교수자

7. `MemoryInstructor_Attacks_v1.png`

- `8열 × 4행`
- Blade Sweep / Hook Pull / Chain Slam / Recover

8. `MemoryInstructor_Reactions_v1.png`

- `8열 × 3행`
- Hurt / Phase Rupture / Death

9. `MemoryInstructor_CoreHalo_v1.png`

- `8열 × 3행`
- Halo Rotation / Core Pulse / Overload

### 위험물·장치

10. `HazardTransitions_v1.png`

- `8열 × 5행`
- Spike / Abyss Tendril / Crusher / Collapse Floor / Falling Debris
- 각 행은 대기, 예고, 작동, 유지, 회복을 한 사이클로 표현한다.

11. `RewindObjectTransitions_v1.png`

- `8열 × 4행`
- Platform / Chain Bridge / Lift / Pulley
- 파손, 채널링, 역재생 복원, 완료가 구분되어야 한다.

12. `BossArenaTransitions_v1.png`

- `8열 × 3행`
- Arena Lock / Safe Platform Restore / Seal Rupture

### 아이템·체크포인트

13. `CollectibleTransitions_v1.png`

- `8열 × 5행`
- Currency / Healing / Maximum Health / Memory Fragment / Rewind Core
- 대기 루프가 아니라 획득·흡수·소멸 전환을 표현한다.

14. `CheckpointTransitions_v1.png`

- `8열 × 3행`
- Activate / Heal Pulse / Respawn Release

### 환경·VFX

15. `AmbientMotion_v1.png`

- `8열 × 4행`
- Hanging Chain / Torn Cloth / Falling Ash / Indigo Fog

16. `SecondaryGameplayVFX_v1.png`

- `8열 × 4행`
- Heavy Hit / Guard Break / Enemy Dissolve / Boss Phase Burst

## 생성 및 투명화

- 각 시트는 기존 관련 시트를 시각 참조로 사용한다.
- 생성 단계에서는 피사체에 사용되지 않는 단색 크로마키 배경을 사용한다.
- 로컬 후처리로 크로마키를 제거하고 RGBA PNG로 변환한다.
- 프레임 셀 사이에는 충분한 여백을 두고 프레임끼리 닿지 않게 한다.
- 텍스트, 번호, 셀 선, 워터마크는 넣지 않는다.

## 검수 기준

- 프레임 수와 격자가 명세와 일치한다.
- 네 모서리 알파가 0이고 피사체 내부가 과도하게 투명하지 않다.
- 같은 행에서 체적과 재질이 유지된다.
- 접지 동작에서 기준점이 튀지 않는다.
- 루프의 첫 프레임과 마지막 프레임이 자연스럽게 이어진다.
- 공격 예고가 실제 공격보다 최소 2프레임 먼저 보인다.
- Hit는 짧고 Death는 되돌아오지 않는 실루엣 변화가 있다.
- 기존 잔재 배경 위에서 앰버 강조가 플레이어보다 밝아지지 않는다.

## 저장 위치

- 적: `HiddenWeight/Assets/Art/Residue/Gameplay/Enemies/Animation/`
- 보스: `HiddenWeight/Assets/Art/Residue/Gameplay/Bosses/Animation/`
- 위험물: `HiddenWeight/Assets/Art/Residue/Environment/Hazards/Animation/`
- 장치: `HiddenWeight/Assets/Art/Residue/Environment/Interactables/Animation/`
- 아이템: `HiddenWeight/Assets/Art/Residue/Gameplay/Items/Animation/`
- 환경: `HiddenWeight/Assets/Art/Residue/Environment/Props/Animation/`
- VFX: `HiddenWeight/Assets/Art/Residue/Gameplay/VFX/`

