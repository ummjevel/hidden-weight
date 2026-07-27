# 잔재 애니메이션 생성 기록

이 폴더의 `*_Source.png`는 생성 원본이고, 게임 적용본은
`HiddenWeight/Assets/Art/Residue/Gameplay` 아래의 투명 PNG다.

## 공통 방향

- 어두운 코즈믹 호러 메트로배니아, 잔재 지역의 회갈색·검은 철골·낮은 남보라색
- 균일한 크로마키 녹색 배경에서 생성한 뒤 알파 PNG로 변환
- 정면 프레젠테이션 보드가 아닌, 고정 격자에 배치된 연속 동작 프레임
- 프레임마다 실루엣, 크기, 발 위치와 카메라 각도를 유지
- 텍스트, 라벨, UI, 배경, 그림자를 제외

## 생성 시트

- Player Locomotion: 8×3, Idle / Walk / Run
- Player Aerial: 6×4, Jump / AirMove / Fall / Land
- Player Actions: 6×2, Attack / Dash
- Player Wall: 6×2, WallCling / WallJump
- 일반 적 4종: 각 4×4, Idle / Move / Attack / Hit·Death
- Wrist Watcher: 6×4 전투 시트와 6×3 반응 시트
- Combat VFX: Hit / Block / Enemy Death / Boss Impact
- Emotion VFX: Rewind Channel / Rewind Complete / Awareness Pulse
- Pickup VFX: Currency / Healing / Memory
- Player VFX: Hit / Death / Respawn
- Memory Instructor VFX: Chain Slam / Core Pulse / Phase Rupture

`EmotionVFX_Discarded_v1.png`는 격자 구성이 불안정한 폐기 원본이며 게임에 넣지 않는다.
실제 적용은 `EmotionVFX_Grid_Source.png`에서 만든 `EmotionVFX_v2.png`를 사용한다.
