# 잔재 1맵 효과음 전체 구성·생성 계획

> 대상: 잔재 지역 플레이 가능 빌드  
> 생성 도구: ElevenLabs Sound Effects  
> 제작 원칙: 기본 조작음을 먼저 완성하고, 지역 고유 능력인 되감기는 마지막에 제작한다.  
> 연관 문서: [전체 오디오 프롬프트](./RESIDUE_AUDIO_GENERATION_PROMPTS.md)

## 1. 공통 사운드 방향

잔재 지역의 효과음은 `마른 금속`, `낡은 돌`, `재`, `천`, `억눌린 저음`을 중심으로 한다.
플레이어의 기본 행동은 짧고 선명해야 하며, 환경음과 보스음만 긴 잔향을 허용한다.

- 시점: 2D 횡스크롤 메트로배니아
- 분위기: 고딕 폐허, 죄책감, 거대한 손 형상의 도시, 남색 어둠, 희미한 앰버 불빛
- 금지 요소: 음악, 대사, 명확한 언어, 총성, 현대 기계음, 트레일러식 폭발음
- 기본 출력: WAV, 48 kHz
- 권장 후처리 저장: 24-bit 원본과 게임용 변환본을 모두 보관
- 기본 Prompt Influence: `55%`
- 짧은 조작음: Looping `OFF`
- 지속음: Looping `ON`
- 한 프롬프트에서 나온 4개 결과는 서로 다른 행동으로 쓰지 않고 같은 행동의 랜덤 변형으로 사용

### 파일 규칙

```text
SFX_[주체]_[행동]_[재질]_[번호].wav
```

예:

```text
SFX_PLAYER_STEP_STONE_01.wav
SFX_PLAYER_STEP_STONE_02.wav
SFX_PLAYER_HURT_01.wav
```

## 2. 제작 우선순위

| 단계 | 범위 | 목적 | 완료 조건 |
|---|---|---|---|
| P0 | 플레이어 이동 | 조작감 확정 | 이동 테스트 10분 동안 반복감과 피로감이 없음 |
| P1 | 기본 전투·피격 | 타격감 확정 | 공격 성공·실패·피격을 화면 없이도 구분 |
| P2 | 일반 적 | 적 종류와 공격 예고 구분 | 주요 공격 예고가 실제 타격보다 먼저 들림 |
| P3 | 아이템·UI·월드 | 탐험 보상과 상호작용 전달 | 획득·실패·해금 상태를 소리만으로 구분 |
| P4 | 중간 보스·최종 보스 | 전투 가독성 확장 | 위험 패턴마다 독립적인 예고음이 존재 |
| P5 | 되감기 | 잔재 고유 정체성 완성 | 시작·지속·취소·완료 상태를 모두 구분 |

## 3. 게임 적용 공통 규칙

### 랜덤 변형

- 걷기·달리기·공격·피격은 최소 3개 변형을 사용한다.
- 같은 파일의 연속 재생을 막는다.
- 재생 피치는 매번 `±3%`, 음량은 `±1.5 dB` 안에서만 무작위로 바꾼다.
- 점프·대시·UI 확인음처럼 조작 타이밍을 알려주는 소리는 피치를 크게 흔들지 않는다.

### 우선순위와 음량

| 그룹 | 상대 우선순위 | 시작 음량 기준 |
|---|---:|---:|
| 플레이어 피격·사망 | 100 | -6 dB |
| 보스 공격 예고 | 95 | -7 dB |
| 플레이어 공격·대시 | 90 | -8 dB |
| 적 공격·피격 | 80 | -9 dB |
| 아이템·상호작용 | 70 | -10 dB |
| 걷기·달리기 | 55 | -14 dB |
| 환경 장식음 | 30 | -18 dB |

수치는 최종 마스터가 아니라 첫 구현용 기준이다. 여러 효과음이 겹칠 때는 플레이어 피격과 보스
예고음이 묻히지 않아야 한다.

