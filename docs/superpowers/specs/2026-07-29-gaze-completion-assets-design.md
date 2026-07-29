# 응시 이미지 제작 100% 보완 세트 설계

## 목표

기존 응시 지역의 18종 애니메이션 시트와 12개 메인 방·3개 비밀방 배경은 유지하고, 실제 플레이 마감에 부족한 신규 이미지 9종을 추가한다. Unity 연결은 제외하며 이 세트의 생성·투명화·격자 검사가 끝나면 2맵 `응시`의 이미지 제작을 100%로 판정한다.

## 시각 언어

- 기본색: 저채도 보라, 먹색, 검은 철, 제한적인 청록
- 기만·위험: 마젠타와 보라색 홍채
- 진실·해금·인지: 청록색 단일 눈과 얇은 균열
- 형태: 눈꺼풀, 홍채, 가면, 철창, 고딕 극장, 매달린 관객
- 전경·배경은 플레이어와 공격 판정보다 낮은 명도를 사용한다.
- 기존 PNG는 덮어쓰지 않고 신규 파일만 추가한다.

## 신규 파일 9종

### `GazeEnemyProjectiles_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/`
- 격자: 8×4
- 행: 순례자의 소리 파문 / 알리는 입의 비명탄 / 매달린 관객의 시선 그림자 / 얼굴 없는 재판관의 판결 참격

### `GazeBossProjectiles_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/`
- 격자: 8×5
- 행: 홍채 문지기 스캔 빔 / 눈꺼풀 파편비 / 철창 사슬 채찍 / 만인의 시선 가짜 눈탄 / 진짜 청록 눈의 폭로 타격

### `GazePlatformStates_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Environment/Terrain/Animation/`
- 격자: 8×4
- 행: 안정 발판의 감시 반응 / 시선에 의한 해체 / 해체된 상태 / 진실 시야로 재현

### `GazeImpactVFX_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Gameplay/VFX/Animation/`
- 격자: 8×4
- 행: 기본 타격 / 시선 빔 화상 / 착지 먼지 / 가면·눈꺼풀 방어 파괴

### `GazeForegroundMotion_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/`
- 격자: 8×4
- 행: 가까운 사슬·케이지 / 찢어진 극장 커튼 / 화면 가장자리 관객 가면 / 낮은 보라 안개와 작은 눈

### `GazeBackgroundMotion_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Environment/VFX/Animation/`
- 격자: 8×4
- 행: 하늘의 거대 홍채 회전 / 창문 눈 깜빡임 / 먼 매달린 케이지 흔들림 / 관객 군집이 동시에 고개 돌림

### `GazeRoomTransitions_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/Environment/Interactables/Animation/`
- 격자: 8×4
- 행: 홍채 문 봉쇄 / 봉쇄 해제 / 케이지·다리 지름길 / 거울·커튼 비밀 통로

### `GazeUIIcons_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/UI/`
- 격자: 8×4, 정적 아이콘 32개
- 1행: 입구 / 출구 / 상행 / 하행 / 문 / 잠긴 문 / 지름길 / 비밀 통로
- 2행: 체크포인트 / 회복 / 화폐 / 기억 조각 / 인지 조각 / 진실 시야 / 시선 위험 / 은폐물
- 3행: 눈먼 순례자 / 알리는 입 / 매달린 관객 / 얼굴 없는 재판관 / 중간 보스 / 지역 보스 / NPC / 기록물
- 4행: 미발견 / 발견 / 완료 / 재방문 / 현재 위치 / 목표 / 보스 격파 / 지역 완료

### `GazeStatusUI_v1.png`

- 위치: `HiddenWeight/Assets/Art/Gaze/UI/Animation/`
- 격자: 8×3
- 행: 인지·진실 시야 충전과 사용 / 발각·주시 누적과 해제 / 기억 획득·보스 경고·지역 완료

## 제작 규칙

- 9개 파일은 각각 별도의 ImageGen 호출로 생성한다.
- 균일한 `#00ff00` 배경에서 생성한 뒤 soft matte와 despill로 제거한다.
- 모든 셀은 192×192로 정규화한다.
- 공격체·VFX·UI는 Center, 발판·문은 Bottom Center 피벗을 전제로 한다.
- 텍스트, 숫자, 워터마크, 셀 선, 완성 배경 장면, 캐릭터 본체는 넣지 않는다.

## 검수

- 신규 파일 9개가 모두 존재한다.
- 모든 파일이 투명 RGBA PNG이며 네 모서리 알파가 0이다.
- 명세의 행·열로 정확히 등분된다.
- 공격체는 예고·유효·소멸이 구분된다.
- 배경·전경 루프의 첫 프레임과 마지막 프레임 사이에 큰 위치 점프가 없다.
- UI 아이콘은 32~64픽셀 축소 상태에서도 구분된다.
- 기존 응시 자산의 보라·청록·먹색과 충돌하지 않는다.
