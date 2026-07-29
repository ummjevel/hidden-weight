# 응시 지역 룸 레이어 생성 기록

설계:
`docs/superpowers/specs/2026-07-28-gaze-room-layers-design.md`

실행 계획:
`docs/superpowers/plans/2026-07-28-gaze-room-layers.md`

## 공통 규칙

- 내장 이미지 생성 도구로 룸별 `BG_Far`, `BG_Mid`, `FG_Overlay`를 각각 제작한다.
- 룸 콘셉트가 구도 기준이며 `03-gaze-sky-observer-v2-clean-master.png`가 색·세계관 기준이다.
- `BG_Far`는 불투명 원경, `BG_Mid`와 `FG_Overlay`는 크로마키 제거 후 투명 RGBA다.
- 실제 발판, 기물, 활성 눈, 문, 캐릭터, 적, 아이템, UI, 글자를 포함하지 않는다.
- 모든 적용본은 1672×941로 정규화한다.

## 룸별 생성 방향

| 폴더 | 원본 콘셉트 | 핵심 레이어 방향 |
|---|---|---|
| `Room01` | `01-entry-threshold.png` | 입구 도시와 눈꺼풀형 터널 프레임 |
| `Room02` | `02-exposed-plaza.png` | 노출 광장, 끊어진 원경 교량과 측면 교각 |
| `Room03` | `03-lower-prison-district.png` | 하층 감옥 허브와 관람 건축 |
| `Room04` | `04-hushed-crevice.png` | 좁은 도시 틈, 낮은 천장과 압박 벽 |
| `Room05` | `05-fixed-gaze-hall.png` | 고정 응시 제단과 비활성 눈 건축 |
| `Room06` | `06-rotating-gaze-arcade.png` | 회전 홍채 회랑과 교차 교량 |
| `Room07` | `07-hanging-cage-lift.png` | 수직 감옥 도시와 케이지 승강축 |
| `Room08` | `08-upper-cage-transit.png` | 상층 운송교와 승강탑 |
| `Room09` | `09-faceless-audience-gallery.png` | 곡선 관람석과 극장형 프레임 |
| `Room10` | `10-optic-nerve-viaduct.png` | 거대 눈, 시신경 고가교와 감시탑 |
| `Room11` | `11-great-ocular-cathedral.png` | 파손된 대성당과 눈형 장미창 |
| `Room12` | `12-pupil-sanctum.png` | 동공 정렬 성소와 원형 극장 |
| `Secret01` | `S1-flooded-observation-cell.png` | 침수 관측실과 왜곡 반사 |
| `Secret02` | `S2-hidden-cage-archive.png` | 숨은 케이지 기록고와 승강축 |
| `Secret03` | `S3-blind-chamber.png` | 닫힌 눈 회랑과 천장 틈의 외곽광 |

각 룸의 공통 생성 프롬프트는 다음 구조로 사용했다.

- `BG_Far`: 원본 콘셉트의 구도·색·랜드마크를 유지한 불투명 원거리 풍경. 하단 중앙의
  플레이 영역은 저대비로 유지하고 전경·발판·상호작용 요소를 제외한다.
- `BG_Mid`: 룸별 건축 실루엣, 비활성 교량, 감옥·관람석·성당 구조만 분리한다. 모든
  요소를 단색 `#00FF00` 위에 생성한 뒤 알파로 변환한다.
- `FG_Overlay`: 화면 가장자리에 붙는 기둥, 아치, 쇠창살, 짧은 사슬, 모서리 잔해만
  생성하며 중앙 약 65%를 개방한다. 같은 방식으로 알파 변환한다.

## 결과

- 런타임 PNG: `HiddenWeight/Assets/Art/Gaze/Room01`~`Room12`,
  `HiddenWeight/Assets/Art/Gaze/Secret01`~`Secret03`
- 생성 원본: 현재 폴더의 `*_Source.png`
- 룸 합성본: `composites/`
- 검토 연락판: `contact-sheets/gaze-rooms-15-composite-contact-sheet.jpg`,
  `contact-sheets/gaze-rooms-45-layer-contact-sheet.jpg`
- 최종 수량: 15개 룸 × 3개 레이어 = 45장
- 적용본 규격: 전부 1672×941
- 투명도: `BG_Far` 불투명, `BG_Mid`와 `FG_Overlay` RGBA
