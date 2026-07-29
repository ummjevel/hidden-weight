# Hidden Weight UI 비주얼 에셋 제작 목록

## 2026-07-29 연결 상태

| 묶음 | 상태 | 다음 제작/연결 |
|---|---|---|
| Residue 지도·상태 시트 | 런타임 연결 완료 | 축소 가독성 최종 QA |
| Gaze 지도·상태 시트 | 런타임 연결 완료 | 노출 문양의 저섬광 대표 프레임 QA |
| Fracture 룸 배경 15장 | 1024px 평면 합성 연결 | 전경/중경/배경 3레이어 분리 제작 |
| Fracture 지도·상태 UI | 미제작 | 8×4 지도 아이콘, 8×3 예지·불안·진행 시트 |
| 공통 패널·버튼·스크롤 | 코드 도형 사용 중 | 9-slice 패널, 버튼 4상태, 탭, 스크롤 핸들 |
| 글꼴 | 기본 런타임 폰트 사용 중 | 한국어 본문 1종과 제목용 1종, OFL/상업 사용 라이선스 확인 |
| 입력 글리프 | 텍스트 기반 | 키보드·Xbox·PlayStation 계열 단색 글리프 |
| 기억 기록 | 코드 카드 사용 중 | 기억 카드 9-slice, 연결 실, 지역 탭, 새 기록 점 |

Fracture 전용 에셋을 생성할 때는 밝은 민트·라벤더·옅은 살구를 주조색으로 하되, 안전/위험을 색만으로 구분하지 않는다. 예지 상태는 흰 윤곽과 어긋난 이중 형태, 불안 상태는 끊어진 외곽선, 진행 상태는 닫히는 균열 문양을 함께 사용한다. 네온 발광, cyan-on-dark 패널, 장난감 같은 치비 장식은 사용하지 않는다.

> 기준일: 2026-07-28
> 연결 문서: `UI_UX_DESIGN.md`
> 목적: UI 기능 구현 뒤 수작업·벡터 제작·이미지 생성에 공통으로 사용할 에셋 명세

---

## 1. 제작 원칙

- UI-0~UI-4는 단순 도형과 임시 아이콘으로 기능을 검증하고, 최종 에셋은 UI-5에서 교체한다.
- 아이콘은 64px에서도 실루엣만으로 구분되어야 하며 색에 의존하지 않는다.
- 메뉴 장식은 래스터 이미지보다 9-slice 또는 벡터/SDF를 우선한다.
- 생성형 이미지로 만든 초안은 그대로 사용하지 않고 투명 배경 정리, 대칭 교정, 선 두께 통일을 거친다.
- 모든 래스터 에셋은 원본의 2배 크기로 제작한 뒤 Unity에서 축소 사용한다.
- 파일명은 `UI_{Category}_{Name}_{State}` 형식을 사용한다.

### 공통 납품 조건

| 항목 | 기준 |
| --- | --- |
| 색 공간 | sRGB |
| 투명 이미지 | PNG-24 + alpha |
| 아이콘 원본 | SVG 권장, PNG 사용 시 256×256 |
| 패널 원본 | 1024×1024 또는 9-slice 최소 256×256 |
| 필터 | UI는 Bilinear, 픽셀 아트 채택 시 Point로 전체 통일 |
| 여백 | 아이콘 외곽 12.5% 안전 여백 |
| 피벗 | 중앙, 방향성 포인터만 의미에 맞춰 별도 지정 |
| 상태 변형 | 색조 변경만 하지 말고 윤곽·채움·균열 등 형태도 변경 |

---

## 2. P0 — UI-0 메뉴 기반

UI-0 기능 구현에는 최종 에셋이 필요하지 않다. 아래 에셋은 UI-5 교체용이다.

