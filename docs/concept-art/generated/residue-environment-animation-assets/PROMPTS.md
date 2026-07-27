# 잔재 환경 애니메이션 이미지 생성 기록

기존 잔재 환경 리소스의 검은 철, 낡은 청동, 뼈 같은 구조, 절제된 호박색 광원과 남색 그림자를 유지하면서 게임용 2D 애니메이션 시트로 확장했다.

## 공통 조건

- 횡스크롤 게임용 정측면 오브젝트
- 한 행당 8개의 순차 프레임
- 동일 행의 위치·크기·조명·오브젝트 정체성 유지
- 크로마 그린 원본에서 배경을 제거해 최종 RGBA PNG 제작
- 텍스트, 테두리, 격자선, 캐릭터, 배경 장면 제외

## 제작 항목

1. `RewindPlatforms_Animation_Source.png`
   - 소형 발판, 중형 발판, 사슬 다리가 잔해 상태에서 완전한 형태로 되감기는 3행.
2. `RewindMechanisms_Animation_Source.png`
   - 문, 승강기, 도르래가 파손 상태에서 복원되는 3행.
3. `Hazards_Animation_Source.png`
   - 바닥 가시 전개, 심연 촉수 공격·수축, 압착기 개폐의 3행.
4. `CollapseHazards_Animation_Source.png`
   - 석조 바닥 붕괴와 매달린 추 낙하·충돌의 2행.
5. `AmbientProps_Animation_Source.png`
   - 찢어진 장례 천, 빈 새장, 호박색 성유물 등불의 반복 동작 3행.
6. `AmbientVFX_Animation_Source.png` 및 효과별 보정 원본
   - 재·먼지, 낮게 흐르는 남색 안개, 희미한 불씨와 돌 파편의 반복 동작 3행.

최종 사용 파일과 프레임 규격은 `HiddenWeight/Assets/Art/Residue/Environment/ANIMATION_README.md`를 따른다.
