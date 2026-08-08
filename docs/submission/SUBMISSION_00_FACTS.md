# 제출 문서 공통 사실표

> 목적: `게임 소개 및 설명 문서`, `AI 활용 기술 문서`, `팀원 롤 기술서` PDF에 공통으로 들어가는 정보를 한곳에서 관리한다.
> 상태: 초안
> 작성 기준일: 2026-08-08

## 1. 프로젝트 기본 정보

| 항목 | 내용 | 상태 |
|---|---|---|
| 게임명 | Hidden Weight | 확정 |
| 팀명 | IF98 | 가안 |
| 제출일 | 2026-08-08 | 확인 필요 |
| 개발 엔진 | Unity 6000.5.4f1 | 확정 |
| 렌더링 | URP 17.5.0, 2D Renderer | 확정 |
| 장르 | 싱글 플레이 2D 횡스크롤 액션 어드벤처 / 메트로배니아 | 확정 |
| 대상 해상도 | 1920x1080 | 확정 |
| 현재 빌드 | `HiddenWeight/Builds/macOS/HiddenWeight.app` | 확인 필요 |

## 2. 공식 제출 항목 대응표

| 제출 문서 | 공식 요구 내용 | 작성 방식 |
|---|---|---|
| 게임 소개 및 설명 문서 | 게임 제목 및 한 줄 소개 | `Hidden Weight`와 한 줄 소개를 첫 페이지에 배치 |
| 게임 소개 및 설명 문서 | 게임 방법: 목표, 조작, 종료 조건 포함 | 목표/진행 구조/조작표/클리어 조건을 표 중심으로 정리 |
| 게임 소개 및 설명 문서 | 실행 방법: 게임 설치 및 실행 방법 | 빌드 확정 후 플랫폼별 실행 방법 작성 |
| 게임 소개 및 설명 문서 | 플레이 링크 또는 설치 방법 | 배포 전까지 `추후 삽입`으로 표시 |
| 게임 소개 및 설명 문서 | 플레이 영상 링크 | 영상 업로드 전까지 `추후 삽입`으로 표시 |
| AI 활용 기술 문서 | AI 활용에 대한 기술적 설명 | AI 도구별 사용 목적, 작업 흐름, 대표 프롬프트/지시 사항 정리 |
| AI 활용 기술 문서 | 외부 에셋 / 오픈소스 출처 | 음원, 폰트, Unity 패키지, AI 생성물 출처를 표로 정리 |
| 팀원 롤 기술서 | 팀원별 이름 / 담당 역할 | 가나다 순으로 이름과 담당 역할 작성 |
| 팀원 롤 기술서 | 각 팀원이 실제로 맡아 구현한 영역 | 맵, 아트, 음원, 영상, 배포, 문서 등 실제 작업 기준으로 작성 |
| 팀원 롤 기술서 | 협업·분업 방식 | 기획 공유, 담당 영역 분업, AI 활용, 구현/검수/문서화 흐름 정리 |

## 3. 한 줄 소개

꿈속 세계에서 과거의 죄책감, 현재의 수치심, 미래의 불안을 차례로 마주하고, 감정으로 얻은 능력으로 길을 열어 나가는 2D 메트로배니아 게임.

## 4. 게임 개요

- 배경은 현실이 아니라 감정과 기억이 공간으로 변한 꿈속 세계다.
- 플레이어는 튜토리얼 지역인 `몽환의 우주`에서 기본 조작을 익힌 뒤, 세 감정 지역을 순서대로 탐험한다.
- 지역 구조는 과거의 죄책감을 마주하는 `잔재`, 현재의 수치심을 마주하는 `응시`, 미래의 불안을 마주하는 `균열`로 이어진다.
- 각 감정은 단순 수집 요소가 아니라 이동, 전투, 환경, 연출을 바꾸는 능력으로 작동한다.
- 최종 목표는 감정을 제거하는 것이 아니라 외면했던 감정을 받아들이고 꿈의 끝으로 나아가는 것이다.

## 5. 게임 방법

| 구간 | 역할 | 주요 능력 |
|---|---|---|
| 몽환의 우주 | 기본 이동, 점프, 대시, 벽점프 학습 | 없음 |
| 잔재 | 과거와 죄책감을 다루는 첫 지역 | 되감기 |
| 응시 | 현재와 수치심을 다루는 지역 | 숨죽이기, 자각 |
| 균열 | 미래와 불안을 다루는 지역 | 예지 |
| 엔딩 | 세 감정 지역을 통과한 뒤 꿈의 끝에 도달하는 마무리 구간 | 자각 |