### 2D 공간 처리

- 화면 중심을 기준으로 좌우 패닝을 적용한다.
- 일반 적은 화면 밖으로 멀어질수록 고역을 줄인다.
- 플레이어 기본음은 위치와 관계없이 지나치게 좌우로 치우치지 않게 한다.
- 공격 예고음은 타격 순간보다 최소 `0.25초` 먼저 재생한다.
- 발소리는 애니메이션 프레임 이벤트에서 재생한다.

---

# 4. P0 — 플레이어 이동 효과음

아래 코드 블록은 각각 ElevenLabs에 **한 번씩 별도로 입력**한다.

## 4.1 걷기

### SFX_PLAYER_STEP_STONE

- Duration: `0.35s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated light footstep of a small agile cloaked character on dusty ancient charcoal stone, a soft boot tap, tiny grit and restrained cloth movement. Dark gothic 2D game sound, dry and close, delicate rather than heavy, no music, no voice, no reverb, no extra footsteps.
```

### SFX_PLAYER_STEP_METAL

- Duration: `0.4s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated light footstep of a small cloaked character on old hollow black iron plating, a precise metallic tick, tiny rust movement and very short dark resonance. Gothic ruined-city game sound, dry and close, no music, no voice, no large clang, no extra footsteps.
```

