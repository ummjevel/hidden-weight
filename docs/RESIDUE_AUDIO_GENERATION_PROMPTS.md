# 잔재 1맵 오디오 생성 프롬프트

> 방향: 계층형 오디오 구성  
> 음악: 희미하고 슬픈 기억 테마가 탐험 중에는 거의 들리지 않다가 보스전에서 선명해짐  
> 지역 질감: 검은 철골, 거대한 손가락 폐허, 사슬, 재, 죄책감, 잘못 복원된 과거

> 기본 조작음부터 실제 제작하는 순서와 ElevenLabs 개별 설정은
> [잔재 1맵 효과음 전체 구성·생성 계획](./RESIDUE_SFX_PRODUCTION_PLAN.md)을 우선 참고한다.

## 0. 사용 방법

### 음악 생성 AI

- 아래 `MUS_*` 프롬프트는 각각 독립적으로 복사한다.
- 보컬과 가사는 사용하지 않는다.
- 루프용 음악은 시작과 끝에 큰 충격음이나 긴 페이드가 없어야 한다.
- 가능하면 WAV, 48 kHz, 24-bit로 출력한다.
- 탐험곡과 레이어는 동일한 BPM과 조성을 유지한다.

### 효과음 생성 AI

아래 공통 문장을 각 `SFX_*` 프롬프트 앞에 붙인다.

```text
Create an isolated production-ready sound effect for a dark gothic cosmic-horror 2D metroidvania. Dry and detailed, restrained rather than cinematic, no music, no voice-over, no dialogue, no modern machinery, no gunshot, no trailer boom, no excessive reverb, no clipping. Leave a clean short tail. Output WAV, 48 kHz, 24-bit.
```

반복음에는 다음 문장을 추가한다.

```text
Make it a perfectly seamless loop with no audible click, fade, or obvious restart.
```

변형이 필요한 프롬프트에는 생성 AI가 지원하면 한 번에 여러 결과를 요청하고, 지원하지 않으면
같은 프롬프트로 3~4번 생성한다.

---

# 1. 배경 음악·연출 음악

## MUS_RESIDUE_EXPLORATION

```text
Instrumental seamless exploration music for a dark gothic cosmic-horror metroidvania region called Residue, representing guilt and an unresolved past. 54 BPM, D minor, very sparse. A low cello-like drone, distant bowed iron, muted piano notes with long gaps, soft sub-bass breathing, and an incomplete five-note memory motif that never resolves. Add a faint broken pulse that occasionally omits a beat, as if the music has forgotten part of itself. Oppressive but mournful, suitable for 30 minutes of exploration without fatigue. No vocals, no choir, no heroic melody, no jump scares, no loud percussion, no modern synth arpeggio. 3-minute seamless loop, stable loudness, loop point must be invisible.
```

## MUS_RESIDUE_LOWER_LAYER

```text
Create a seamless lower-ruins music layer designed to play over a 54 BPM D minor exploration track. Mostly low stone resonance, filtered contrabass friction, granular ash movement, and a distant irregular two-beat iron knock. Preserve large areas of silence. It must add claustrophobia without introducing a new melody or changing harmony. No vocals, no bright percussion, no bass drop. 3-minute seamless loop with transparent beginning and ending.
```

## MUS_RESIDUE_UPPER_LAYER

```text
Create a seamless upper-tower tension layer at 54 BPM in D minor, compatible with a sparse gothic exploration score. Thin metallic bowing, chain harmonics, restrained low frame-drum pulses with deliberately missing beats, and cold high wind tones. Increase vertical exposure and danger without becoming combat music. No melody, no vocals, no cymbal crashes, no trailer sound. 3-minute seamless loop.
```

## MUS_REWIND_SHRINE

```text
Instrumental seamless shrine ambience for discovering the power to rewind broken objects. 54 BPM, D minor, using the same incomplete five-note memory motif as the Residue exploration theme, now played by fragile music-box-like metal and soft bowed glass over a warm amber drone. Beautiful but uneasy, as if the past is being restored incorrectly. Sparse, sacred, intimate, no choir, no vocals, no triumphant resolution, no drums. 90-second seamless loop.
```

## MUS_REWIND_UNLOCK_STINGER

```text
An 8-second instrumental ability-unlock cue for gaining rewind power in a gothic cosmic-horror game. Begin with reversed stone fragments and chains inhaling toward the listener, reveal a clear but incomplete five-note D minor memory motif on dark piano and bowed metal, then end with one deep restrained impact and a lingering amber-violet harmonic. Emotional and significant, not triumphant, no vocals, no choir, no trailer braam.
```

## MUS_WRIST_WATCHER_BATTLE

