# 균열 전체 지도·룸 콘셉트 생성 프롬프트

## 생성 방식

- 생성 도구: built-in `image_gen`
- 용도 분류: `stylized-concept`
- 기준 이미지: `docs/concept-art/04-fracture-liminal-paradise-v3.png`
- 출력 규격: 1672×941 PNG
- 전체 지도는 기준 이미지 편집, 룸 이미지는 전체 지도와 인접 룸을 참조한 신규 생성

## 공통 시각 규칙

```text
Use case: stylized-concept.
Asset type: wide 2D side-scrolling metroidvania environment concept art.
Create a bright liminal paradise made from pale marble ruins, translucent glass architecture,
shallow reflective water, lavender flowers and hanging vines beneath an immense mint-blue sky.
Palette: pastel mint, ice blue, lavender, pale peach light and white marble.
The scene must remain calm and beautiful at first glance.
Add only five percent subtle unease through one-column-off symmetry, reflections that contain
one extra arch, faint future-ghost architecture and unnaturally repeated distant gateways.
No explicit horror, flesh, blood, eyes, teeth or monsters.
Strict side-view traversal composition with clear horizontal rests, vertical routes, stairs,
bridges and readable entrances, but no player character or gameplay entities.
No character, humanoid silhouette, enemy, boss, item, UI, text, labels, numbers or watermark.
No purple wireframe blockout shapes.
Painterly high-detail fantasy environment art with quiet gameplay readability.
```

## 전체 지도

```text
Input image: the Fracture reference is the authoritative palette, material and atmosphere source.
Edit it into one enormous interconnected exploration region.
Remove the small white-haired player character completely and reconstruct the marble walkway,
flowers and shadows behind that character.
Remove every translucent purple wireframe placeholder and replace each with a believable
pale-marble or translucent-glass moving platform belonging to the same architecture.
Recompose the landscape so it reads as one explorable metroidvania world:
lower-left glass-garden entrance, misaligned promenade, central possibility-plaza hub,
lower submerged garden and foresight sanctuary, right-side time-lag greenhouse,
floating architecture, a central reversing elevator shaft, mirrored upper hall,
second-hand watchtower, not-yet-built ruins and the final sky fracture at the highest point.
Show three physical shortcut loops back toward the central hub.
Keep the central vertical beam and repeated arches as the main long-range landmark.
No map labels, arrows or diagram lines; express navigation only through architecture.
```

## 룸별 공통 규칙

```text
The Fracture world master fixes world identity, landmark placement and progression direction.
This image is a closer room-scale view from inside that same enormous map, not a separate landscape.
Preserve the room's entrance and exit heights so it can connect to adjacent rooms.
Keep distant landmarks consistent with the room's location in the world.
Use broad quiet shapes behind the gameplay plane and avoid foreground clutter over the center.
Do not include a player character, humanoid silhouette or any gameplay actor.
```

## F01~F12

### F01 — 유리 정원

```text
Lower-left entrance room. An intact pale-marble garden arcade stands above shallow glassy water.
The route enters from the left at lower-floor height and exits right at the same height.
The distant central sky beam is barely visible through repeating arches.
Subtle unease: the water reflects one additional column.
```

### F02 — 어긋난 산책로

```text
Immediately right of F01. A beautiful upper marble promenade crosses the room but three segments
are slightly separated and destined to collapse; a safe lower arcade curves beneath and rejoins
the right exit. Entry and exit remain at lower-mid height.
Subtle unease: stair counts differ by one between mirrored sides.
```

### F03 — 가능성 광장

```text
Central hub plaza with a broad safe floor and four readable routes: left to F02, down to F04,
right to F06 and an inactive upward elevator route toward F08.
The distant second-hand watchtower and final vertical sky fracture are both visible.
A pale empty doorframe flickers as faint future architecture.
```

### F04 — 흔들리는 하층정원

```text
Submerged lower garden beneath F03. Low flowerbeds and marble islands hover just above water,
forming slow side-to-side traversal lanes. Entry descends from upper left; exit rises toward F05
on the right. A shadowed waterline passage hints at FS1 below.
```

### F05 — 예지 성소

```text
Quiet foresight sanctuary slightly above and right of F04. A narrow vertical beam illuminates
a small marble altar inside an open circular pavilion. One glass platform and one incomplete
future-ghost doorway teach predicted position. A shortcut arch leads back toward F03.
```

### F06 — 시차 온실

```text
Large luminous glass greenhouse to the right of F03 and above F05. Three pale glass platforms
occupy distinct heights and travel paths among white frames and lavender vines.
The main route climbs right toward F07; a reverse-moving side platform reaches FS2.
Subtle unease: identical greenhouse bays repeat too far into the mist.
```

### F07 — 부유 건축군

```text
Mid-right floating architecture district above open reflective water.
Horizontal and vertical marble fragments cross without touching, separated by wide safe islands.
The route rises from lower left toward F08 at upper center.
The watchtower is now larger and the central sky beam more prominent.
```

### F08 — 역행 승강축

```text
Tall central reversing elevator shaft connecting the hub to the upper district.
Stacked open arches form three floors with broad safety alcoves.
A translucent marble lift is currently below its faint future-ghost position.
Lower shortcut sightline points back toward F03; upper exit leads to F09.
```

### F09 — 거울 가능성실

```text
High mirrored possibility hall with two nearly symmetrical traversal lanes and a central pool.
Left and right paths look equivalent but one contains a missing future platform.
Subtle unease: reflections disagree with the real column count and the lighting angle.
Exit rises toward the watchtower on the upper right.
```

### F10 — 초침 감시탑

```text
High circular watchtower arena built from white marble rings and one long clock-hand bridge.
Entry arrives from lower left; a post-battle bridge can extend toward F07 as shortcut C.
The vertical sky fracture hangs directly behind the tower.
Keep the circular central space broad enough for a mid-boss arena.
```

### F11 — 아직 오지 않은 폐허

```text
Quiet upper ruin after the watchtower. Only foundations, broken stairs and empty doorframes exist
in the present, while a complete palace appears as a very faint two-seconds-ahead ghost silhouette.
The route moves from lower left to upper right toward F12.
A repeated ghost door in a side wall hints at FS3.
```

### F12 — 내일의 균열

```text
Highest final arena beneath an enormous mint-white fracture splitting into three faint branches.
Broad left stable zone, changing central platforms and an open right attack zone are readable.
The surrounding paradise remains beautiful while distant architecture appears slightly too symmetrical.
No boss or character; show only the empty climactic environment and a future exit door.
```

## 비밀방

### FS1 — 버려진 가능성

```text
Hidden submerged passage below F04. Repeating marble columns vanish into blue water and align for
one narrow route only at a particular moment. Dimmer lavender plants and refracted daylight preserve
the region palette. The reflection contains an impossible continuation of the corridor.
```

### FS2 — 멈춘 오후

```text
Small sealed greenhouse beyond F06 where warm pale-peach afternoon light, petals and pollen appear
perfectly frozen. A reverse-moving glass platform arrives from the left. The quiet room is beautiful,
still and empty, with one repeating greenhouse bay too many.
```

### FS3 — 선택되지 않은 문

```text
Unfinished side corridor beside F11. A plain pale wall, a shallow reflecting pool and scattered
foundations dominate the present. One delicate future-ghost door appears where no physical opening
exists, repeated faintly three times at the same position. No overt horror.
```