클리어 조건은 세 감정 지역을 통과하고 마지막 구간에 도달하는 것이다. HP가 0이 되면 게임오버 화면 없이 마지막 체크포인트에서 부활한다.

## 6. 조작표

| 기능 | 기본 키 |
|---|---|
| 좌우 이동 | `A` / `D` 또는 방향키 `←` / `→` |
| 달리기 | `Shift` 홀드 |
| 점프 / 벽점프 | `Space` |
| 대시 | `Left Ctrl` |
| 공격 | `J` 또는 `1` |
| 상호작용 | `E` 또는 `Enter` |
| 감정 스킬 | `K` 또는 `2` |
| 자각 | `L` 또는 `3` 홀드 |
| 지도 | `M` 또는 `Tab` |
| 일시정지 | `Esc` |

주의: 실제 코드 기준으로 좌우 이동은 Unity `Horizontal` 축을 사용하므로 `A/D`와 방향키가 모두 동작한다. 제출 전 최종 빌드에서 보조 키(`1`, `2`, `3`, `E`, `Enter`, `M`, `Tab`)가 실제 UI/입력 안내와 일치하는지 확인해야 한다.

## 7. 실행 방법 초안

1. 배포된 압축 파일을 내려받아 압축을 해제한다.
2. macOS 빌드 기준 `HiddenWeight.app`을 실행한다.
3. macOS 보안 경고가 뜨는 경우 `시스템 설정 > 개인정보 보호 및 보안`에서 실행을 허용한다.
4. 타이틀 화면에서 시작을 선택해 플레이한다.

최종 제출 문서에는 실제 배포 방식에 맞춰 웹 URL, 다운로드 링크, APK 또는 테스트 링크 중 하나를 명시한다.

## 8. AI 활용 요약

| 도구 | 사용 목적 | 대표 산출물 |
|---|---|---|
| Codex | 코드 구현, 오류 분석, 테스트, Git 작업, 문서 정리, image-gen 스킬을 통한 이미지 생성 보조 | Unity 기능 구현, QA 수정, 제출 문서 초안, 일부 이미지 생성/수정 |
| Claude Code | 코드 구현 보조, 구조 분석, 문서/기획 정리 | 개발 보조, 구현 방향 검토 |
| ChatGPT | 기획 정리, 문구 작성, 제출 문서 구성 | 게임 소개문, AI 활용 정리, 문서 초안 |
| Gemini | 캐릭터 이미지 생성을 위한 프롬프트 설계 | 캐릭터 생성용 프롬프트 구조 및 지시문 |
| ElevenLabs Sound Effects | 일부 효과음 생성 | 공격, 피격, 리스폰, 보상, 게이트, UI 등 시그니처 SFX 후보 |
| ChatGPT 이미지 생성 | Gemini로 설계한 프롬프트를 바탕으로 캐릭터 및 일부 아트 생성 | 캐릭터 이미지, 일부 콘셉트/스프라이트 아트 |

추가로 사용한 AI 도구가 있다면 최종 문서에 도구명, 사용 목적, 산출물을 보강한다.

### 사용 모델

| 구분 | 도구/모델 | 사용 내용 |
|---|---|---|
| 기획 및 캐릭터 방향성 논의 | Claude Code / Sonnet 5 High | 게임 분위기, 캐릭터 디자인 방향, 기획 대화 정리 |
| 개발 및 맵 기반 구현 (전민정 작업 기준) | Codex / GPT 5.6 Sol | Unity 코드 구현, 프로젝트 구조 분석, 맵 베이스 개발, 반복 수정 및 검증 |

### Codex loop 활용

전민정의 맵 기반 개발 과정에서는 Codex의 loop 방식도 활용했다. 초기 맵 베이스와 공통 구조를 만들 때 장시간 반복 작업으로 구현을 진행했고, 진행 중 결정이 필요한 사항은 사용자에게 확인한 뒤 다음 작업으로 넘어가도록 했다. 이 과정은 맵 구조, 기본 시스템, Unity 적용 상태를 빠르게 잡는 데 사용했으며, 이후 팀 회의와 플레이 확인을 통해 수정했다.

### AI 이미지 생성 흐름