```text
Instrumental seamless mid-boss battle music for the Wrist Watcher, a towering black-iron guardian in a ruined hand-shaped city. 84 BPM, D minor. Develop the same five-note memory motif from the exploration theme using aggressive low strings, struck iron, chain percussion, and an uneven pulse that alternates between complete and missing beats. Heavy and mechanical but still mournful. Clear attack rhythm without becoming fast metal or orchestral trailer music. No vocals, no choir, no brass fanfare. 2-minute seamless loop.
```

## MUS_WRIST_WATCHER_VICTORY

```text
A restrained 6-second mid-boss victory cue in D minor. The mechanical chain rhythm abruptly collapses, the five-note memory motif plays once on a damaged piano but loses its final note, followed by a low iron resonance fading into wind. No triumph, no vocals, no cymbal swell.
```

## MUS_HALL_OF_REGRET

```text
Near-silent instrumental ambience for a corridor before the final boss in a gothic cosmic-horror game. No conventional rhythm. Extremely low room tone, one distant bowed-metal harmonic every 12 to 18 seconds, occasional almost-inaudible reversed piano resonance, and a fragment of the five-note memory motif that never fully emerges. Mournful, exposed, psychologically heavy. No vocals, no heartbeat, no jump scare. 90-second seamless loop.
```

## MUS_MEMORY_INSTRUCTOR_PHASE1

```text
Instrumental seamless final-boss music for the Memory Instructor, a giant gallows-and-chain entity teaching the player through punishment. 72 BPM, D minor. The established five-note memory motif is now clearly audible on low piano and cello, surrounded by ritual chain percussion, bowed iron, and a slow funeral pulse. Tragic rather than evil, controlled tension with room for combat sounds. No vocals, no choir, no heroic brass, no constant wall of sound. 2-minute seamless loop.
```

## MUS_MEMORY_INSTRUCTOR_PHASE2_LAYER

```text
Create a synchronized phase-two combat layer for a 72 BPM D minor final-boss track. Add broken double-time chain ticks, dissonant bowed-metal overtones, deeper sub pulses, and fractured repetitions of the established five-note memory motif. It must align over the phase-one music without changing tempo or root key. More desperate and unstable, but not louder than the main track. No vocals, no choir, no trailer percussion. 2-minute seamless loop.
```

## MUS_MEMORY_INSTRUCTOR_VICTORY

```text
A 10-second final-boss defeat cue in D minor. Gallows chains lose tension and fall silent, the five-note memory motif plays slowly and completely for the first time on soft piano and cello, but the final chord remains open and unresolved. End with quiet ash and distant wind. Sad relief, no triumph, no vocals, no large cinematic hit.
```

## MUS_RESIDUE_EXIT

```text
A 12-second transition cue from the dark Residue region toward a more exposed and watchful next region. Begin with the completed D minor memory motif in low piano, let the final note stretch into a thin violet harmonic and distant breathing texture, then leave two seconds of near silence. No vocals, no percussion climax, no hard ending.
```

---

# 2. 공간 환경음

## 반복 환경음

### AMB_ENTRY_BRIDGE

```text
Seamless ambience loop for a vast ruined bridge inside a black-iron city built around gigantic fingers: low cold wind, distant chain sway, sparse ash grains, huge empty vertical space, and one barely audible structural groan far away. Dark and spacious, no melody, no obvious repeating event. 90 seconds.
```

### AMB_LOWER_RUINS

```text
Seamless ambience loop for buried lower ruins: muffled air, close stone pressure, fine dust trickling through cracks, rare distant rubble movement, almost no wind. Claustrophobic and dry, no creatures, no melody, no obvious repetition. 90 seconds.
```

### AMB_INSIDE_FINGERS

```text
Seamless ambience loop inside an impossible gigantic finger-shaped structure: low fibrous friction mixed with stone and iron resonance, slow tendon-like pressure, distant chain tension, and subtle hollow pulses that might be architecture or a living body. Non-gory, restrained, no heartbeat cliché, no voice. 90 seconds.
```

### AMB_LIFT_SHAFT

```text
Seamless ambience loop for a tall gothic lift shaft: vertical wind, long metallic resonance, lightly vibrating suspended chains, distant pulley movement from far above, and occasional tiny stone dust. Deep sense of height, no rhythmic machine loop, no melody. 90 seconds.
```

### AMB_UPPER_TOWER

```text
Seamless ambience loop for an exposed upper watchtower: stronger cold wind through iron ribs, several distant chain lengths moving at different speeds, faint cage creaks, and far below city resonance. Wide and dangerous, no thunder, no melody. 90 seconds.
```

### AMB_GALLOWS