### SFX_PLAYER_STEP_BRIDGE

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated light footstep on a suspended ancient stone-and-chain bridge, a small deck contact followed by one subtle chain response. Fragile and controlled, dark gothic 2D game sound, no music, no voice, no loud impact, no extra footsteps.
```

### SFX_PLAYER_STEP_FIBROUS

- Duration: `0.4s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated light footstep on an uncanny surface between weathered stone and dry dense fibers, a soft compressed texture with tiny dust, eerie but not wet or gory. Close 2D game sound, no music, no voice, no squelch, no extra footsteps.
```

## 4.2 달리기

달리기 프롬프트 결과는 걷기보다 접촉음이 조금 강하고 꼬리가 짧아야 한다. 실제 게임에서는
각 발마다 한 파일씩 교대로 재생하고 재생 속도를 강제로 높이지 않는다.

### SFX_PLAYER_RUN_STONE

- Duration: `0.3s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated quick running footstep of a small agile cloaked character on dusty ancient charcoal stone, firm boot contact, short grit scatter and a quick cloth flick. Responsive and compact, no music, no voice, no reverb, no sequence of steps.
```

### SFX_PLAYER_RUN_METAL

- Duration: `0.32s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated quick running footstep on old hollow black iron, a crisp restrained metal contact with tiny rust particles and an extremely short ring. Agile gothic game sound, no music, no voice, no heavy clang, no sequence of steps.
```

### SFX_PLAYER_RUN_BRIDGE

- Duration: `0.36s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated quick running footstep on a suspended stone-and-chain bridge, compact deck impact with one brief tense chain twitch. Urgent but lightweight, no music, no voice, no collapse, no sequence of steps.
```

### SFX_PLAYER_RUN_FIBROUS

- Duration: `0.32s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated quick running footstep on a dry uncanny stone-fiber surface, compact compressed contact, brittle fibers and a tiny dust release. Eerie, agile and non-gory, no music, no voice, no wet squelch, no sequence of steps.
```

## 4.3 공중 이동

### SFX_PLAYER_JUMP

- Duration: `0.4s`
- Looping: `OFF`
- Prompt Influence: `52%`

```text
Isolated small agile jump, a quick cloth lift, soft boot push and thin displaced air with a faint midnight-violet magical trace. Responsive dark fantasy 2D game sound, no music, no voice, no cartoon boing, no impact.
```

### SFX_PLAYER_LAND_LIGHT

- Duration: `0.5s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated light landing of a small cloaked character on dusty dark stone, two soft boot contacts, short cloth settling and a tiny ash puff. Controlled and readable, no music, no voice, no heavy impact, no long reverb.
```

### SFX_PLAYER_LAND_HEAVY

- Duration: `0.75s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated hard landing of a small cloaked character on ancient dark stone, compact body weight, brittle grit burst and one restrained low stone resonance. Painful but not cinematic, no music, no voice, no superhero slam, no explosion.
```

### SFX_PLAYER_DASH

- Duration: `0.5s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated supernatural horizontal dash by a small cloaked character, sharp reversed cloth snap, compressed air cut and a thin midnight-violet granular trail that collapses quickly. Dark fantasy, responsive, no music, no voice, no engine, no sci-fi laser.
```

### SFX_PLAYER_WALL_GRAB

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `57%`

```text
Isolated wall catch by a small agile character, boots and hands gripping rough gothic stone, a brief scrape, cloth tension and a few falling grains. Dry and compact, no music, no voice, no prolonged slide.
```

### SFX_PLAYER_WALL_SLIDE_LOOP

- Duration: `2s`
- Looping: `ON`
- Prompt Influence: `48%`

```text
Perfectly seamless controlled wall-slide loop, soft boot and cloth friction against rough ancient stone with sparse tiny grit. Quiet, steady and dry for a 2D gothic game, no music, no voice, no impact, no pulse, no audible restart.
```

### SFX_PLAYER_WALL_JUMP

- Duration: `0.5s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated agile wall jump, one firm boot push against rough stone, brief grit burst, cloth snap and quick sideways air movement. Responsive dark fantasy game sound, no music, no voice, no cartoon effect.
```

---

# 5. P1 — 기본 전투·피격 효과음

## 5.1 공격 동작

### SFX_PLAYER_ATTACK_SWING

- Duration: `0.4s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated fast melee swing by a small agile character, narrow weapon air cut, restrained cloth arc and a faint dark-violet edge. Sharp and readable but not metallic or huge, no impact, no music, no voice, no laser.
```

### SFX_PLAYER_ATTACK_SWING_HEAVY

- Duration: `0.65s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated committed heavy melee swing, tense cloth wind-up followed by a broad dense air cut and restrained dark-violet energy edge. Weighty but not cinematic, no impact, no music, no voice, no explosion.
```

### SFX_PLAYER_ATTACK_AIR

- Duration: `0.42s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated mid-air melee swing, quick weapon arc with more open air movement, light cloth flutter and a thin dark-violet edge. Agile and compact, no impact, no music, no voice, no sci-fi sound.
```

## 5.2 공격 적중 재질

### SFX_HIT_ASH_FLESH

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated melee hit against a dry ash-filled creature, compact body contact, brittle crust break and a small burst of dark powder. Disturbing but not wet or gory, no weapon swing, no music, no voice, no explosion.
```

### SFX_HIT_STONE

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated melee weapon hit against ancient cracked charcoal stone, sharp mineral contact, a small chip break and short dense resonance. Dry and readable, no weapon swing, no music, no voice, no large destruction.
```

### SFX_HIT_IRON

- Duration: `0.5s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated melee hit against old black iron armor, compact hard contact, restrained metal scrape and one short low ring. Defensive and weighty, no weapon swing, no music, no voice, no giant clang.
```

### SFX_HIT_BLOCKED

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated clearly blocked melee strike against hardened iron and stone, sharp rejected contact, brief scrape and a tight downward resonance. Communicates zero damage immediately, no weapon swing, no music, no voice, no huge clang.
```

## 5.3 플레이어 상태

### SFX_PLAYER_HURT

- Duration: `0.6s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated player damage response without spoken voice, compact body and cloth impact, involuntary breath-like air release and a tiny fracture of violet memory energy. Immediate and painful, no dialogue, no scream, no music, no long reverb.
```

### SFX_PLAYER_KNOCKBACK