| ID | 에셋 | 수량/상태 | 권장 형식 | 사용처 |
| --- | --- | ---: | --- | --- |
| UI_Panel_Primary | 기본 메뉴 패널 | 1, 9-slice | SVG/PNG | 타이틀·일시정지·설정 |
| UI_Panel_Modal | 확인창 패널 | 1, 9-slice | SVG/PNG | 파괴적 행동 확인 |
| UI_Button_Primary | 기본 버튼 | Idle/Selected/Pressed/Disabled | 9-slice | 모든 메뉴 |
| UI_Focus_Merge | 선택 이중 윤곽 | 6~8프레임 또는 분리 레이어 | SVG/PNG | 키보드·패드 포커스 |
| UI_Divider_Thin | 얇은 구분선 | 1 | SVG | 메뉴 탭·정보 그룹 |
| UI_Logo_Main | Hidden Weight 로고 | 한글 부제 포함/미포함 2종 | SVG/PNG | 타이틀·마케팅 |
| UI_Icon_Warning | 주의 문양 | 1 | SVG | 확인창·저장 실패 |
| UI_Icon_Close | 닫기 | 1 | SVG | 모달·탭 |

### 메뉴 패널 생성 참고

```text
얇은 먹색 반투명 패널, 완전 대칭이 아닌 미세하게 어긋난 이중 윤곽,
원과 작은 균열 모티브, 장식은 모서리 두 곳에만 제한,
고딕 판타지 문양이나 금속 프레임 없이 몽환적이고 절제된 2D 게임 UI,
중앙은 텍스트를 위해 비어 있음, 투명 배경, 정면, 조명 효과 없음
```

피해야 할 요소: 과도한 금장, 돌 프레임, 사실적인 재질, 복잡한 룬 문자, 메뉴 내용을 침범하는 장식.

---

## 3. P1 — 플레이 HUD

### 3.1 체력

| ID | 에셋 | 상태 | 규격 | 형태 규칙 |
| --- | --- | --- | --- | --- |
| UI_Health_Core | 체력 핵 | Full | 128×128 | 안정된 원, 내부에 작은 씨앗 |
| UI_Health_Core | 체력 핵 | Empty | 128×128 | 같은 원의 금 간 외곽선 |
| UI_Health_Core | 체력 핵 | Low | 128×128 | 안쪽으로 찌그러진 원 |
| UI_Health_Fracture | 피해 파편 | 6~8조각 | 64×64 | 바깥으로 흩어지는 얇은 조각 |
| UI_Health_Heal | 회복 링 | 6~8프레임 | 256×256 | 안쪽으로 닫히는 부드러운 원 |

### 3.2 감정 문양

| ID | 감정 | 규격 | 핵심 실루엣 | 필수 상태 |
| --- | --- | --- | --- | --- |
| UI_Emotion_Rewind | 되감기 | 256×256 | 끊어진 원과 반시계 궤적 | Locked/Ready/Active/Cooldown |
| UI_Emotion_Hush | 숨죽이기 | 256×256 | 닫히는 눈꺼풀과 수축 원 | Locked/Ready/Active |
| UI_Emotion_Foresight | 예지 | 256×256 | 앞쪽으로 갈라지는 이중 윤곽 | Locked/Ready/Active/Cooldown |
| UI_Emotion_Awareness | 자각 | 256×256 | 어긋난 두 원이 정렬되는 형태 | Locked/Ready/Active/Unstable |

각 문양은 단색 마스크로도 읽혀야 한다. 지역색은 Unity 머티리얼 또는 Image color로 입히고 원본에 굽지 않는다.

### 3.3 전투·상태