```text
Seamless ambience loop for a colossal memory gallows boss arena: extremely low sub resonance, hanging chains under constant tension, slow wood-and-iron stress, ash moving across stone, and unnaturally suppressed wind. Ritualistic and oppressive, no voices, no music. 90 seconds.
```

### AMB_SECRET_ROOM

```text
Seamless ambience loop for a hidden memory chamber: muffled external world, high-frequency air almost removed, faint stone resonance, a soft backward-moving dust texture, and one ambiguous distant eye-like wet-stone movement every long interval. Intimate and uncanny, non-gory, no whisper words, no melody. 60 seconds.
```

## 무작위 환경 단발음

### AMB_ONESHOT_CHAIN_CREAK

```text
One isolated heavy suspended chain giving a slow uneven creak and two small link movements, distant medium-sized gothic space. Generate 4 natural variations, 1.5 to 3 seconds each.
```

### AMB_ONESHOT_CAGE_SWAY

```text
An empty iron cage gently swings once, old hooks and thin bars complaining softly, then settles. Generate 3 restrained variations, 2 to 4 seconds.
```

### AMB_ONESHOT_PEBBLES

```text
Three to six tiny charcoal pebbles break loose, fall through a tall space, strike stone at different distances, and stop. Generate 4 variations, no large collapse.
```

### AMB_ONESHOT_DISTANT_COLLAPSE

```text
A large iron-and-stone structure collapses very far away in a cavernous city, mostly low rumble and delayed metal resonance, never a close explosion. Generate 3 variations, 4 to 7 seconds.
```

### AMB_ONESHOT_FINGER_GROAN

```text
A gigantic finger-shaped structure subtly bends somewhere far away: deep fibrous stone friction, stretched iron mesh, and a low non-animal groan. Non-gory and ambiguous, 4 to 6 seconds. Generate 3 variations.
```

### AMB_ONESHOT_MEMORY_WHISPER

```text
An extremely faint human-like memory texture with no intelligible words, reversed breath and distant room resonance, gender and age ambiguous, unsettling but quiet. Generate 4 variations, 2 to 4 seconds.
```

### AMB_ONESHOT_BELL

```text
One very distant damaged bronze bell strike with an incomplete, detuned decay swallowed by wind. No church melody. Generate 3 variations, 5 to 8 seconds.
```

### AMB_ONESHOT_PUDDLE_DROP

```text
One small drop hits a shallow black puddle on stone, close and delicate, with a very short dark room reflection. Generate 4 subtle variations.
```

### AMB_ONESHOT_WINDOW_DIE

```text
A tiny distant amber lamp inside an iron tower sputters twice and extinguishes, with faint glass and metal resonance. Generate 3 variations, 1 to 2 seconds.
```

### AMB_ONESHOT_DISTANT_FOOTSTEP

```text
One impossibly heavy footstep or structural impact occurs far across a giant bridge, followed by a delayed chain tremor. Ambiguous source, no monster vocal. Generate 3 variations, 3 to 5 seconds.
```

---

# 3. 플레이어 효과음

### SFX_PLAYER_STEP_STONE

```text
Light small-character footsteps on dusty dark stone, soft cloth and tiny grit, agile but cautious. Generate 4 distinct short variations, each under 0.35 seconds.
```

### SFX_PLAYER_STEP_METAL

```text
Light footsteps on old hollow gothic iron plating, restrained metallic tick with short dark resonance. Generate 4 distinct variations, each under 0.4 seconds.
```

### SFX_PLAYER_STEP_BRIDGE

```text
Light footsteps on a suspended stone-and-chain bridge, subtle deck click and tiny chain response. Generate 4 distinct variations, each under 0.45 seconds.
```

### SFX_PLAYER_STEP_FINGER

```text
Light footsteps on a strange surface that is both ancient stone and dense fibrous material, dry and non-gory. Generate 4 distinct variations, each under 0.4 seconds.
```

### SFX_PLAYER_JUMP

```text
A small agile jump: quick cloth lift, soft airy impulse, tiny violet magical tail, no cartoon boing. 0.35 seconds.
```

### SFX_PLAYER_LAND_LIGHT

```text
Light landing on dusty stone with cloth movement and a small ash puff. Generate 3 variations, under 0.5 seconds.
```

### SFX_PLAYER_LAND_HEAVY

```text
Hard landing on dark stone, compact body impact, stone grit and short low resonance, no superhero slam. Generate 2 variations, under 0.8 seconds.
```

### SFX_PLAYER_DASH_START

```text
Fast supernatural dash start: compressed air, reversed cloth snap, and a thin midnight-violet streak, sharp but not sci-fi. 0.3 seconds.
```

### SFX_PLAYER_DASH_PASS

```text
Short airy dash pass-by with cloth and restrained violet granular trail, no engine sound. 0.45 seconds.
```