- 캐릭터 생성: Claude Code와 대화하며 게임에 어울리는 캐릭터 방향을 설계한 뒤, Gemini에 HTML 정리 문서와 색상 코드표를 전달해 이미지 생성용 프롬프트를 구성했다. 이후 해당 프롬프트를 ChatGPT 이미지 생성 기능에 전달해 캐릭터 이미지를 생성했다.
- 1맵 이미지 및 일부 이미지 수정: Codex 사용 중 image-gen 스킬로 생성하거나 수정했다.
- 생성된 이미지는 그대로 사용하지 않고, 게임 톤과 Unity 적용 상태에 맞게 팀원이 검수 및 수정했다.

### 대표 프롬프트 예시

| 구분 | 내용 |
|---|---|
| 목적 | 기본 캐릭터 시트 생성을 위한 이미지 프롬프트 |
| 설계 흐름 | Claude Code로 캐릭터 디자인 방향 논의 → Gemini로 HTML 문서와 색상 코드표 기반 프롬프트 정리 → ChatGPT 이미지 생성으로 캐릭터 이미지 생성 |
| English Prompt | `Cute chibi SD character design, cute 2d game sprite, full body, soft pastel white and silver hair (#E7EAF0), pale glowing skin (#EEE7E2), luminescent cyan mint glowing eyes (#73D8D4), wearing a simple cream tunic sweater (#F5F3EF) and dark grey leggings, no cape, minimalistic silhouette, clean lines, charming and dreamy aesthetic, metroidvania concept art, isolated on neutral dark background --ar 1:1` |
| 한글 해석 | 귀여운 chibi SD 캐릭터 디자인, 귀여운 2D 게임 스프라이트, 전신, 부드러운 파스텔 화이트/실버 머리카락, 은은히 빛나는 밝은 피부, 발광하는 청록/민트빛 눈동자, 심플한 크림색 튜닉 스웨터와 어두운 회색 레깅스 착용, 망토 없음, 미니멀한 실루엣, 깔끔한 선, 귀엽고 몽환적인 감성, 메트로배니아 컨셉 아트, 어두운 중립 배경 |
| ChatGPT 공유 링크 | https://chatgpt.com/share/6a76748a-7a8c-83ee-94e2-a509a2c6801e |
| Gemini 공유 링크 | https://share.gemini.google/zsYJr5HcPc6t |

주의: 위 공유 링크는 사용자 제공 공개 링크다. 최종 PDF 제출 전 일반 브라우저/비로그인 환경에서 접근 가능 여부를 확인한다.

캐릭터 제작 과정에서 AI 도구를 이렇게 나누어 사용한 이유는 전민정의 작업 환경과 사용 경험에 기반한다. 기획 대화와 캐릭터 방향성 논의는 당시 Claude Code 구독 환경을 사용하고 있었고, 게임 분위기와 캐릭터 톤을 이어서 정리하기에 적합해 Claude Code에서 진행했다. 이미지 생성은 평소 ChatGPT 이미지 생성 결과에 만족한 경험이 있어 ChatGPT를 사용했다. Gemini는 이전 영상 생성 작업에서 긴 조건과 색상 코드표를 이미지 생성용 프롬프트로 구조화하는 데 만족스러운 결과를 얻은 경험이 있어, 이번에도 캐릭터 프롬프트 정리 단계에 사용했다.

### 대표 사례 2. 맵 개발 중 Codex image-gen 활용

맵 개발 과정에서는 Codex의 image-gen 스킬을 사용해 배경, 지형, 오브젝트, 룸 콘셉트 이미지를 생성하거나 수정했다. 단순히 이미지만 생성한 것이 아니라, 기획 문서의 감정/시간축 설정을 바탕으로 프롬프트를 만들고, 생성 결과를 Unity에서 사용할 수 있도록 후처리했다.

대표 작업 방식은 다음과 같다.

1. 기획 문서와 기준 이미지를 먼저 정리한다.
2. Codex가 이미지 생성용 프롬프트와 금지 조건을 작성한다.
3. image-gen으로 원본 이미지를 생성한다.
4. 필요한 경우 `#00ff00` 크로마키 배경을 제거해 투명 PNG로 변환한다.
5. Unity 적용본을 만들고, 실제 플레이 화면에서 가독성/충돌 판정/분위기를 검수한다.
6. 다른 팀원이 생성한 적/오브젝트 아트는 맵 개발 과정에서 크기, 배치, 투명 배경, 시각 가독성 기준으로 수정·보정한다.

