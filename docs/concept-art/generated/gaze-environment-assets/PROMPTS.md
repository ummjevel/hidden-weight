# 응시 지역 환경 이미지 생성 기록

설계 문서:
`docs/superpowers/specs/2026-07-28-gaze-environment-art-design.md`

실행 계획:
`docs/superpowers/plans/2026-07-28-gaze-environment-art.md`

## 공통 조건

- 내장 이미지 생성 도구를 사용한다.
- `03-gaze-sky-observer-v2-clean-master.png`와 응시 지역 룸 15장을 시각 기준으로 사용한다.
- 먹색 철골, 푸른 흑색 젖은 석재, 저채도 보라, 제한적인 냉청록을 유지한다.
- 각 원본은 정확한 격자와 균일한 `#00FF00` 크로마키 배경으로 생성한다.
- 그림자, 바닥면, 풍경, 캐릭터, 적, 텍스트, 라벨, UI, 워터마크를 넣지 않는다.
- 크로마키 제거 후 Unity 적용본은 투명 RGBA PNG로 저장한다.

## 최종 생성 내역

각 최종 프롬프트의 전문은
`docs/superpowers/plans/2026-07-28-gaze-environment-art.md`의 동일한 Task에 기록했다.
실제 생성 시에도 해당 프롬프트의 구성·격자·금지 조건을 그대로 사용했다.

| 아틀라스 | 격자 | 프롬프트 핵심 | 생성 원본 | Unity 적용본 |
| --- | --- | --- | --- | --- |
| 지형 | 6×4 | 바닥·모서리·벽·경사·파손 끝 24셀 | `Gaze_TerrainTiles_v1_Source.png` | `Terrain/Gaze_TerrainTiles_v1.png` |
| 발판 | 6×3 | 석재·철골·안구교·현수·케이지 발판 18셀 | `Gaze_Platforms_v1_Source.png` | `Terrain/Gaze_Platforms_v1.png` |
| 시선 위험 | 6×4 | 고정·회전·천장·바닥·보스 눈의 4상태 | `Gaze_EyeHazards_v1_Source.png` | `Hazards/Gaze_EyeHazards_v1.png` |
| 엄폐 | 6×3 | 기둥·눈꺼풀막·철창·이동판·조각상·잔해 | `Gaze_CoverObjects_v1_Source.png` | `Interactables/Gaze_CoverObjects_v1.png` |
| 승강·수송 | 6×3 | 빈 케이지·승강기·현수 발판·레일·도르래 | `Gaze_TransitStructures_v1_Source.png` | `Interactables/Gaze_TransitStructures_v1.png` |
| 문·숏컷 | 6×4 | 사슬막·게이트·홍채문·거울문·동공문의 4상태 | `Gaze_DoorsShortcuts_v1_Source.png` | `Interactables/Gaze_DoorsShortcuts_v1.png` |
| 환경 장식 | 6×4 | 새장·사슬·조각상·거울·등·잔해·데칼 24종 | `Gaze_EnvironmentProps_v1_Source.png` | `Props/Gaze_EnvironmentProps_v1.png` |
| 능력 오브젝트 | 6×4 | 체크포인트·숨죽임·자각·기억·비밀 표식의 4상태 | `Gaze_AbilityObjects_v1_Source.png` | `Interactables/Gaze_AbilityObjects_v1.png` |
| 환경 VFX | 8×3 | 안개·시선 먼지·역방향 그림자 8프레임 루프 | `Gaze_AmbientVFX_v1_Source.png` | `VFX/Gaze_AmbientVFX_v1.png` |

승강·수송 원본의 첫 생성에는 케이지 내부에 사람 형태가 포함되어, 후속 편집 프롬프트로
모든 사람 형태를 제거하고 완전히 빈 케이지로 교정했다. 6×3과 8×3 원본은 생성 도구의
기본 캔버스 여백을 행 단위로 정규화해 각각 정사각형 256px 및 192px 셀로 만들었다.

## 크로마 제거

정적 기물은 `remove_chroma_key.py`의 border 자동 키, soft matte, despill,
투명 임계값 12, 불투명 임계값 220을 사용했다. 환경 VFX에는 반투명 가장자리를 보존하기
위해 `edge-feather 0.25`를 추가했다.