- Duration: `0.55s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated small character knocked backward, sharp cloth snap, displaced air and brief boot scrape with loose grit. Fast and readable, no vocal, no impact source, no music, no cinematic boom.
```

### SFX_PLAYER_DEATH

- Duration: `2.5s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated death of a small cloaked memory-being, body loosens into soft ash and fractured midnight-violet particles, one restrained low emotional tone, then silence. Tragic and intimate, no scream, no music, no explosion, no triumphant cue.
```

### SFX_PLAYER_RESPAWN

- Duration: `2s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated player reformation at an ancient checkpoint, ash flows backward, cloth materializes, warm amber memory energy gathers and ends with a quiet clean confirmation. Dark gothic fantasy, no music, no voice, no triumphant flourish.
```

### SFX_PLAYER_HEAL

- Duration: `1.3s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated healing action, a warm amber pulse opens inside a dark reliquary, soft glass harmonic rises with a restrained breath of air, ending in a clear gentle confirmation. Intimate, no music, no voice, no sparkling fairy sound.
```

### SFX_PLAYER_LOW_HEALTH_LOOP

- Duration: `4s`
- Looping: `ON`
- Prompt Influence: `45%`

```text
Perfectly seamless subtle low-health pressure loop, muted internal pulse, faint cloth breath and unstable violet grain. Quiet psychological tension beneath combat, no literal heartbeat, no music, no voice, no alarm, no audible restart.
```

---

# 6. P2 — 일반 적 효과음

적마다 `발견 → 예고 → 공격 → 피격 → 사망`의 다섯 상태가 소리만으로 구분되어야 한다.

## 6.1 잔재 보행자

### SFX_WALKER_NOTICE

- Duration: `0.9s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated alert sound of a thin ash-and-iron humanoid noticing the player, dry neck twitch, tiny chain tension and a hollow breath with no language. Unsettling and restrained, no music, no spoken voice, no roar.
```

### SFX_WALKER_ATTACK_TELEGRAPH

- Duration: `0.75s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated melee attack warning from a thin ash-and-iron humanoid, joints pull tight, rusty blade drags briefly and pressure rises toward a clear stop. Must warn before the strike, no impact, no music, no voice, no trailer rise.
```

### SFX_WALKER_ATTACK

- Duration: `0.6s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated attack by a thin ash-and-iron humanoid, sudden dry joint snap and narrow rusty blade cut through air. Fast, hostile and compact, no impact, no music, no voice, no giant metal clang.
```

### SFX_WALKER_HURT

- Duration: `0.55s`
- Looping: `OFF`
- Prompt Influence: `60%`

```text
Isolated damage reaction of a thin ash-and-iron humanoid, brittle torso crack, short metal shiver and dry ash release. No language, no scream, no weapon swing, no music, no wet gore.
```

### SFX_WALKER_DEATH

- Duration: `1.4s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated death of a thin ash-and-iron humanoid, frame folds inward, small joints break, ash empties out and one loose iron piece settles. Dark and restrained, no scream, no music, no explosion.
```

## 6.2 매달린 손가락

### SFX_FINGER_NOTICE

- Duration: `0.9s`
- Looping: `OFF`
- Prompt Influence: `65%`

```text
Isolated awakening of a hanging finger-shaped creature made from dry stone and dense fibers, tendons tighten, dust falls and one joint clicks into attention. Uncanny but non-gory, no voice, no music, no wet flesh.
```

### SFX_FINGER_DROP_TELEGRAPH

- Duration: `0.8s`
- Looping: `OFF`
- Prompt Influence: `65%`

```text
Isolated warning before a ceiling creature drops, dry fibers stretch, stone dust trickles and tension rapidly stops at the release point. Clear gameplay telegraph, no impact, no voice, no music, no cinematic rise.
```

### SFX_FINGER_DROP