| 활용 영역 | 활용 내용 |
|---|---|
| 튜토리얼맵 `몽환의 우주` | T01~T04 배경, 궤도·손 모듈, 지형 아틀라스, 전경 파편 생성. 생성 이미지는 배경/장식에 사용하고 충돌 판정은 Unity Tilemap/Collider로 분리했다. |
| 1맵 `잔재` 지형/배경 | 죄책감과 과거를 표현하는 저채도 석재, 철골, 사슬, 청회색 테두리 규칙을 프롬프트로 고정하고, 지형 모듈과 배경을 생성했다. |
| 1맵 `잔재` 게임 오브젝트 | 적, 아이템, 위험 요소, 보스 파츠 등 일부 아트는 다른 팀원이 이미지 생성으로 제작했고, 맵 개발 과정에서 크기, 배치, 투명 배경, Unity 적용 상태를 수정·보정했다. |
| 플레이어 애니메이션 보정 | 승인된 캐릭터 정체성을 유지한 채 이동, 점프, 공격, 대시, 벽 동작, 숨죽이기, 자각 시트를 생성하고 기존 Unity atlas 규격에 맞게 정규화했다. |
| 균열 지역 콘셉트 | 밝지만 불안한 미래 지역의 전체 지도와 룸별 콘셉트를 built-in `image_gen`으로 생성했다. |

맵 이미지 생성 대표 프롬프트는 튜토리얼맵 T01 배경 1개만 넣는다.

```text
Use case: stylized-concept
Asset type: production game background for T01 of an original 2D side-scrolling psychological dream game
Primary request: a silent dreamlike outer space where a tiny self is about to awaken, expressing memory before it has taken a clear shape
Scene/backdrop: vast charcoal-black and deep-indigo space, sparse cold-white stars, translucent muted-lavender nebulae, one or two extremely thin incomplete circular orbits, a very distant ambiguous hand-shaped constellation barely readable near the horizon
Style/medium: painterly high-detail 2D game environment, psychological cosmic surrealism, soft atmospheric depth, not photorealistic
Composition/framing: 16:9 wide side-scrolling background; calm open negative space; lower 35 percent dark and visually quiet for gameplay collision art; no bright horizontal shapes that resemble platforms
Lighting/mood: still, weightless, gentle birth, beautiful before unsettling
Color palette: charcoal, deep indigo, muted lavender, cold white
Constraints: no character, no enemy, no text, no UI, no logo, no watermark, no explicit eye, no gore, no detailed foreground objects, no false floors, bridges, stairs or ledges
```

PDF에는 내부 문서 경로나 작업 로그 파일명을 넣지 않고, 위 활용 내용과 대표 프롬프트/결과 캡처만 정리한다.

## 9. 외부 에셋 및 오픈소스 출처

| 항목 | 출처 | 라이선스 / 비고 | 상태 |
|---|---|---|---|
| 일반 게임플레이 효과음 | Kenney Impact Sounds, Kenney RPG Audio | CC0 | 확인 필요 |
| UI confirm 효과음 | Freesound `Game Audio Starter Pack by Rob_Marion` | Creative Commons Zero 1.0 | 확인 필요 |
| 폰트 | NanumMyeongjo | SIL Open Font License 1.1 | 확인 필요 |
| 게임 엔진 | Unity | Unity 라이선스 및 패키지 라이선스 적용 | 확인 필요 |
| Unity 패키지 | URP, 2D Tilemap, 2D Sprite, UGUI, Test Framework 등 | Unity Companion / Package Distribution License 계열 | 확인 필요 |
| AI 생성 이미지 | 팀 프롬프트 기반 생성 후 검수 및 Unity 적용 | 생성 도구 정책 확인 필요 | 확인 필요 |
| AI 생성 효과음 | ElevenLabs Sound Effects 생성 후 편집 | 사용 권한 확인 필요 | 확인 필요 |

## 10. 팀원 정보

| 이름 | 담당 역할 | 실제 구현/작업 영역 |
|---|---|---|
| 김승혁 | 기획, 아트/AI 활용, 개발, 문서 수정 | 개발 기반 구현을 위한 기획 문서/AI 프롬프트 작성, Unity 프로젝트 초기 세팅, 플레이어 조작·기본 시스템 등 공통 기반 개발, 담당 맵 개발, 음원 생성·선별·관리, 맵 배경 생성, 전민정이 작성한 제출 기반 문서 검토 및 수정 |
| 임채원 | 기획, 아트/AI 활용, 개발, 문서 수정 | 공통 기반 수정 및 공통 버그 수정, 담당 맵 개발, 캐릭터·스프라이트·아트 생성, 배포 방법 조사, 전민정이 작성한 제출 기반 문서 검토 및 수정 |
| 전민정 | 기획, 아트/AI 활용, 개발, 문서 작성 | 전체 아이디어 기획, 게임 기획 문서 작성, 맵 세부 기획, 맵 베이스 개발, 공통 기반 수정, 담당 맵 개발, 아트 적용·수정, 영상 생성, 제출 문서의 기반 초안 작성 |