### SFX_PLAYER_DASH_END

```text
Small dash stop with soft displaced air, shoe scrape, and quickly collapsing magical residue. 0.35 seconds.
```

### SFX_PLAYER_WALL_CLING

```text
Small hands and shoes catch on rough gothic stone, brief scrape with falling grit. Generate 3 variations, under 0.5 seconds.
```

### SFX_PLAYER_WALL_SLIDE_LOOP

```text
Seamless short loop of controlled cloth and shoe friction sliding down rough stone, sparse falling grit, not harsh. 1.5 seconds.
```

### SFX_PLAYER_WALL_JUMP

```text
Quick push away from stone wall, shoe impact, small grit burst and agile air movement. Generate 2 variations, under 0.5 seconds.
```

### SFX_PLAYER_ATTACK_SWING

```text
Small fast melee weapon or energy-edged swing, cloth-assisted arc with restrained dark-violet air cut. Generate 4 variations, 0.25 to 0.45 seconds, no impact included.
```

### SFX_PLAYER_ATTACK_HIT

```text
Clean melee hit against an ash-and-iron creature: compact stone crack, small metal scrape and dark particulate burst. Generate 4 variations, under 0.5 seconds.
```

### SFX_PLAYER_HURT

```text
Player damage response without spoken vocal: short cloth/body impact, breath-like nonverbal exhale and violet energy fracture. Generate 3 variations, under 0.6 seconds.
```

### SFX_PLAYER_DEATH

```text
Player death effect: body dissolves into soft ash and fractured violet memory particles, low emotional tone, no scream, no large explosion. 2 to 3 seconds.
```

### SFX_PLAYER_RESPAWN

```text
Player reforms at a checkpoint: ash flows backward, small cloth movement, warm amber memory pulse and quiet final materialization. 2 seconds.
```

### SFX_PLAYER_HEAL

```text
Compact healing sound: warm amber reliquary opens, low glass harmonic rises, soft breath releases, then a clean gentle confirmation. 1.2 seconds.
```

### SFX_PLAYER_LOW_HEALTH_LOOP

```text
Subtle seamless low-health loop: muted internal pressure pulse and faint cloth breath, psychologically tense but quieter than combat, no literal loud heartbeat. 4 seconds.
```

---

# 4. 되감기 능력

### SFX_REWIND_TARGET_FOUND

```text
Short target-acquired cue for a rewindable object: two tiny stone grains move backward into place and an amber harmonic focuses. Clear but unobtrusive, 0.35 seconds.
```

### SFX_REWIND_TARGET_SWITCH

```text
Very short rewind target-switch tick: reversed metal fleck and soft amber click, 0.15 seconds. Generate 3 variations.
```

### SFX_REWIND_CHANNEL_START

```text
Rewind channel begins: surrounding air inhales, loose ash reverses direction, low amber-violet tone opens. No impact, 0.8 seconds.
```

### SFX_REWIND_CHANNEL_LOOP

```text
Perfectly seamless rewind channel loop: granular stone moving backward, stretched chain harmonics, low amber-violet oscillation, slowly increasing internal tension without changing overall volume. 3 seconds.
```

### SFX_REWIND_THRESHOLD

```text
Rewind channel reaches completion threshold: focused rising harmonic, three reverse clicks locking into alignment, restrained anticipation. 0.7 seconds.
```

### SFX_REWIND_CANCEL

```text
Rewind channel collapses before completion: reversed particles lose cohesion and fall normally, low tone folds inward. No failure buzzer, 0.6 seconds.
```

### SFX_REWIND_NO_TARGET

```text
Soft unusable rewind response: empty reversed breath and one dull stone tick, clear but not annoying. 0.4 seconds.
```

### SFX_REWIND_COMPLETE

```text
General rewind completion: many small fragments snap backward into exact positions followed by one deep restrained normal-direction locking impact and a warm amber resonance. 1.4 seconds.
```

### SFX_REWIND_STONE

```text
Broken gothic masonry reconstructs backward: falling stones reverse upward, dust sucks into seams, blocks interlock, then a heavy stable stone lock. 2.5 seconds.
```

### SFX_REWIND_CHAIN

```text
Loose heavy chains rewind into tension: links race backward through iron guides, slack disappears, and the final hook locks firmly. 2 seconds.
```

### SFX_REWIND_BRIDGE

```text
A shattered chain bridge reconstructs backward: stone deck fragments return, chains thread and tighten, final deck flex and restrained settling groan. 3 seconds.
```

### SFX_REWIND_LIFT

```text
A broken gothic lift mechanism restores backward: gears unbreak, chain rewinds through a pulley, wheel aligns, amber power returns, final mechanical lock. 3 seconds.
```