- Duration: `0.6s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated fast downward lunge of a long finger-shaped stone-fiber creature, sudden dry tendon release and dense air displacement. No landing impact, no voice, no music, no wet flesh, no explosion.
```

### SFX_FINGER_IMPACT

- Duration: `0.85s`
- Looping: `OFF`
- Prompt Influence: `65%`

```text
Isolated heavy finger-shaped creature striking ancient stone floor, dense dry impact, short mineral crack, dust burst and a low restrained structural response. No voice, no music, no cinematic boom, no wet gore.
```

### SFX_FINGER_DEATH

- Duration: `1.5s`
- Looping: `OFF`
- Prompt Influence: `65%`

```text
Isolated death of a finger-shaped stone-fiber creature, rigid joints curl incorrectly, dry inner strands snap and the form crumbles into dust. Uncanny and non-gory, no scream, no music, no wet sound.
```

## 6.3 애도 운반자

### SFX_CARRIER_CHARGE_TELEGRAPH

- Duration: `1.1s`
- Looping: `OFF`
- Prompt Influence: `63%`

```text
Isolated warning of a large burdened ash creature preparing to charge, heavy frame lowers, chains pull taut, boots grind stone and pressure stops just before release. Clear gameplay telegraph, no attack impact, no voice, no music.
```

### SFX_CARRIER_CHARGE

- Duration: `1.5s`
- Looping: `ON`
- Prompt Influence: `55%`

```text
Perfectly seamless short charge movement loop of a large burdened ash-and-iron creature, pounding restrained steps, shaking chains and dense cloth mass moving forward. No impacts, no voice, no music, no audible restart.
```

### SFX_CARRIER_WALL_IMPACT

- Duration: `1.1s`
- Looping: `OFF`
- Prompt Influence: `66%`

```text
Isolated large ash-and-iron creature colliding with an ancient stone wall, heavy compact impact, chain recoil, cracked masonry and falling grit. Powerful but not cinematic, no voice, no music, no explosion.
```

### SFX_CARRIER_DEATH

- Duration: `1.8s`
- Looping: `OFF`
- Prompt Influence: `64%`

```text
Isolated death of a large burdened ash-and-iron creature, knees fail, chains lose tension, heavy dry body collapses and ash leaks across stone. Tragic and restrained, no scream, no music, no explosion.
```

---

# 7. P3 — 아이템·UI·월드 상호작용

### SFX_ITEM_SMALL_PICKUP

- Duration: `0.7s`
- Looping: `OFF`
- Prompt Influence: `52%`

```text
Isolated pickup of a small residue shard, tiny dark glass contact, soft ash inhale and a brief warm amber confirmation. Subtle and repeatable, no music, no voice, no coin sound, no bright arcade sparkle.
```

### SFX_ITEM_MEMORY_FRAGMENT

- Duration: `1.8s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated collection of an important memory fragment, fragile dark glass pieces draw together, distant reversed cloth breath, warm amber tone and one incomplete violet harmonic. Emotional but restrained, no music, no voice, no triumphant fanfare.
```

### SFX_ITEM_HEALTH_PICKUP

- Duration: `1s`
- Looping: `OFF`
- Prompt Influence: `54%`

```text
Isolated health pickup, a small warm amber vessel opens, soft glass resonance and a clean low pulse settle into the player. Clear and comforting within a dark gothic world, no music, no voice, no fairy sparkle.
```

### SFX_CHECKPOINT_ACTIVATE

- Duration: `2s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated activation of an ancient gothic checkpoint shrine, dormant iron petals unlock, ash briefly flows upward, warm amber memory light blooms and ends with a low secure confirmation. Sacred but uneasy, no choir, no music, no voice.
```

### SFX_CHECKPOINT_REST

- Duration: `1.6s`
- Looping: `OFF`
- Prompt Influence: `52%`

```text
Isolated resting at an active checkpoint, old iron gently settles, cloth relaxes, ash quiets and a warm amber pulse closes softly. Safe but melancholic, no music, no voice, no bright magical flourish.
```