| ID | 에셋 | 수량 | 규격 | 사용처 |
| --- | --- | ---: | --- | --- |
| UI_Ring_Cooldown | 쿨타임 원형 마스크 | 1 | 256×256 | 감정 문양 외곽 |
| UI_Ring_Channel | 채널링 원호 | 1 | 256×256 | 되감기 대상 주변 |
| UI_Marker_Target | 자동 대상 표시 | Selected/Secondary | 128×128 | 되감기·예지 대상 |
| UI_Marker_NoTarget | 대상 없음 | 1 | 128×128 | 짧은 실패 피드백 |
| UI_Status_AttackLocked | 공격 불가 | 1 | 128×128 | 숨죽이기 중 공격 입력 |
| UI_Boss_Frame | 보스 체력선 프레임 | 1, 9-slice | 1024×64 | 화면 하단 |
| UI_Boss_PhaseMark | 보스 단계 흔적 | 1 | 64×64 | 이미 지난 단계 경계 |
| UI_Damage_Edge | 피격 방향 파형 | 8방향 회전 가능 1종 | 256×128 | 화면 가장자리 |

### HUD 생성 참고

```text
minimal dreamlike metroidvania HUD icon sheet, thin bone-white line art,
four distinct symbols: broken counter-clockwise circle, closing eyelid,
forward-split double contour, two misaligned circles becoming aligned,
flat orthographic graphic design, transparent background, no text,
consistent stroke width, readable at 32 pixels, no glow baked into image
```

---

## 4. P1 — 토스트·저장·보상

| ID | 에셋 | 상태/수량 | 규격 | 사용처 |
| --- | --- | ---: | --- | --- |
| UI_Toast_Narrative | 파편 문장 배경 | 1, 9-slice | 1024×180 | 하단 중앙 |
| UI_Toast_Reward | 보상 토스트 배경 | 1, 9-slice | 512×128 | 좌하단 |
| UI_Icon_Fragment | 기억 파편 | 1 | 128×128 | 파편 획득·기록 |
| UI_Icon_Currency | 일반 재화 | 1 | 128×128 | 재화 획득 |
| UI_Icon_HealthShard | 체력 조각 | 1 | 128×128 | 영구 성장 |
| UI_Icon_Shortcut | 열린 숏컷 | 1 | 128×128 | 숏컷 알림·지도 |
| UI_Icon_Checkpoint | 체크포인트 | Idle/Active | 128×128 | 저장·지도 |
| UI_Save_Spinner | 기억 중 표시 | 12프레임/벡터 회전 | 96×96 | 우하단 |
| UI_Save_Complete | 저장 완료 | 1 | 96×96 | 저장 표시 |
| UI_Save_Failed | 저장 실패 | 1 | 96×96 | 오류 표시 |

저장 아이콘은 플로피디스크를 사용하지 않는다. 작은 열린 원이 감기고 닫히는 “기억의 고리”로 표현한다.

---

## 5. P2 — 입력 아이콘

### 5.1 키보드·마우스

| ID 패턴 | 필요 키 |
| --- | --- |
| UI_Key_{Key} | WASD, 방향키, Space, Shift, Ctrl, J, K, L, E, Esc, Tab, Enter, Backspace |
| UI_Mouse_{Input} | Left, Right, Middle, Wheel, Move |

### 5.2 게임패드

| 계열 | 필요 입력 |
| --- | --- |
| Xbox | A/B/X/Y, LB/RB, LT/RT, View/Menu, D-pad, LS/RS, Stick 방향 |
| PlayStation | Cross/Circle/Square/Triangle, L1/R1, L2/R2, Create/Options, D-pad, L3/R3 |
| Generic | South/East/West/North, Shoulder, Trigger, Start/Select, D-pad, Stick |

### 5.3 스타일

- 키캡은 채운 사각형보다 얇은 둥근 윤곽을 사용한다.
- 글리프는 폰트 문자 대신 벡터 패스로 제작해 플랫폼별 차이를 줄인다.
- 32px, 48px, 64px에서 획 두께가 무너지지 않는지 확인한다.
- 입력 아이콘 원본은 흰색 단색으로 만들고 선택 상태는 Unity에서 색을 입힌다.

---

## 6. P2 — 지도