### SFX_REWIND_GATE

```text
A ruined iron gate restores backward: bars straighten, fragments reconnect, chains pull into correct routes, amber lock engages. 2.5 seconds.
```

### SFX_REWIND_READY

```text
Very subtle rewind ability cooldown-ready cue: one warm amber harmonic and a tiny backward stone click. Under 0.35 seconds.
```

---

# 5. 일반 적

## 잔재 보행자

### SFX_WALKER_IDLE

```text
Idle sound of a humanoid creature made from compressed ash, cracked stone and small iron pieces: dry shifting weight, tiny internal ember crackle. Generate 4 variations, no voice.
```

### SFX_WALKER_NOTICE

```text
Ash-and-stone walker notices the player: body stiffens, grit pulls inward, one low hollow resonance. 0.7 seconds.
```

### SFX_WALKER_STEP

```text
Heavy uneven creature step made of compact ash and stone on masonry. Generate 4 variations, under 0.5 seconds.
```

### SFX_WALKER_TELEGRAPH

```text
Ash walker prepares a short melee strike: stone shoulder twists, grit compresses, brief readable scrape. 0.6 seconds.
```

### SFX_WALKER_ATTACK

```text
Ash walker performs a short heavy arm strike, dry stone arc followed by compact ground-level impact. Generate 3 variations, under 0.8 seconds.
```

### SFX_WALKER_HIT

```text
Ash walker is hit: compact stone crack, loose ash burst and tiny iron fragment. Generate 3 variations, under 0.5 seconds.
```

### SFX_WALKER_DEATH

```text
Ash walker collapses into a low pile of charcoal debris and faint dying embers, no vocal. 1.5 seconds.
```

## 매달린 손가락

### SFX_FINGER_IDLE

```text
An unnatural long finger creature hangs from a ceiling: tiny joint tension, dry tendon-like stone friction and chain movement. Generate 4 quiet variations, non-gory.
```

### SFX_FINGER_NOTICE

```text
Hanging finger detects the player: joints lock in sequence from top to bottom and dust stops falling. 0.6 seconds.
```

### SFX_FINGER_CRAWL

```text
Long iron-bone finger crawls using sharp joints on stone, irregular delicate taps and scraping. Generate 4 variations.
```

### SFX_FINGER_DROP_TELEGRAPH

```text
Ceiling finger prepares to drop: chain goes taut, joints curl inward, tiny stones fall below. Clear 0.8-second warning.
```

### SFX_FINGER_DROP

```text
Long finger creature releases from ceiling and cuts rapidly through air, dry joint rattle, no impact. 0.5 seconds.
```

### SFX_FINGER_IMPACT

```text
Iron-bone finger strikes stone floor point-first, sharp stone crack and short chain recoil. Generate 3 variations, under 0.7 seconds.
```

### SFX_FINGER_DEATH

```text
Finger creature joints disconnect and fall as dry bone-like iron segments, then dissolve into ash. Non-gory, 1.5 seconds.
```

## 애도 운반자

### SFX_CARRIER_IDLE

```text
Large mourning carrier wrapped in heavy funeral cloth: fabric drag, hidden frame creak, restrained low breath-like resonance without a voice. Generate 4 variations.
```

### SFX_CARRIER_NOTICE

```text
Mourning carrier turns toward the player, funeral cloth pulls tight over a rigid frame and chain ornaments stop moving. 0.8 seconds.
```

### SFX_CARRIER_STEP

```text
Heavy measured steps with dragging funeral cloth and small hanging chains. Generate 4 variations.
```

### SFX_CARRIER_CHARGE_TELEGRAPH

```text
Mourning carrier prepares a long charge: cloth stretches violently backward, feet grind into stone, low pressure builds. Clear 1-second warning.
```

### SFX_CARRIER_CHARGE_LOOP

```text
Short seamless loop of a heavy shrouded creature charging across stone, rushing cloth, repeated heavy foot impacts and chain vibration. 1.5 seconds.
```

### SFX_CARRIER_WALL_IMPACT

```text
Massive shrouded carrier slams into a restored stone wall: deep compact impact, masonry fracture, cloth snaps forward, chains scatter. 1 second.
```

### SFX_CARRIER_DEATH

```text
Mourning carrier loses internal support, funeral cloth collapses empty onto stone, frame and chains fall inside it. Sad and heavy, no vocal, 2 seconds.
```

## 굳은 잔재

### SFX_HARDENED_IDLE

```text
Armored residue guardian idles: dense stone plates settle, internal iron shield resonates faintly. Generate 4 variations.
```

### SFX_HARDENED_NOTICE

```text
Armored residue guardian raises its defense: stone plates slide together and one deep iron lock engages. 0.8 seconds.
```