### SFX_GATE_UNLOCK

- Duration: `1.5s`
- Looping: `OFF`
- Prompt Influence: `63%`

```text
Isolated ancient black-iron gate unlocking, one corroded latch retracts, chain tension releases and a heavy mechanism settles with a short low resonance. Gothic and physical, no music, no voice, no modern machinery.
```

### SFX_SHORTCUT_OPEN

- Duration: `2.1s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated permanent shortcut opening in a ruined gothic city, several old iron locks release in sequence, a restrained chain moves and a stone barrier settles into place. Satisfying but not triumphant, no music, no voice, no huge crash.
```

### SFX_SECRET_REVEAL

- Duration: `1.4s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated hidden passage revealing itself, brittle wall dust shifts inward, a thin uncanny violet resonance appears and one concealed stone edge slides free. Quiet discovery, no music, no voice, no puzzle jingle.
```

### SFX_HIDDEN_EYE_REVEAL

- Duration: `1.8s`
- Looping: `OFF`
- Prompt Influence: `64%`

```text
Isolated reveal of a hidden eye embedded in ancient architecture, dry stone membrane separates, many tiny inner surfaces adjust and a low focused pressure turns toward the listener. Deeply uncanny, no wet gore, no voice, no music, no jump-scare hit.
```

### SFX_UI_CONFIRM

- Duration: `0.35s`
- Looping: `OFF`
- Prompt Influence: `48%`

```text
Isolated dark gothic UI confirmation, one soft iron tick joined by a tiny warm amber glass tone. Clean, restrained and readable, no music, no voice, no arcade beep, no reverb.
```

### SFX_UI_CANCEL

- Duration: `0.3s`
- Looping: `OFF`
- Prompt Influence: `48%`

```text
Isolated dark gothic UI cancel sound, a muted backward iron tick and a short dry cloth-like release. Clean and unobtrusive, no music, no voice, no electronic beep, no reverb.
```

### SFX_UI_ERROR

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `52%`

```text
Isolated unavailable-action UI sound, two restrained low iron taps with a dull violet grain that immediately collapses. Clear but not irritating, no music, no voice, no alarm, no electronic buzzer.
```

---

# 8. P4 — 보스 핵심 효과음

## 8.1 손목 감시자

### SFX_WRIST_SWEEP_TELEGRAPH

- Duration: `1s`
- Looping: `OFF`
- Prompt Influence: `65%`

```text
Isolated warning before a towering black-iron guardian sweeps a long blade, wrist gears pull taut, blade edge slowly scrapes stone and chain tension rises to a precise release point. Clear boss telegraph, no attack, no music, no voice.
```

### SFX_WRIST_BLADE_SWEEP

- Duration: `0.8s`
- Looping: `OFF`
- Prompt Influence: `65%`

```text
Isolated wide blade sweep from a towering black-iron guardian, massive but fast metal edge cutting dense air with a short chain recoil. Dangerous and readable, no impact, no music, no voice, no cinematic boom.
```

### SFX_WRIST_STAGGER

- Duration: `1.2s`
- Looping: `OFF`
- Prompt Influence: `64%`

```text
Isolated stagger of a towering black-iron guardian, wrist mechanism skips, armor locks collide, chains slacken and one heavy knee strikes stone. Exposes temporary weakness, no music, no voice, no explosion.
```

### SFX_WRIST_DEATH

- Duration: `2.8s`
- Looping: `OFF`
- Prompt Influence: `66%`

```text
Isolated defeat of a towering black-iron wrist guardian, blade loses tension, internal joints fail in sequence, chains fall and the hollow frame settles into ash. Heavy and mournful, no scream, no music, no triumphant explosion.
```

## 8.2 기억의 훈육자

### SFX_INSTRUCTOR_CHAIN_TELEGRAPH