팀원 순서는 가나다 순으로 기재한다. 담당 역할은 기획, 아트/AI 활용, 개발, 문서 순서를 기준으로 실제 기여에 맞게 작성한다.

### 협업·분업 방식 초안

팀은 주 1~2회 온라인 Discord 회의를 통해 진행 상황을 공유하고 작업 범위를 정했다. 초기 기획과 최종 제출 준비 단계에서는 오프라인으로 모여 전체 방향을 조율했다.

개발은 공통 기반 개발, 기반 수정, 맵 베이스 개발, 맵별 분담 개발 순서로 진행했다. 먼저 플레이어 조작, 기본 시스템, 프로젝트 세팅, 맵 구성 방식 등 공통 기반을 만들고, 테스트와 회의를 통해 필요한 부분을 수정했다. 이후 각 맵의 구조를 정한 뒤 팀원별로 담당 맵을 나누어 개발했다.

작업 분배는 회의에서 액션 아이템을 정리한 뒤, 팀원이 자원하거나 희망하는 영역을 선택하고 일정과 작업량에 맞춰 조율하는 방식으로 진행했다. 각자 맡은 맵, 아트, 음원, 영상 작업 결과는 회의와 공유 자료를 통해 함께 검토하고 수정했다. 제출 문서는 전민정이 기반 초안을 작성하고, 김승혁과 임채원이 내용을 검토·수정하는 방식으로 진행했다.

## 11. 문서 작성 방향

- PDF 페이지 제한은 없으므로 간단하고 핵심적인 내용 위주로 작성한다.
- 세 문서는 긴 설명보다 표, 항목형 정리, 대표 사례 중심으로 구성한다.
- 플레이 링크와 플레이 영상 링크는 빌드 및 배포 완료 후 삽입한다.
- AI 도구 목록은 현재 `Codex`, `Claude Code`, `ChatGPT`, `Gemini`, `ChatGPT 이미지 생성`, `ElevenLabs Sound Effects`를 기준으로 작성하되, 추가 도구가 생기면 최종 PDF 전에 보강한다.

## 12. 제출 전 체크해야 할 항목

### 링크 및 배포

- [ ] 플레이 링크 또는 다운로드 링크 확정
- [ ] 플레이 영상 링크 확정
- [ ] 링크 공개 권한 확인
- [ ] QR 코드 삽입 여부 결정
- [ ] macOS, Windows, WebGL, APK 중 제출 대상 플랫폼 확정

### 실행 및 조작

- [ ] 최종 빌드 파일명과 실행 경로 확인
- [ ] 압축 해제 후 실행 가능 여부 확인
- [ ] macOS 보안 경고 안내 필요 여부 확인
- [ ] 방향키 `←` / `→` 이동 동작 확인
- [ ] `A/D`, `Shift`, `Space`, `Ctrl`, `J`, `K`, `L`, `Esc` 동작 확인
- [ ] 문서의 보조 키 안내(`1`, `2`, `3`, `E`, `Enter`, `M`, `Tab`)가 실제 빌드와 일치하는지 확인

### 에셋 및 라이선스

- [ ] Kenney CC0 음원 출처 링크 확인
- [ ] Freesound CC0 음원 출처 링크 확인
- [ ] NanumMyeongjo OFL 라이선스 표기 확인
- [ ] Unity 패키지 및 오픈소스 출처 표기 범위 확인
- [ ] AI 생성 이미지의 사용 도구와 생성/편집 내역 확인
- [ ] AI 생성 효과음의 사용 도구와 생성/편집 내역 확인
- [ ] 외부 에셋과 AI 생성 에셋을 문서에서 구분해 표기

### 팀 및 역할

- [ ] 팀명 `IF98` 최종 확정 여부 확인
- [ ] 팀원 이름 표기 확인
- [ ] 팀원별 담당 역할 확정
- [ ] 팀원별 실제 구현/작업 영역 확인
- [ ] 협업 방식 설명 확정

### PDF 품질

- [ ] 한글 폰트 깨짐 여부 확인
- [ ] 표가 페이지 밖으로 잘리지 않는지 확인
- [ ] 링크 클릭 가능 여부 확인
- [ ] 표지, 목차, 페이지 번호 필요 여부 확인
- [ ] 제출 파일명 규칙 확인