### SFX_HARDENED_STEP

```text
Very heavy armored stone-and-iron footsteps with short floor resonance. Generate 4 variations.
```

### SFX_HARDENED_BLOCK

```text
Melee strike is blocked by thick gothic iron and stone armor: hard metallic stop, stone grit, short low ring. Generate 4 variations.
```

### SFX_HARDENED_TELEGRAPH

```text
Armored guardian raises a heavy weapon for a slow attack, plates grind and weight shifts backward. Clear 1-second warning.
```

### SFX_HARDENED_ATTACK

```text
Armored guardian delivers a slow crushing strike, heavy iron arc and compact stone impact. Generate 3 variations.
```

### SFX_HARDENED_DEATH

```text
Armored residue guardian fractures from inside, heavy plates separate and fall one after another, final low iron collapse. 2 seconds.
```

---

# 6. 손목의 감시자

### SFX_WRIST_INTRO

```text
A towering black-iron wrist guardian awakens: multiple internal amber locks ignite from bottom to top, long arms unfold, giant blade drags free. 3 seconds.
```

### SFX_WRIST_IDLE

```text
Short seamless idle loop for a giant iron guardian: slow internal gear pressure, hanging chain movement and faint amber core pulse. 3 seconds.
```

### SFX_WRIST_STEP

```text
Huge narrow iron guardian step on a tower floor, long limb movement and deep focused impact. Generate 4 variations.
```

### SFX_WRIST_SWEEP_TELEGRAPH

```text
Giant blade sweep warning: long segmented arm extends, blade scrapes lightly across stone, chain tension rises. Clear 1-second telegraph.
```

### SFX_WRIST_SWEEP

```text
Enormous curved iron blade sweeps horizontally through air, layered heavy whoosh and sharp metallic edge, no impact. Generate 3 variations.
```

### SFX_WRIST_CHARGE_TELEGRAPH

```text
Wrist guardian charge warning: feet lock, torso amber core compresses, chains pull backward and blade points forward. 1 second.
```

### SFX_WRIST_CHARGE_LOOP

```text
Short seamless loop of a giant iron guardian charging across stone, rapid long-limbed impacts, chain vibration and heavy air movement. 1.5 seconds.
```

### SFX_WRIST_WALL_IMPACT

```text
Giant iron guardian crashes blade-first into a tower wall: deep iron impact, masonry burst, long blade resonance and chain recoil. 1.5 seconds.
```

### SFX_WRIST_DROP_WARNING

```text
Overhead boss drop warning: distant chain release above, descending metallic whistle and small stones trembling on the floor. 1 second.
```

### SFX_WRIST_DROP_IMPACT

```text
Towering iron guardian lands heavily on a circular stone arena, focused floor shock, chain splash and brief low resonance. 1.2 seconds.
```

### SFX_WRIST_HURT

```text
Giant iron guardian takes damage: armor plate crack, amber core sputter and low mechanical recoil. Generate 4 variations.
```

### SFX_WRIST_STUN

```text
Wrist guardian enters impact stun: internal mechanisms skip and wind down, blade tip drops onto stone, amber core becomes unstable. 1.5 seconds.
```

### SFX_WRIST_DEATH

```text
Giant wrist guardian death: amber mechanisms fail in sequence, long limbs lose tension, blade and chains collapse across stone, final core extinguishes. 4 seconds, no explosion.
```

---

# 7. 기억의 교수자

### SFX_INSTRUCTOR_INTRO

```text
Colossal gallows entity awakens: many suspended chains pull taut across a huge arena, iron arms rotate from sockets, caged memory core opens with a low violet resonance. 4 seconds.
```

### SFX_INSTRUCTOR_CORE_LOOP

```text
Perfectly seamless low loop of a giant caged memory core: slow amber-violet pressure pulse, restrained iron vibration, granular memories moving backward. 3 seconds.
```

### SFX_INSTRUCTOR_ARM_MOVE

```text
Massive blade or hook arm rotates around an ancient iron socket, layered bearing grind and chain-assisted weight movement. Generate 4 variations.
```

### SFX_INSTRUCTOR_CHAIN_WARNING

```text
Boss chain-slam warning: overhead gallows chain rapidly draws taut, links align one by one, hook trembles under extreme pressure. Clear 1.2-second telegraph.
```

### SFX_INSTRUCTOR_CHAIN_DROP

```text
Very heavy hooked chain drops rapidly through a huge vertical space, escalating link rattle and dark air displacement, no impact. 0.8 seconds.
```

### SFX_INSTRUCTOR_CHAIN_IMPACT

```text
Colossal hooked chain slams into stone arena floor, concentrated iron impact, radial masonry crack and short low shock. Generate 3 variations.
```

