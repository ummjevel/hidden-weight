# 균열 지역 게임플레이 이미지 생성 기록

## 공통 아트 방향

- 아름다운 미래가 생물처럼 위장한 백룸형 폐허
- 상아색 대리석, 옅은 민트·시안 유리, 연보라 꽃, 잘못된 대칭
- 공포의 직접 묘사는 최소화하고 복제된 팔다리·시간차 잔상·유리 균열로만 암시
- 모든 게임플레이 요소는 투명 배경 RGBA PNG

## 생성 방식

- 적 4종과 보스 2종은 이미지 생성 모델로 원화를 만든 뒤 크로마키 제거·규격화를 거쳤다.
- 지형·발판·문·이동 장치·환경 기믹·VFX·UI는
  `build_fracture_gameplay_art.py`가 동일 팔레트와 형태 문법으로 생성한다.
- 캐릭터와 맵 연결, Collider, Pivot, Animator 설정은 포함하지 않는다.

## 스프라이트 시트 분할

| 파일 | 배열 | 셀 크기 |
|---|---:|---:|
| 적 4종 | 8 × 4 | 192 × 192 |
| SecondHandWatcher_Combat | 10 × 7 | 192 × 192 |
| SecondHandWatcher_Transitions | 8 × 4 | 192 × 192 |
| UnarrivedSelf_Combat | 8 × 7 | 192 × 192 |
| UnarrivedSelf_Possibilities | 8 × 4 | 192 × 192 |
| UnarrivedSelf_Reactions | 8 × 3 | 192 × 192 |
| 대부분의 환경 애니메이션·VFX | 8 × 3 또는 8 × 4 | 192 × 192 |

## 이미지 생성 프롬프트 핵심

### 적 공통

```text
Professional 2D side-view metroidvania animation sprite sheet.
Fracture-region palette: ivory marble, pale cyan glass, lavender flowers,
subtle wrong symmetry and faint cosmic unease. Strict 8 columns x 4 rows,
consistent scale and baseline, separated silhouettes, flat chroma green
#00FF00 background, no scenery, no text, no borders.
Rows: idle, movement, attack, hit/death transition.
```

### Second-Hand Watcher

```text
Tall elegant clockwork sentinel made from white marble and pale-violet glass,
elongated faceless oval mask with one cyan vertical time-slit, asymmetrical
floating clock hands, jointed limbs and torn lavender mantle. Side-view
metroidvania boss. Include idle, stalking, clock-hand slashes, delayed strike,
time-bolt cast, stagger, glass-and-marble death, teleport and phase transition.
```

### The Unarrived Self

```text
Tall translucent pearl-glass humanoid with long flowing white hair,
featureless face with a tiny lavender crack, asymmetric white mantle,
pale-cyan future glow and subtly duplicated limbs. Include suspended idle,
glide, glass-ribbon attacks, possibility shards, stagger, petal dissolve,
winged form, ribbon-beast form, divided possible selves, oracle form,
awakening, phase change and defeated acceptance.
```

## 재생성

```bash
python docs/concept-art/generated/fracture-gameplay-art/build_fracture_gameplay_art.py
```

