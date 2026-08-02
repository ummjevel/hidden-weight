# 잔재 모듈형 환경 V3 생성 기록

2026-08-02 생성. `docs/mood_img.png`의 첫 번째 행과 잔재 최종 컨셉
`docs/concept-art/02-residue-cloud-face-v6.png`를 기준으로 삼았다.

## 적용 원칙

- 남색·청회색·회갈색 저채도 석재를 기본으로 사용한다.
- 검은 철골, 사슬, 매우 적은 탁한 황동빛만 보조로 사용한다.
- 플레이 가능한 윗면은 노란 선 대신 차가운 청회색 석재 테두리로 읽힌다.
- 긴 바닥과 높은 벽은 한 이미지를 늘이지 않고 원래 종횡비를 유지한 모듈을 반복한다.
- 몬스터, 충돌 지형, 잔재 외 지역의 아트는 이 세트의 적용 대상이 아니다.

## 생성 파일

| 파일 | 원본 크기 | 용도 |
| --- | ---: | --- |
| `Residue_ModularTerrain_v3_Source.png` | 1536×1024 | 바닥 캡·중간·끝, 독립 발판 3종, 절벽·채움 모듈 |
| `Residue_ModularWallsStairs_v3_Source.png` | 1254×1254 | 세로 벽 3종, 양방향 계단, 모서리, 벽타기 기둥 |
| `Residue_Background_Bridge_v3.png` | 1536×1024 | R01~R07, S1~S2 하층·다리 원경 |
| `Residue_Background_Shaft_v3.png` | 1536×1024 | R08~R09 수직 승강축 원경 |
| `Residue_Background_BellTower_v3.png` | 1536×1024 | R10~R12, S3 종탑·상층 원경 |

Unity 적용본은 아래에 있다.

- `HiddenWeight/Assets/Art/Residue/Environment/Terrain/ModularV3`
- `HiddenWeight/Assets/Art/Residue/Backgrounds/V3`

## 공통 프롬프트 요약

```text
Original 2D side-view dark metroidvania environment for the Residue zone.
Use only the top-row Residue mood: desaturated navy, blue-gray and taupe-gray.
Ruined slate blocks, restrained black wrought iron ribs, sparse chains and extremely
sparse muted brass joints. Preserve clear gameplay silhouettes at small scale.
Walkable surfaces use a continuous cool blue-gray stone rim and natural cold edge
light. No yellow safety stripe, neon outline, painted guide, debug marker, character,
enemy, UI, text, logo or watermark. Painterly production game art, not pixel art,
not photorealistic and not isometric.
```

투명 전경 시트는 균일한 녹색 배경으로 생성한 뒤 imagegen 스킬의
`remove_chroma_key.py`로 알파 PNG로 변환했다. Unity 전용 슬라이서는 각 셀의 실제 알파
경계를 다시 계산하여 투명 여백 때문에 반복 조각 사이가 벌어지지 않게 한다.