### SFX_INSTRUCTOR_BLADE_SWEEP

```text
Massive segmented gallows blade sweeps across a boss arena, slow heavy air pressure with a sharp iron edge. Generate 3 variations, no impact.
```

### SFX_INSTRUCTOR_HOOK_PULL

```text
Large hook catches and drags across cracked stone, heavy chain pulling, sparks restrained and dark. 1.5 seconds.
```

### SFX_INSTRUCTOR_PLATFORM_BREAK

```text
Boss safety platform breaks apart under chain pressure: gothic masonry splits, suspended supports snap, pieces fall into a deep void. 2 seconds.
```

### SFX_INSTRUCTOR_PLATFORM_REWIND

```text
Boss safety platform rewinds into place: fragments rise from a void, iron supports reconnect backward, final protected platform locks firmly. 2.5 seconds.
```

### SFX_INSTRUCTOR_PHASE_WARNING

```text
Final boss phase-transition warning: every chain in the arena becomes silent and taut, memory core frequency rises, small stones lift from the floor. 2 seconds.
```

### SFX_INSTRUCTOR_PHASE_RUPTURE

```text
Final boss arena seal ruptures: violet memory pressure bursts through circular stone seams, iron ribs open, chains recoil outward, then sound drops suddenly. 2.5 seconds, no trailer explosion.
```

### SFX_INSTRUCTOR_HURT

```text
Colossal gallows entity takes damage: iron socket cracks, chain tension slips and violet core distorts. Generate 4 variations.
```

### SFX_INSTRUCTOR_DEATH

```text
Colossal gallows entity dies without exploding: core pulse becomes irregular and stops, chains lose tension one group at a time, giant arms hang inert, final hook touches stone in near silence. 6 seconds.
```

---

# 8. 체크포인트·아이템·지형·장치

### SFX_CHECKPOINT_ACTIVATE

```text
Gothic memory checkpoint activates: black iron petals open, amber crystal ignites from darkness, small memory motes rise, final warm lock. 2 seconds.
```

### SFX_CHECKPOINT_IDLE_LOOP

```text
Perfectly seamless quiet checkpoint loop: contained amber flame, tiny glass harmonic and sparse memory particles, unobtrusive under music. 4 seconds.
```

### SFX_CHECKPOINT_HEAL

```text
Checkpoint restores the player: warm amber pulse expands, soft reversed ash gathers, low reassuring harmonic settles. 1.5 seconds.
```

### SFX_CURRENCY_IDLE

```text
Perfectly seamless tiny currency idle loop: small charcoal stones with amber cores lightly resonate and glint, almost silent. 3 seconds.
```

### SFX_CURRENCY_PICKUP

```text
Small charcoal currency stones pull rapidly toward the player, three compact amber clicks and soft confirmation. Generate 4 variations, under 0.6 seconds.
```

### SFX_HEALING_PICKUP

```text
Chained healing reliquary is collected: chain releases, warm amber glass tone rises and dissolves into the player. 0.9 seconds.
```

### SFX_HEALTH_SHARD_PICKUP

```text
Maximum-health shard collection: black iron petals open, amber crystal rings with a deeper lasting harmonic, restrained permanent-upgrade confirmation. 1.5 seconds.
```

### SFX_MEMORY_FRAGMENT_IDLE

```text
Perfectly seamless memory-fragment idle loop: fragile amber crystal, quiet violet internal flicker and faint reversed whisper texture with no words. 4 seconds.
```

### SFX_MEMORY_FRAGMENT_PICKUP

```text
Memory fragment enters the player: crystal cracks without breaking, reversed piano resonance reveals one note of the memory motif, then soft violet absorption. 1.8 seconds.
```

### SFX_LIFT_START

```text
Ancient gothic lift starts: amber lock engages, massive wheel overcomes resistance, chains pull taut, platform gives one heavy jerk. 1.5 seconds.
```

### SFX_LIFT_LOOP

```text
Perfectly seamless lift movement loop: slow giant pulley rotation, consistent heavy chain travel, suspended platform vibration, restrained iron resonance. 3 seconds.
```

### SFX_LIFT_STOP

```text
Ancient lift stops: wheel decelerates, chain tension shifts, platform settles twice, final mechanical lock. 1.5 seconds.
```

### SFX_GATE_UNLOCK

```text
Gothic shortcut gate unlocks: amber seal releases, two heavy chain locks withdraw, iron bars loosen. 1.2 seconds.
```

### SFX_GATE_OPEN

```text
Tall heavy gothic iron gate rises through ancient guides, chain pull and deep metal scrape, ending fully open. 2.5 seconds.
```