| ID | 에셋 | 상태/수량 | 규격 | 사용처 |
| --- | --- | ---: | --- | --- |
| UI_Map_NodeRoom | 일반 방 노드 | Unknown/Visited/Current | 96×96 | 노드 지도 |
| UI_Map_NodeSecret | 비밀방 노드 | Discovered/Visited | 96×96 | 발견 후 표시 |
| UI_Map_Link | 방 연결선 | Seen/Untraveled/Opened | 256×32 | 노드 연결 |
| UI_Map_Checkpoint | 체크포인트 | 1 | 96×96 | 지도 |
| UI_Map_Shortcut | 숏컷 | Closed/Open | 96×96 | 지도 |
| UI_Map_Boss | 보스방 | Seen/Cleared | 96×96 | 지도 |
| UI_Map_Exit | 지역 출구 | Seen/Open | 96×96 | 지도 |
| UI_Map_Player | 현재 위치 | 8프레임 호흡 또는 벡터 | 96×96 | 현재 방 |
| UI_Map_RegionResidue | 잔재 지역 문양 | 1 | 128×128 | 지역 탭 |
| UI_Map_RegionGaze | 응시 지역 문양 | 1 | 128×128 | 지역 탭 |
| UI_Map_RegionFracture | 균열 지역 문양 | 1 | 128×128 | 지역 탭 |

지도 노드는 실제 방 그림을 축소하지 않는다. 크기와 위험도를 암시하는 원·타원·균열 정도만 사용한다.

---

## 7. P2 — 기억 기록

| ID | 에셋 | 수량/상태 | 규격 | 사용처 |
| --- | --- | ---: | --- | --- |
| UI_Journal_Card | 파편 카드 | Normal/Recent/Connected | 9-slice | 기록 목록 |
| UI_Journal_Thread | 기억 연결선 | 1 | 256×32 | 연결된 파편 |
| UI_Journal_RegionPast | 과거 탭 | 1 | 128×128 | 잔재 필터 |
| UI_Journal_RegionPresent | 현재 탭 | 1 | 128×128 | 응시 필터 |
| UI_Journal_RegionFuture | 미래 탭 | 1 | 128×128 | 균열 필터 |
| UI_Journal_Overlap | 겹쳐진 기억 | 1 | 128×128 | 재해석 항목 |
| UI_Badge_New | 새 기록 점 | 1 | 48×48 | 탭·카드 |
| UI_Frame_MemoryScene | 발견 장소 실루엣 프레임 | 1, 9-slice | 512×288 | 상세 화면 |

발견 장소 이미지는 UI 공통 에셋으로 생성하지 않고 실제 방의 카메라 캡처 또는 단색 실루엣을 사용한다.

---

## 8. P3 — 설정·접근성

| ID | 에셋 | 필요 상태 | 규격 |
| --- | --- | --- | --- |
| UI_Control_Slider | 슬라이더 | Track/Fill/Handle/Focused | 9-slice+SVG |
| UI_Control_Toggle | 토글 | Off/On/Focused/Disabled | 128×128 |
| UI_Control_Dropdown | 드롭다운 | Closed/Open/Selected | 128×128 |
| UI_Control_Tab | 탭 | Idle/Selected/New | 9-slice |
| UI_Control_Scroll | 스크롤바 | Track/Handle | 9-slice |
| UI_Icon_Audio | 오디오 | 1 | 128×128 |
| UI_Icon_Display | 화면 | 1 | 128×128 |
| UI_Icon_Controls | 조작 | 1 | 128×128 |
| UI_Icon_Accessibility | 접근성 | 1 | 128×128 |
| UI_Icon_Gameplay | 게임플레이 | 1 | 128×128 |
| UI_Pattern_ColorAssist | 감정별 색각 보조 패턴 | 4종 | 256×256 |

접근성 아이콘은 사람 신체를 단순화한 범용 심볼보다 해당 기능을 직접 나타내는 아이콘을 우선한다.

---

## 9. P3 — 로딩·전환·지역명