- Duration: `1.2s`
- Looping: `OFF`
- Prompt Influence: `66%`

```text
Isolated boss warning from an enormous gallows-and-chain entity, several chains pull tight in different directions, old wood or bone-like structure bends and pressure stops sharply before release. Terrifying and readable, no attack impact, no music, no voice.
```

### SFX_INSTRUCTOR_CHAIN_STRIKE

- Duration: `0.9s`
- Looping: `OFF`
- Prompt Influence: `66%`

```text
Isolated enormous ancient chain striking across a stone arena, violent iron movement, dense air cut and compact stone contact with a short low response. Brutal but controlled, no music, no voice, no trailer boom.
```

### SFX_INSTRUCTOR_MEMORY_BURST

- Duration: `1.4s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated burst of corrupted memory from a giant gothic entity, layered reversed ash, fractured violet glass energy and a low inward pressure that snaps outward. Supernatural and disturbing, no music, no voice, no sci-fi laser, no explosion boom.
```

### SFX_INSTRUCTOR_PHASE_CHANGE

- Duration: `3s`
- Looping: `OFF`
- Prompt Influence: `66%`

```text
Isolated phase change of a giant gallows-and-chain memory entity, all chains suddenly tighten, the arena structure groans, ash reverses upward and a deep violet fracture opens before an abrupt controlled silence. No music, no voice, no trailer braam.
```

### SFX_INSTRUCTOR_DEATH

- Duration: `4s`
- Looping: `OFF`
- Prompt Influence: `64%`

```text
Isolated defeat of a giant gallows-and-chain memory entity, punishment mechanisms lose tension one by one, chains descend, dry structure folds inward and trapped ash exhales into quiet open space. Tragic relief, no scream, no music, no explosion.
```

---

# 9. P5 — 되감기 효과음

기본 플레이 음향과 전투 믹스가 확정된 뒤 제작한다. 되감기는 다른 마법 효과와 달리 `역재생된
재질음 + 앰버 기억음`으로 인식시킨다.

### SFX_REWIND_TARGET_FOUND

- Duration: `0.45s`
- Looping: `OFF`
- Prompt Influence: `52%`

```text
Isolated selection of a rewindable broken object, one tiny fragment moves backward into place and a restrained amber-violet focus tone appears. Clear targeting feedback, no music, no voice, no electronic lock-on beep.
```

### SFX_REWIND_START

- Duration: `0.8s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated start of object rewind magic, nearby ash inhales, tiny stone and iron fragments reverse toward their origin and a low amber-violet pressure begins. Intimate gothic magic, no music, no voice, no explosion, no sci-fi sweep.
```

### SFX_REWIND_LOOP

- Duration: `3s`
- Looping: `ON`
- Prompt Influence: `48%`

```text
Perfectly seamless object-rewind channel loop, quiet reversed ash, small stone grains and iron dust continuously drawing inward around a restrained amber-violet harmonic pressure. No pulse, no music, no voice, no climax, no audible restart.
```

### SFX_REWIND_CANCEL

- Duration: `0.65s`
- Looping: `OFF`
- Prompt Influence: `55%`

```text
Isolated interrupted rewind, inward-moving fragments lose tension, scatter slightly and the amber-violet channel tone collapses without damage. Clear cancellation feedback, no music, no voice, no error beep, no explosion.
```

### SFX_REWIND_COMPLETE

- Duration: `1.5s`
- Looping: `OFF`
- Prompt Influence: `58%`

```text
Isolated completion of object rewind, reversed fragments lock into a restored form, one restrained stone-and-iron contact lands and a warm amber memory harmonic resolves imperfectly. Satisfying but uneasy, no music, no voice, no triumphant flourish.
```

### SFX_REWIND_STONE

- Duration: `2.5s`
- Looping: `OFF`
- Prompt Influence: `62%`