### SFX_GATE_CLOSE

```text
Tall gothic iron gate descends, restrained heavy guide scrape, final firm lock without explosive slam. 2 seconds.
```

### SFX_BRIDGE_SETTLE

```text
Restored chain bridge accepts weight: deck flexes once, several chain lengths tighten at different times, loose links settle. Generate 3 variations, 1 to 2 seconds.
```

### SFX_SPIKES_WARNING

```text
Floor spike trap warning: hidden iron springs compress and stone slots click open. Clear 0.45-second warning.
```

### SFX_SPIKES_EXTEND

```text
Row of gothic iron spikes extends rapidly from stone slots, sharp mechanical thrust and short metallic stop. Generate 3 variations.
```

### SFX_TENDRIL_WARNING

```text
Void tendril attack warning: dark fibers gather below the floor and pull ash inward, unsettling but non-gory. 0.7 seconds.
```

### SFX_TENDRIL_ATTACK

```text
Several dark iron-organic tendrils lash upward, dry fibrous whip and small stone scatter. Generate 3 variations.
```

### SFX_CRUSHER_WARNING

```text
Large gothic crusher warning: overhead chains tighten, gear tooth skips, dust falls from directly above. Clear 0.8-second telegraph.
```

### SFX_CRUSHER_IMPACT

```text
Massive iron-and-stone crusher closes against the floor, deep compact collision, masonry grit and short chain recoil. Generate 3 variations.
```

### SFX_FLOOR_CRACK

```text
Suspended gothic floor begins to fail: three progressive stone cracks, tiny debris falls into a void. 1 second.
```

### SFX_FLOOR_COLLAPSE

```text
Suspended stone platform collapses into a deep space, supports snap, masonry separates and falling debris recedes downward. 2 seconds.
```

### SFX_SECRET_EYE_REVEAL

```text
Hidden eye relief appears inside a stone wall under awareness: stone texture peels apart acoustically, thin violet harmonic focuses, one heavy lid-like slab shifts. Uncanny, non-gory, 1.8 seconds.
```

### SFX_SECRET_EYE_OPEN

```text
Ancient eye embedded in iron-and-stone wall slowly opens: layered stone plates slide like an eyelid, wetness extremely subtle, violet memory resonance emerges. Non-gory, 2 seconds.
```

---

# 9. UI·시스템

### SFX_UI_MOVE

```text
Minimal gothic menu navigation tick: tiny muted iron and soft ash texture, dark but clear. Generate 4 variations, under 0.15 seconds.
```

### SFX_UI_CONFIRM

```text
Minimal menu confirmation: compact amber glass tone and soft iron lock, under 0.35 seconds.
```

### SFX_UI_CANCEL

```text
Minimal menu cancel: short reversed ash movement and muted descending metal tone, under 0.3 seconds.
```

### SFX_UI_PAUSE

```text
Game pause sound: surrounding ambience folds inward, one low muted memory tone remains. 0.5 seconds.
```

### SFX_UI_UNPAUSE

```text
Game resumes: muted world ambience expands outward and a tiny iron detail returns. 0.5 seconds.
```

### SFX_UI_MAP_OPEN

```text
Old memory map opens: dry layered parchment-like stone sheet, faint chain slide and amber dust reveal. 0.6 seconds.
```

### SFX_UI_MAP_CLOSE

```text
Memory map closes: layered stone-paper folds softly, amber dust collapses inward. 0.5 seconds.
```

### SFX_UI_ROOM_DISCOVERED

```text
New room discovered cue: two quiet notes from the incomplete D minor memory motif with soft stone resonance. 0.8 seconds.
```

### SFX_UI_FRAGMENT_RECORDED

```text
Memory fragment is recorded in the journal: reversed crystal grain, one fragile piano note and restrained amber confirmation. 1 second.
```

### SFX_UI_ACTION_DENIED

```text
Subtle unavailable-action cue: dull iron tick and empty low breath, clear but non-irritating. Under 0.3 seconds.
```

---

# 10. 권장 제작 순서

1. `MUS_RESIDUE_EXPLORATION`
2. `AMB_ENTRY_BRIDGE`, `AMB_LOWER_RUINS`, `AMB_INSIDE_FINGERS`
3. 플레이어 이동·공격·피격 효과음
4. 되감기 효과음 전체
5. 체크포인트·아이템·승강기·관문
6. 일반 적 4종
7. 손목의 감시자
8. 기억의 교수자
9. 나머지 음악 변주와 UI

음악을 먼저 여러 곡 만들지 말고 탐험곡 한 곡의 음색·BPM·기억 테마를 확정한 뒤 나머지
음악 프롬프트를 실행해야 지역 전체가 하나의 작품처럼 들린다.