| ID | 에셋 | 수량 | 규격 | 사용처 |
| --- | --- | ---: | --- | --- |
| UI_Loading_Residue | 잔재 로딩 문양 | 1 | 256×256 | 긴 씬 로딩 |
| UI_Loading_Gaze | 응시 로딩 문양 | 1 | 256×256 | 긴 씬 로딩 |
| UI_Loading_Fracture | 균열 로딩 문양 | 1 | 256×256 | 긴 씬 로딩 |
| UI_Loading_Ending | 엔딩 중립 문양 | 1 | 256×256 | 엔딩 전환 |
| UI_RegionTitle_Frame | 지역명 얇은 장식 | 1 | 1024×256 | 첫 진입 2초 |
| UI_Transition_Grain | 전환용 미세 입자 | 2~3 seamless | 512×512 | 페이드 질감 |

로딩 문양은 감정 문양과 같은 조형 언어를 공유하되 스킬 사용 가능 상태로 오해하지 않도록 채움 없이 사용한다.

---

## 10. 폰트와 로고

| ID | 에셋 | 요구 사항 |
| --- | --- | --- |
| UI_Font_Body | 본문 폰트 | 한글 완성형·영문·숫자·기본 기호, 높은 소문자 가독성, SDF 생성 허용 라이선스 |
| UI_Font_Title | 제목 폰트 | 한글 부제 지원, 얇고 길게 뻗은 형태, 작은 본문에는 사용하지 않음 |
| UI_Font_Fallback | 폴백 폰트 | CJK 확장·특수기호 대응 |
| UI_Logo_Main | 영문 로고 | 작은 원과 어긋난 이중 윤곽, 과도한 호러 효과 금지 |
| UI_Logo_Subtitle | 한글 부제 | “눈뜨는 꿈”, 로고와 분리 가능한 레이어 |

폰트 선택 시 OFL 또는 상업적 게임 배포와 SDF 변환이 명확히 허용된 라이선스만 사용한다.

---

## 11. 제작 묶음과 우선순위

| 묶음 | 포함 | 제작 시점 |
| --- | --- | --- |
| A. 메뉴 키트 | 패널, 버튼, 포커스, 모달, 로고 | UI-5 시작 |
| B. 핵심 HUD | 체력, 감정 4종, 쿨타임, 채널링, 보스 | UI-1 기능 검증 후 |
| C. 보상·저장 | 파편, 재화, 체크포인트, 저장 링 | UI-1/SaveService 후 |
| D. 입력 글리프 | 키보드, Xbox, PlayStation, Generic | UI-2 Input System 확정 후 |
| E. 지도·기록 | 지도 노드·연결·기억 카드 | UI-3 와이어프레임 검증 후 |
| F. 설정·접근성 | 컨트롤, 탭, 보조 패턴 | UI-4 기능 검증 후 |
| G. 전환 | 로딩 문양, 지역명, 입자 | 전체 플로우 통합 후 |

기능 레이아웃이 확정되기 전에 최종 패널과 버튼을 먼저 생성하지 않는다. 크기와 상태 수가 바뀌면 재작업 비용이 가장 크기 때문이다.

---

## 12. 에셋별 검수 체크리스트

- [ ] 흑백으로 바꿔도 의미를 구분할 수 있다.
- [ ] 32px에서 외곽선이 끊기지 않는다.
- [ ] 투명 배경 가장자리에 흰색 매트가 없다.
- [ ] 같은 계열 아이콘의 선 두께와 내부 여백이 같다.
- [ ] Selected 상태가 색 변화만으로 구성되지 않았다.
- [ ] 9-slice 코너와 중앙 장식이 늘어나지 않는다.
- [ ] 원본 파일과 Unity용 내보내기 파일이 분리되어 있다.
- [ ] 파일명과 ID가 이 문서의 명명 규칙과 일치한다.
- [ ] 생성형 에셋이면 사용 모델·프롬프트·후처리 내역을 함께 기록했다.
- [ ] 라이선스와 상업적 사용 가능 여부가 기록되어 있다.
