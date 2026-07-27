# Residue Gameplay Asset Prompt Log

모든 이미지는 내장 이미지 생성 도구를 사용했으며, `#00ff00` 단색 배경으로 생성한 뒤
로컬 크로마키 제거를 거쳐 투명 PNG로 저장했다.

공통 스타일 기준:

- `residue-full-region-map-v2.png`: 전체 분위기, 팔레트, 크기
- `Residue_TerrainAtlas.png`: 검은 철골·석재·사슬 재질
- `Residue_InteractablesAtlas.png`: 고딕 구조, 앰버 발광, 소품 렌더링
- `character_sprite_ref.png`: 플레이어 정체성, 복장, 비율, 동작

## 일반 적

검은 철골과 압축된 재로 이루어진 잔재 보행자, 천장에 거꾸로 매달리는 손가락형 매복 적,
관 같은 등짐을 진 돌진 적, 방패 팔을 가진 굳은 정예를 2×2 정면 분리 시트로 생성한다.
각 실루엣은 서로 겹치지 않고, 어두운 배경에서 읽히는 앰버 점광과 낮은 남보라 강조만 쓴다.

## 아이템과 위험 요소

일반 재화, 회복 성물, 최대 체력 조각, 기억 파편, 가시, 심연 경고 구조, 정상·파손 발판,
파손 도르래를 3×3 분리 시트로 생성한다. 기존 아틀라스와 동일한 석재·철골·사슬 재질을
유지하고 일반 아이템과 영구 보상의 크기·발광 강도를 구분한다.

## 손목의 감시자

감시탑과 손목 관절이 융합된 중간 보스를 동일한 비율로 유지하며 Idle, Sweep Anticipation,
Charge Anticipation, Charge Impact, Drop Attack, Hurt의 6포즈로 생성한다.

## 기억의 교수자

교수대, 갈비뼈, 사슬이 융합된 지역 보스를 조립형으로 만든다. Torso, Caged Head,
Lower Root, Blade Arm, Hook Arm, Gallows Halo, Short Chain, Hooked Chain,
Safety Platform의 9개 파츠와 원형 연결 소켓을 포함한다.

## 숏컷

Broken/Restored Chain Bridge, Dormant/Active Lift, Broken/Restored Pulley의 상태 쌍을
동일한 크기와 구조로 유지하며 3×2 시트로 생성한다.

## 플레이어

`character_sprite_ref.png`의 흰 머리, 작은 검은 얼굴, 짙은 보라 드레스와 비율을 유지한다.
잔재 배경에 맞게 드레스 명도를 조금 낮추되 흰 머리와 라벤더 림라이트의 가독성은 보존한다.
Idle, Walk, Run, Jump, Fall, Land, Attack, Dash의 8개 핵심 포즈를 4×2로 생성한다.
