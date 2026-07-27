# 잔재 환경 애니메이션 PNG 시트

이 폴더의 애니메이션 리소스는 모두 투명 배경 RGBA PNG 스프라이트 시트다.  
PNG 자체가 움직이는 형식은 아니며, 각 행을 왼쪽에서 오른쪽으로 8프레임 재생한다.

## 시트 구성

| 파일 | 격자 / 셀 크기 | 행 구성 | 권장 재생 |
|---|---:|---|---|
| `Interactables/Animation/RewindPlatforms_Animation_v1.png` | 8×3 / 256×256 | 소형 발판 복원, 중형 발판 복원, 사슬 다리 복원 | 12~14 FPS, 1회 |
| `Interactables/Animation/RewindMechanisms_Animation_v1.png` | 8×3 / 222×296 | 문 복원, 승강기 복원, 도르래 복원 | 12~14 FPS, 1회 |
| `Hazards/Animation/Hazards_Animation_v2.png` | 8×3 / 256×256 | 가시 전개, 촉수 공격, 압착기 작동 | 12~16 FPS |
| `Hazards/Animation/CollapseHazards_Animation_v1.png` | 8×2 / 222×444 | 바닥 붕괴, 낙하 추 충돌 | 12~14 FPS, 1회 |
| `Props/Animation/AmbientProps_Animation_v1.png` | 8×3 / 256×256 | 장례 천 흔들림, 빈 새장 흔들림, 등불 맥동 | 8~10 FPS, 반복 |
| `VFX/Animation/AmbientVFX_Animation_v2.png` | 8×3 / 256×256 | 재·먼지, 남색 지면 안개, 불씨·파편 | 행별 8 / 6~8 / 10~12 FPS, 반복 |

## 사용 기준

- 한 행 안에서는 모든 프레임의 피벗을 같은 위치로 고정한다.
- 복원·붕괴 계열은 첫 프레임과 마지막 프레임에서 멈출 수 있게 사용한다.
- 천·새장·등불·환경 효과는 마지막 프레임 다음에 첫 프레임을 이어 반복한다.
- 실제 충돌 판정은 그림의 변화와 분리한다. 가시·압착기·붕괴 바닥은 판정 전환 시점을 별도로 지정한다.
- 바닥 타일, 벽, 기둥처럼 움직이지 않는 요소는 기존 정적 PNG를 사용한다.

총 프레임 수는 136장이다.