```text
Isolated restoration of a broken gothic stone structure in reverse, rubble lifts, grit streams upward, cracks close and heavy pieces reconnect with controlled mineral pressure. Physical and uncanny, no music, no voice, no destruction explosion.
```

### SFX_REWIND_CHAIN

- Duration: `2s`
- Looping: `OFF`
- Prompt Influence: `64%`

```text
Isolated restoration of a broken ancient chain in reverse, loose links rise, scrape backward, reconnect one by one and regain tension with a restrained low iron resonance. Gothic and physical, no music, no voice, no huge clang.
```

### SFX_REWIND_GATE

- Duration: `2.8s`
- Looping: `OFF`
- Prompt Influence: `64%`

```text
Isolated rewind restoration of a ruined black-iron gate, bent bars reverse, broken hinges reassemble, chain tension returns and the full structure locks into an imperfect restored state. Uncanny and heavy, no music, no voice, no modern machine.
```

---

# 10. 실제 생성 순서

## 첫 번째 생성 묶음 — 조작감 검증

1. `SFX_PLAYER_STEP_STONE`
2. `SFX_PLAYER_RUN_STONE`
3. `SFX_PLAYER_JUMP`
4. `SFX_PLAYER_LAND_LIGHT`
5. `SFX_PLAYER_LAND_HEAVY`
6. `SFX_PLAYER_DASH`
7. `SFX_PLAYER_ATTACK_SWING`
8. `SFX_HIT_ASH_FLESH`
9. `SFX_PLAYER_HURT`
10. `SFX_PLAYER_DEATH`

이 10개를 먼저 적용한 뒤 나머지를 생성한다. 캐릭터 조작이 둔하게 느껴지면 음량을 올리기 전에
효과음 앞부분의 무음과 불필요한 꼬리를 먼저 자른다.

## 두 번째 생성 묶음 — 재질과 이동 확장

- 금속·다리·섬유질 걷기와 달리기
- 벽 잡기·벽 미끄러짐·벽 점프
- 공격 적중 재질 4종
- 회복·부활·저체력

## 세 번째 생성 묶음 — 콘텐츠 확장

- 일반 적
- 아이템·체크포인트·문·숏컷
- UI
- 보스
- 되감기

## 11. 파일 선별 기준

ElevenLabs가 출력한 네 결과 중 다음 조건을 가장 잘 만족하는 결과를 고른다.

1. 시작 무음이 짧다.
2. 행동 한 번만 들린다.
3. 음악이나 불필요한 배경음이 없다.
4. 다른 효과음과 겹쳐도 핵심 접촉음이 남는다.
5. 과도하게 영화적이거나 현실적인 큰 소리가 아니다.
6. 반복 변형끼리 음색과 체감 크기가 비슷하다.

발소리와 공격음은 좋은 결과를 3~4개 모두 사용한다. 체크포인트, 문 개방, 사망처럼 자주 반복되지
않는 소리는 가장 좋은 결과 하나만 사용해도 된다.

## 12. 완료 점검표

- [ ] 걷기와 달리기가 음량뿐 아니라 접촉 강도로 구분된다.
- [ ] 돌·금속·다리·섬유질 표면이 화면을 보지 않고도 구분된다.
- [ ] 점프·대시·착지 소리가 애니메이션 프레임과 일치한다.
- [ ] 공격 헛스윙과 적중을 즉시 구분할 수 있다.
- [ ] 플레이어 피격음이 적 피격음보다 우선적으로 들린다.
- [ ] 모든 위험 공격에 실제 타격보다 먼저 들리는 예고음이 있다.
- [ ] 같은 발소리 파일이 연속 재생되지 않는다.
- [ ] 반복음의 시작과 끝에서 클릭이나 페이드가 들리지 않는다.
- [ ] 체크포인트·기억 파편·숏컷 개방의 중요도가 서로 다르게 들린다.
- [ ] 되감기의 시작·지속·취소·완료를 화면 없이 구분할 수 있다.
