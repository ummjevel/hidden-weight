# Hidden Weight 콘셉트 이미지 생성 프롬프트

이 문서는 `docs/concept-art/`에 저장된 콘셉트 이미지와 실제 생성에 사용한 프롬프트를 연결한다.

- 생성 방식: Codex 내장 ImageGen
- 출력 형식: PNG
- 기본 구도: 16:9 횡스크롤 게임 환경 콘셉트
- 중복 제외: `03-gaze-eye-swarm-v3.png` 생성 전 발생한 네트워크 오류 재시도는 성공 프롬프트와 내용이 같아 한 번만 기록했다.
- 참고 이미지 경로는 생성 당시 경로다. `/var/folders/...`의 클립보드 파일은 운영체제가 정리하면 사라질 수 있다.

---

## 01. 프롤로그 — Cosmic Awakening

**결과:** `docs/concept-art/01-prologue-cosmic-awakening.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: 16:9 wide game environment concept art, Prologue scene for a 2D side-scrolling metroidvania
Input images: Image 1 is a regional palette and atmosphere reference only; Image 2 is the tiny white-haired girl protagonist and painterly game-art reference only. Create a new original scene, do not copy specific architecture or layouts.
Primary request: a terrifying but beautiful cosmic awakening scene, psychological cosmic horror 70%, organic grotesque detail 20%, beautiful surrealism 10%
Scene/backdrop: an immeasurably vast star-filled void that also subtly resembles the inside of a cosmic womb and a colossal eye socket; black-indigo space, deep violet nebulae, cold white light
Subject: a very small white-haired girl in a pale simple dress standing on a traversable crescent-shaped stone platform near the lower center, her silhouette readable and consistent with Image 2
Key landmarks: a planet-sized closed eye floating in the far background; thin roots, threads, and hair-like tendrils hanging upward against gravity; constellations that subtly resemble fingers and joints; distant nebula forming an almost invisible face; broken circular gates and small moon platforms suggesting a side-scrolling route
Style/medium: original high-detail painterly 2D game concept art, layered silhouettes and atmospheric depth, elegant hand-painted shapes, not pixel art, not photorealistic
Composition/framing: cinematic 16:9 establishing shot; clear foreground silhouette, continuous playable ground/platform route, midground structures, enormous deep background; protagonist occupies less than 4 percent of frame height to emphasize cosmic scale
Lighting/mood: ominous cold rim light around the girl, faint volumetric starlight, oppressive scale, deeply unsettling on closer inspection, no explicit gore
Constraints: no text, no title, no logo, no UI, no border, no watermark; no copied game characters or recognizable architecture; no giant monster in the immediate foreground; preserve game-level readability and clear traversable surfaces
```

---

## 02-1. 잔재 — 초기 폐허

**결과:** `docs/concept-art/02-residue-ruins.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`
3. `docs/concept-art/01-prologue-cosmic-awakening.png`

```text
Use case: stylized-concept
Asset type: 16:9 wide game environment concept art, Residue zone for the same original 2D side-scrolling metroidvania world
Input images: Image 1 is the palette and regional mood reference; Image 2 defines the tiny white-haired girl protagonist; Image 3 defines the approved painterly visual language, scale, layered depth, and protagonist proportions. Create an original new location.
Primary request: the past and guilt embodied as ancient collapsing ruins, deeply frightening psychological cosmic horror with subtle organic grotesque architecture
Scene/backdrop: an endless abyss filled with ruined bridges, fractured temples, hanging columns, dead roots and suspended debris; dominant desaturated navy #3D4A66, black-blue, gray-brown, with only tiny dying muted-orange lights
Subject: the same very small white-haired girl in a pale dress standing on a clear traversable ruined stone path in the lower middle distance, readable cold rim light
Key landmarks: a colossal seated stone figure with a hollow cavity where its heart and face should be; a broken circular gate; gallows-like pillars leaning over the path; bridge stones floating frozen in mid-collapse as if time stopped; rib-like arches that only reveal their anatomical resemblance on closer inspection; numerous tiny dim eyes hidden inside the hollow statue and deep cracks
Style/medium: original high-detail painterly 2D game environment concept art, elegant dark fantasy with cosmic horror, layered atmospheric perspective, not pixel art, no recognizable borrowed architecture
Composition/framing: cinematic wide side-scroller establishing shot, continuous playable ground across the lower third, foreground framing silhouettes, middle-ground interactive broken bridge, vast ruined background; protagonist less than 4 percent of frame height
Lighting/mood: heavy, ancient, suffocating, cold low-key lighting, mist rising from the abyss, dying orange accent lamps, visible guilt and irreversible damage; scary and grotesque without explicit gore
Constraints: no text, no title, no logo, no UI, no border, no watermark; no copied game characters; no bright cheerful colors; keep traversable surfaces and scene layers clearly readable
```

---

## 02-2. 잔재 V2 — Hell Concept

**결과:** `docs/concept-art/02-residue-hell-concept-v2.png`

**참고 이미지**

1. `codex-clipboard-bc16cc89-6c8d-4f73-8bc3-5df1c99b49ad.png`
2. `codex-clipboard-5686cd83-ad47-4593-ac1e-e65499dd5a4d.png`
3. `codex-clipboard-db4dacea-f57e-4ad0-822c-722bb3fe209a.png`
4. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art for the Residue zone of an original 2D side-scrolling metroidvania
Input images: Images 1-3 are mood and scale references only: use the feeling of an impossibly tall skeletal humanoid intruding into an ordinary ruined landscape, elongated limbs, oppressive distance, dusty hell-colored sky, and tiny human scale. Do not reproduce their exact creatures, composition, buildings, signatures, text, or recognizable details. Image 4 defines only the small white-haired girl protagonist.
Primary request: redesign Residue as a disturbing vision of hell created from guilt and memories, more grotesque and frightening than elegant dark fantasy, but still readable as a game level
Scene/backdrop: a dead suburban district and ruined playground fused with an ancient infernal wasteland; cracked road and stone platforms cross the lower third; abandoned apartment silhouettes, broken swings, utility poles, dead grass, ash pits, hanging roots and rib-like arches; the ground descends into a glowing black-red chasm
Main horror landmark: one original colossal emaciated humanoid skeleton in the far middle background, taller than the buildings, with absurdly long arms touching the streets, a tilted smooth skull containing several small dim eye holes, a hollow rib cage filled with suspended rooms and tiny lights; its body appears partly fossilized into the city, not a copied monster
Additional horror: distant human-shaped figures frozen while facing away; bodies implied only as eroded statues and root bundles, no explicit gore; bridge fragments float in a repeated collapse; gallows-like poles bend toward the protagonist; cloud layers subtly form another enormous open mouth
Subject: the same tiny white-haired girl in a pale dress from Image 4, alone on the playable path near the lower center-left, less than 4 percent of frame height, strong pale silhouette
Style/medium: rough painterly environment keyframe, visible broad brushwork, simplified block-in shapes, concept exploration rather than polished final illustration, original cosmic body horror, not photorealistic, not pixel art
Composition/framing: wide side-scrolling level view with clear foreground silhouette, continuous traversable ground, midground ruins and monster legs, deep background city and sky; leave practical gaps, ledges and a broken bridge for gameplay
Lighting/mood: dirty ochre, burnt umber, ash gray and black-red hell palette; harsh sulfur-yellow backlight behind storm clouds; small cold blue rewind glow on broken objects; suffocating heat haze and falling ash
Constraints: no text, no signature, no watermark, no logo, no UI, no border; no recognizable copied creature or game architecture; no explicit blood, exposed organs, or graphic gore; prioritize disturbing silhouette, oppressive scale and environmental storytelling over decorative detail
```

---

## 02-3. 잔재 V3 — Ambiguous City

**결과:** `docs/concept-art/02-residue-ambiguous-city-v3.png`

**참고 이미지**

1. `docs/concept-art/02-residue-hell-concept-v2.png`
2. 잔재 참고 이미지 3장
3. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art, revised Residue zone for an original 2D side-scrolling metroidvania
Input images: Image 1 is the previous Residue draft and defines only the ochre-black-red hell palette, rough painterly rendering, side-scrolling route, scale, and tiny protagonist. Images 2-4 are mood references only for colossal unease, elongated forms, and an ordinary landscape invaded by something incomprehensible. Image 5 defines only the small white-haired girl. Do not reproduce any exact creature, layout, building, signature, or text.
Primary request: remove the immediately recognizable giant skeleton monster. At first glance this must look like a ruined infernal city. Only after looking longer should the viewer realize the entire city is one enormous crouching human-like fossil made from architecture, negative space, repeated memories, and failed rewinding.
Perception sequence: first read = abandoned apartment ruins, broken elevated roads, dead playground and ash wasteland; second read = bridge arches align like ribs, towers repeat like vertebrae, highways bend like elbows, utility lines resemble tendons; third read = the whole city silhouette is a crouched body with no clear boundary between architecture and anatomy.
Main hidden form: do not draw a complete skull, complete rib cage, or clearly outlined monster. The face exists only as a dark negative space between two leaning apartment blocks and storm clouds. Several dim eye holes are actually distant windows and shift toward the protagonist. One arm is an elevated roadway; multiple semi-transparent earlier positions of that roadway overlap like failed rewind poses. The chest cavity contains rooms, staircases and tiny cold lights instead of organs. Its front and back cannot be distinguished.
Scene details: same dead suburban district fused with ancient hell; broken swings and poles, eroded human-shaped statues facing away, chunks of a collapsed bridge frozen at several repeated moments, black-red chasm beneath the playable route, falling ash, clouds subtly forming an enormous mouth. No explicit bodies or gore.
Subject: the same tiny white-haired girl in a pale dress standing alone on a clear traversable path near the lower center-left, less than 4 percent of frame height, pale readable silhouette.
Style/medium: rough painterly environment block-in, visible broad brushwork, simplified concept shapes, unsettling cosmic architectural body horror, intentionally less polished than final key art, not photorealistic, not pixel art.
Composition/framing: wide side-scrolling level view with clear foreground silhouette and continuous playable ground, practical ledges and broken bridge; the hidden body silhouette spans most of the middle and far background but must not be immediately obvious.
Lighting/mood: dirty ochre sulfur sky, burnt umber ruins, ash gray, black-red abyss, cold blue rewind glow only on duplicated fragments; suffocating heat haze.
Constraints: no obvious standing skeleton, no clearly readable giant monster at first glance, no complete anatomical diagram; no text, signature, watermark, logo, UI, or border; no recognizable copied creature or game architecture; no explicit blood, exposed organs, or graphic gore; prioritize delayed recognition and ambiguity over spectacle.
```

---

## 02-4. 잔재 V4 — Failed Rewind City

**결과:** `docs/concept-art/02-residue-failed-rewind-city-v4.png`

**참고 이미지**

1. `docs/concept-art/02-residue-ambiguous-city-v3.png`
2. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art, Residue zone V4 for an original 2D side-scrolling metroidvania
Input images: Image 1 is the previous approved Residue concept. Preserve its side-scrolling layout, ruined suburban infernal city, tiny protagonist scale, rough painterly language and general ochre-black atmosphere, but substantially increase ambiguity and dread. Image 2 defines only the small white-haired girl.
Primary request: make the city itself an incomprehensible failed-rewind organism. Reduce majestic fantasy spectacle and increase delayed-recognition psychological cosmic horror. The viewer must not be able to identify a complete monster.
Major change 1 — incomplete scale: hide more than half of the colossal form behind black sulfur clouds, ash fog, the frame edges and the underground chasm. Crop every possible head or limb beyond the image. Show only disconnected architectural masses that might belong to a body extending far outside the frame.
Major change 2 — negative-space face: do not draw eyes, skull, mouth or a complete face. Arrange two leaning apartment blocks, broken elevated roads and storm clouds so the darkness between them only ambiguously resembles a gigantic face looking down. A few irregular distant windows act like uncertain pupils only after prolonged viewing.
Major change 3 — failed rewind: overlap the entire midground city in three subtly different temporal positions. Duplicate highways, towers and bridge fragments as thin cold-blue semi-transparent afterimages displaced in incompatible directions. One elevated roadway bends through several impossible elbow positions at once. Debris appears to fall upward and downward simultaneously.
Major change 4 — human unease: across the dead playground and distant road, place many tiny eroded humanoid silhouettes repeating the exact same standing pose as the protagonist. They all face toward her, but use impossible inconsistent scale: some are too large for distant buildings and others impossibly small. Keep them statue-like and non-graphic.
Major change 5 — impossible anatomy through architecture: bridge arches loosely align like ribs but never form a full rib cage; tower clusters repeat like vertebrae; utility wires behave like tendons; staircases exit buildings and re-enter distant structures. Front and back geometry appear visible simultaneously. No boundary between city and organism.
Scene/playability: maintain a clear continuous cracked foreground path across the lower third with practical ledges, a broken bridge gap, abandoned swings and utility poles. The same tiny white-haired girl stands near lower center-left, less than 4 percent of frame height, the only clean pale color.
Style/medium: rough painterly concept block-in, broad visible brushwork, simplified shapes, dirty texture, original architectural cosmic body horror, intentionally less polished than final key art, not photorealistic, not pixel art.
Lighting/mood: replace beautiful golden light with sickly sulfur yellow-brown, rust, ash gray and near-black edges; reduce glowing lava; use smothering black heat haze and falling ash. Cold blue rewind traces should be extremely thin and unnatural.
Constraints: no obvious complete monster, no standing skeleton, no recognizable animal silhouette, no explicit skull or anatomical diagram, no clearly drawn facial features; no text, signature, watermark, logo, UI or border; no blood, exposed organs or graphic gore; prioritize uncertainty, impossible scale, temporal contradiction and delayed recognition.
```

---

## 02-5A. 잔재 V5 초안 — Presence Draft

**결과:** `docs/concept-art/02-residue-presence-v5-draft.png`

**참고 이미지**

1. `docs/concept-art/02-residue-failed-rewind-city-v4.png`
2. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art, Residue zone V5 for an original 2D side-scrolling metroidvania
Input images: Image 1 is the current Residue V4 concept. Preserve its ruined infernal city, sickly sulfur palette, ambiguous architecture, repeated figures, cold-blue rewind traces, clear side-scrolling foreground path, rough painterly language and tiny protagonist. Image 2 defines only the small white-haired girl.
Primary request: improve V4 so the presence of one colossal living entity is undeniable, while its species, full anatomy and exact boundaries remain impossible to understand. Do not return to an obvious giant skeleton or clearly outlined monster.
Existence signal — partial body: use sulfur-yellow backlight to reveal only about 25 to 30 percent of a gigantic crouching silhouette fused into the city. On the upper right, a massive sloped architectural mass must read as one shoulder and part of a hunched back. One broken elevated highway must plausibly read as a bent arm wrapping around the city. Beneath the highway, three or four impossibly long pillar-like forms must suggest fingers pressing into streets, but still resemble structural supports. Hide the entire head inside dense black clouds beyond the top edge. Hide all lower body below the abyss and frame.
Existence signal — failed rewind afterimage: behind and slightly offset from the city, add a very faint thin cold-blue temporal afterimage that suggests an earlier human-like pose: an incomplete arc where a head may have been, one curved spine line and a second displaced arm position. Keep it around 10 to 15 percent visual strength, broken and misaligned with the present architecture. It must reveal posture but not a face or anatomy.
Existence signal — breathing environment: create a large dark cavity near the center of the city mass. Ash, fog, loose papers and tiny debris should visibly stream inward toward it as if the city is inhaling. Rows of distant windows around the cavity illuminate and extinguish in a slow wave. Utility cables and bridge tendons pull taut toward the cavity. A huge soft shadow moves toward the protagonist despite the sulfur backlight.
Ambiguity: maintain impossible overlapping highways and buildings in three temporal positions. The shoulder, arm, fingers and cavity must each be legible enough to imply life, but they must never connect into a clean full silhouette. Front and back geometry overlap. No complete head, face, torso, rib cage or animal outline.
Human unease: retain scattered eroded humanoid silhouettes in inconsistent scale. They all repeat the protagonist's standing pose and face her. Keep the dead playground and empty swings.
Subject/playability: the same tiny white-haired girl in a pale dress stands near lower center-left on a clear continuous cracked foreground platform with a broken bridge gap. She is less than 4 percent of frame height and remains the only clean pale color.
Style/medium: rough painterly concept block-in, broad visible brushwork, simplified shapes, dirty texture, original architectural cosmic body horror, intentionally not polished final art, not photorealistic, not pixel art.
Lighting/mood: sickly sulfur yellow-brown behind the partial silhouette, rust and ash gray ruins, near-black clouds and frame edges, very restrained black-red chasm glow, hair-thin cold blue rewind lines. Oppressive, airless and frightening rather than majestic.
Constraints: no obvious complete monster, no standing skeleton, no recognizable animal silhouette, no explicit skull, eyes, mouth or complete face; no text, signature, watermark, logo, UI or border; no blood, exposed organs or graphic gore. The viewer must immediately feel that something alive is crouching inside the city, but must not be able to define what it is.
```

---

## 02-5B. 잔재 V5 — Crouching Presence

**결과:** `docs/concept-art/02-residue-crouching-presence-v5.png`

**참고 이미지**

1. `docs/concept-art/02-residue-presence-v5-draft.png`
2. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: targeted revision of a rough 16:9 Residue environment concept for an original side-scrolling horror game
Input images: Image 1 is the current V5 draft and must remain the main composition and style reference. Image 2 defines only the tiny white-haired girl.
Primary request: keep the foreground route, sickly sulfur palette, repeated humanoid figures, temporal blue traces, ruined city texture, black frame edges and playground. Change only the central upper city mass so one colossal crouching presence becomes more immediately felt through posture, while its identity and full anatomy stay hidden.
Targeted silhouette correction: consolidate the central-left tower mass into a single unmistakable sloping shoulder rising from behind the city. From that shoulder, make one elevated-road structure bend downward like an extremely long arm bearing weight on the city. At the road's lower termination, use four vertical ruined support columns with slightly tapered ends so they can read as immense fingers pressing into the streets while still looking like architecture. On the opposite side, use the large arching highway as the suggestion of a second arm surrounding the scene.
Hidden head: above and between the shoulders, create a dense head-sized absence in the sulfur backlight, completely filled with moving black cloud and cropped by the top edge. Do not outline a skull or face. The only facial suggestion should come from two or three irregular distant windows buried inside the darkness, too asymmetrical to confirm as eyes.
Breathing cavity: strengthen the central black hollow beneath the hidden head. Make ash, paper and debris stream visibly inward in a spiral. Pull nearby cables taut toward it. Arrange surrounding window lights in a fading radial wave, implying inhalation.
Temporal confirmation: retain the thin cold-blue arcs but shape one broken arc as an earlier shoulder and incomplete head position offset from the present silhouette. Keep this faint and fragmented, never a complete humanoid outline.
Readability goal: at thumbnail size, the viewer should immediately feel that a gigantic hunched being is crouching behind and inside the city. At full size, every apparent body part should break back into roads, buildings, pillars, smoke and negative space. The being's species, face and total scale must remain unknowable.
Subject: same tiny white-haired girl on the lower-left playable path, less than 4 percent of frame height, unchanged.
Style/medium: rough painterly concept block-in with broad brushwork and dirty texture, architectural cosmic body horror, not polished final art, not photorealistic, not pixel art.
Constraints: preserve the overall framing and game path; no complete monster, no visible skeleton, no explicit skull, mouth, eyes or clean body outline; no text, signature, watermark, logo, UI or border; no blood, organs or graphic gore.
```

---

## 02-6. 잔재 V6 — Cloud Face

**결과:** `docs/concept-art/02-residue-cloud-face-v6.png`

**참고 이미지**

1. `docs/concept-art/02-residue-crouching-presence-v5.png`

```text
Use case: precise-object-edit
Asset type: targeted revision of an original 16:9 rough game environment concept, Residue V6
Input image: the current approved V5 concept. Preserve the entire composition, tiny white-haired protagonist, foreground gameplay path, giant architectural fingers on the left, layered highways on the right, repeated human silhouettes, cold-blue rewind arcs, sulfur-brown palette, rough painterly texture, lighting and crop.
Primary request: change only the central upper dark cloud-and-building void so it reads somewhat more clearly as a gigantic face looking downward, but remains ambiguous environmental pareidolia rather than a drawn monster face.
Face construction: use the existing storm-cloud negative space as a broad head-shaped mass. Create two unequal shadowed recesses suggesting eye sockets, formed from gaps between clouds and irregular clusters of distant lit windows. Place them asymmetrically and partially obscure one with smoke. Suggest a crooked nose bridge using one broken vertical tower and hanging cables. Suggest only the faint beginning of a very wide mouth through a horizontal break in the clouds and collapsed bridge silhouettes, but do not draw lips, teeth or a clean mouth outline. Let the lower half dissolve back into ash and architecture.
Expression and gaze: the implied face should feel exhausted, guilty and inhuman, angled slightly downward toward the small protagonist. It should become readable at thumbnail size but remain questionable at full resolution.
Temporal detail: add a very faint offset cold-blue incomplete contour around one eye position and part of the cheek, implying that the face occupied a different past position. Keep the blue trace subtle and broken.
Invariants: change only the central implied face region; keep all other major geometry, game path, figures, protagonist, colors and framing as close to the input as possible.
Style/medium: rough painterly concept block-in, architectural cosmic horror, environmental pareidolia, broad dirty brushwork, not photorealistic, not polished final art.
Constraints: no explicit skull, no normal human portrait, no clean facial outline, no visible skin, no teeth, no gore; no text, signature, watermark, logo, UI or border. The face must still be made entirely from clouds, negative space, windows, tower fragments and bridge debris.
```

---

## 03-1. 응시 — 초기 Cathedral

**결과:** `docs/concept-art/03-gaze-cathedral.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`
3. `docs/concept-art/02-residue-ruins.png`

```text
Use case: stylized-concept
Asset type: 16:9 wide game environment concept art, Gaze zone for the same original 2D side-scrolling metroidvania world
Input images: Image 1 is the purple-teal regional palette reference; Image 2 defines the same tiny white-haired girl protagonist; Image 3 defines the approved painterly detail, scale, layered side-scroller composition, and cosmic-horror visual language. Create an original new location.
Primary request: present-day shame and surveillance embodied as a cathedral, prison, and theater fused into one impossible vertical interior; frightening psychological cosmic horror with controlled grotesque detail
Scene/backdrop: vast low-contrast purple and teal architecture fading into fog, enormous draped walls and galleries, endless depth, architecture subtly shaped like eyelids and sockets
Subject: the same very small white-haired girl in a pale dress on a clearly traversable stone path, visually isolated and exposed, readable pale rim light
Key landmarks: one enormous half-open eye embedded high in the cathedral wall; dozens of mismatched eyes painted or grown into pillars, all looking at the girl from impossible perspectives; hanging cages containing empty humanoid silhouettes; rows of faceless black spectators along balconies; a narrow crawl passage; a distant eye-shaped mirror or altar that implies awareness awakening
Style/medium: original high-detail painterly 2D game environment concept art, elegant dark surrealism, purple #4A3F63 shadows with teal glows, layered atmospheric perspective, not pixel art, no recognizable borrowed architecture
Composition/framing: cinematic wide side-scroller view; foreground balcony silhouettes, continuous playable ground in the lower third, middle-ground cages and narrow passage, huge eye landmark in the background; protagonist less than 4 percent of frame height
Lighting/mood: dim violet ambient light, sickly teal reflections, hazy low contrast, suffocating sensation of being watched; disturbing and bizarre without explicit gore
Constraints: no text, no title, no logo, no UI, no border, no watermark; no copied game characters; keep the image readable as a playable side-scrolling environment; eyes must feel integrated into architecture rather than pasted icons
```

---

## 03-2. 응시 V2 — Sky Observer

**결과:** `docs/concept-art/03-gaze-sky-observer-v2.png`

**참고 이미지**

1. 응시 사용자 참고 이미지 4장
2. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art for the Gaze zone of an original 2D side-scrolling metroidvania
Input images: Images 1-4 are mood and scale references only for an enormous eye-like presence in the sky, oppressive surveillance, tiny human scale, ocular forms hidden in darkness and cloud structures. Do not reproduce any exact creature, eye arrangement, city layout, signature, watermark, text or recognizable composition. Image 5 defines only the small white-haired girl protagonist.
Primary request: create an original city-cathedral surveillance world where the entire sky behaves like one incomprehensible visual organ and the ruined architecture forms its nervous system. It should feel more disturbing and cosmic than decorative fantasy.
Scene/backdrop: a deep urban canyon fused with a cathedral and prison, purple-gray dusk, low-contrast violet fog, teal reflections on wet stone, leaning apartment towers and narrow bridges. The lower third contains a clear side-scrolling route with broken stairs, a narrow hiding passage and a gap crossed by a suspended platform.
Main presence: high in the cloud ceiling, one enormous partially obscured eye-like void watches downward. Do not draw a clean floating eyeball. Its eyelids are formed by two vast storm fronts; its iris is made from rotating cloud strata, distant city lights and a faint galaxy ring; its pupil is a lightless circular absence. Only part of it is visible beyond the top edge, making its total scale unknowable.
Secondary structure: three or four gigantic cloud-and-cable arcs extend from the sky eye across the city like optic nerves or tentacles, but each arc is built from storm clouds, balconies, hanging cages, electrical wires and rows of windows. Embed only a limited number of half-formed eye recesses in these arcs; many should remain closed or uncertain rather than a field of obvious eyeballs.
Surveillance environment: apartment windows and cathedral apertures subtly turn toward the protagonist; hanging cages contain faceless standing silhouettes; balcony crowds have bodies facing forward but empty heads angled toward her; wet puddles reflect open eyes that do not exist above them. A broad cone of missing light sweeps one section of the playable path, implying the gaze hazard without a laser beam.
Subject: the same tiny white-haired girl in a pale dress stands alone near the lower center-left, less than 4 percent of frame height, exposed under a small cold rim light. Her shadow points upward toward the sky eye instead of away from it.
Style/medium: rough painterly environment block-in, visible broad brushwork and simplified shapes, original architectural cosmic horror, early concept exploration rather than polished final illustration, not photorealistic, not pixel art.
Composition/framing: wide 16:9 side-scrolling establishing view; foreground ruined railings and silhouettes, continuous traversable ground across the lower third, midground cages and crowds, enormous sky-organ occupying the upper half. Maintain strong scale hierarchy and playable surfaces.
Color palette: desaturated violet and bruised purple, charcoal, cold teal #5BA7AF reflections, pale lavender eye glow #B6A4D9, nearly black pupil. Less fiery and hellish than Residue; colder, quieter and more exposed.
Lighting/mood: suffocating stillness, damp haze, no direct sunlight, the feeling that every surface is watching. Frightening through surveillance and impossible scale, not gore.
Constraints: no simple giant floating eyeball with a clean outline, no excessive carpet of identical eyes, no recognizable copied monster or city, no text, signature, watermark, logo, UI or border; no explicit blood, organs or graphic gore; preserve clear side-scrolling gameplay readability.
```

---

## 03-3. 응시 V3 — Eye Swarm

**결과:** `docs/concept-art/03-gaze-eye-swarm-v3.png`

**참고 이미지**

1. `docs/concept-art/03-gaze-sky-observer-v2.png`
2. 응시 사용자 참고 이미지 2장
3. `docs/character_sprite_ref.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art, Gaze zone V3 for an original 2D side-scrolling metroidvania
Input images: Image 1 is the current Gaze V2 concept and defines the composition, purple-teal city-cathedral, giant sky eye, hanging cages, crowds, playable foreground route and tiny protagonist. Images 2-3 are mood references only for oppressive irregular eye clusters and cloud-scale ocular forms; do not copy their exact arrangement, creature, composition, text, signature or watermark. Image 4 defines only the small white-haired girl.
Primary request: intensify Gaze into overwhelming clustered-eye horror. The viewer should feel surrounded by countless incompatible gazes, while the image remains readable as a game level and retains one clear visual hierarchy.
Hierarchy layer 1 — central eye: preserve one enormous partially obscured eye-like void in the upper sky as the dominant landmark. Its eyelids remain storm fronts, iris made from cloud strata and distant lights, pupil almost perfectly black. Do not turn it into a clean floating eyeball.
Hierarchy layer 2 — medium eye colonies: transform the large cloud-and-bridge optic-nerve arcs into irregular colonies containing dozens of medium eyes of different sizes and shapes. Some eyes squeeze against each other, some share a single pupil, some have horizontal or sideways eyelids, some are incomplete recesses, and a few contain smaller eyes inside the iris. Avoid even spacing or decorative symmetry.
Hierarchy layer 3 — tiny embedded eyes: integrate hundreds of very small half-hidden eyes into peripheral architecture and texture: apartment windows, cracked masonry, drain holes, railings, wet pavement, cathedral carvings, cables and fog. At first they should read as city texture; on closer inspection they become eyes. Concentrate them around the top and left/right frame edges, not across the playable path.
Eye states: mix roughly 30 percent closed, 25 percent barely open, 20 percent looking directly at the protagonist, 15 percent looking in conflicting directions, 10 percent pupil-like holes without visible eyelids. Do not make every eye fully open.
Psychological meaning: inside a few larger irises, place extremely faint repeated silhouettes of the same white-haired protagonist, suggesting self-judgment multiplied into countless observers. Balcony crowds remain faceless, but their empty head openings subtly resemble closed eyelids.
Gameplay space: preserve a clear continuous foreground path across the lower third, left hiding tunnel, stair route, central suspended platform and right landing. Keep eye density lower immediately around these traversable surfaces. A broad cone of missing light crosses one route as a gaze hazard. Puddles reflect eyes absent from the sky.
Subject: the same tiny white-haired girl in a pale dress near lower center-left, less than 4 percent of frame height, isolated by a cold pale rim light. Her shadow points upward toward the central eye.
Style/medium: rough painterly environment block-in, broad visible brushwork, simplified early concept shapes, original cosmic surveillance horror with dense organic patterning, not polished final illustration, not photorealistic, not pixel art.
Color palette: bruised violet, charcoal and low-contrast gray-purple; cold teal reflections #5BA7AF; pale lavender highlights #B6A4D9; nearly black pupils. Keep it colder and slightly brighter than Residue.
Mood: silent, damp, exposed, visually suffocating, intense clustered-eye unease without explicit gore.
Constraints: maintain clear focal hierarchy and protagonist readability; no uniform wallpaper pattern, no identical repeated eye stamps, no simple giant floating eyeball, no recognizable copied monster or city; no text, signature, watermark, logo, UI or border; no blood, exposed organs or graphic gore.
```

---

## 04-1. 균열 — 초기 Pastel Abyss

**결과:** `docs/concept-art/04-fracture-pastel-abyss.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`
3. `docs/concept-art/03-gaze-cathedral.png`

```text
Use case: stylized-concept
Asset type: 16:9 wide game environment concept art, Fracture zone for the same original 2D side-scrolling metroidvania world
Input images: Image 1 is the bright mint-lavender regional palette reference; Image 2 defines the same tiny white-haired girl protagonist; Image 3 defines the approved painterly detail, monumental scale, layered side-scroller composition, and cosmic-horror visual language. Create an original new location and make it dramatically brighter than Image 3.
Primary request: future anxiety embodied as a breathtakingly beautiful pastel paradise whose geometry and physics are profoundly wrong; luminous cosmic horror that becomes more disturbing the longer it is viewed
Scene/backdrop: a bright mint and lavender flower field surrounding floating pale stone cities and mirror-calm water, almost heavenly at first glance; a perfectly symmetrical horizon that feels artificial
Subject: the same tiny white-haired girl in a pale dress standing on a clear traversable stone-and-flower platform in the lower third, with a soft lavender contact shadow so she remains readable against the bright background
Key landmarks: a colossal vertical fracture splitting the daylight sky; floating doorways leading nowhere; towers repeated in impossible perfect symmetry; semi-transparent future positions of moving platforms; a cracked platform showing its future collapsed form; clouds subtly revealing a face larger than the entire world; every tree, tower and flower casts a shadow in a different impossible direction; one extremely thin fluorescent cyan line inside the sky fracture
Style/medium: original high-detail painterly 2D game environment concept art, luminous surreal fantasy fused with psychological cosmic horror, delicate hand-painted forms, mint #B7F5D7, lavender #D8C2FF, pale apricot and restrained fluorescent cyan, not pixel art
Composition/framing: cinematic wide side-scroller establishing shot, readable continuous foreground path, interactive floating platforms in the middle ground, immense floating city and face-cloud background, protagonist less than 4 percent of frame height
Lighting/mood: overexposed serene daylight, pearlescent haze, apparently safe and joyful but intensely uncanny, no darkness used as the primary source of fear, no explicit gore
Constraints: no text, no title, no logo, no UI, no border, no watermark; no copied game characters or recognizable architecture; do not make it merely cute or idyllic; preserve clearly readable playable surfaces and wrong-way shadows
```

---

## 04-2. 균열 V2 — Living Paradise

**결과:** `docs/concept-art/04-fracture-living-paradise-v2.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`
3. `docs/concept-art/04-fracture-pastel-abyss.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art, revised Fracture zone for an original 2D side-scrolling metroidvania
Input images: Image 1 defines the mint-lavender-pale-apricot regional palette; Image 2 defines only the tiny white-haired girl protagonist; Image 3 is an earlier Fracture draft and defines the bright atmospheric depth and clear side-scrolling route, but redesign the environment to feel much more biologically alive, deceptively beautiful and disturbing. Create an original scene.
Primary request: a gorgeous pastel future that is secretly one living organism pretending to be a flower field and floating city. First glance should feel heavenly and safe; second glance should reveal skin, organs and impossible biological symmetry; third glance should imply the entire landscape is imitating the protagonist.
Scene/backdrop: an endless luminous mint-and-lavender meadow under overexposed midday light, mirror-calm shallow water, floating pale towers and doorways, soft clouds. Keep the lower third as a clear traversable stone-and-flower path with ledges, one moving platform gap and one cracked platform.
Biological disguise — ground: from a distance the terrain is covered in dense pastel flowers. Up close, flower centers resemble pores, irises and closed eyelids; petals have the soft texture and folds of skin without explicit gore. Roots beneath broken platforms connect like nerves. The ground subtly rises and falls in broad breathing waves.
Biological disguise — architecture: floating towers and bridges use biomorphic growth rules, resembling seed pods, vertebrae, inner-ear spirals and folded organs while remaining usable architecture. A monumental doorway at the center is perfectly beautiful and floral, but its opening resembles a pupil or body aperture only after prolonged viewing. No explicit flesh or anatomy.
Wrong symmetry: arrange the city and meadow in almost perfect radial and bilateral symmetry inspired by microscopic organisms. Introduce small impossible errors: one repeated tower is mirrored but lit from the wrong side; flowers on the left cast shadows rightward while flowers on the right cast shadows upward; reflections show additional buildings that do not exist; a path curves symmetrically but never reaches its own center.
Imitation: place several distant flower-and-vine figures that repeat the white-haired protagonist's silhouette and standing pose. Some are too tall, some child-sized, some fused in pairs. They should initially read as ornamental shrubs and only later as botanical humanoids. Avoid explicit faces.
Cosmic presence: the cloud field behind the central doorway forms an enormous serene incomplete face, almost invisible in the bright light. The sky is split by one hair-thin vertical fluorescent cyan fracture. Behind the fracture, use repeating radial cellular patterns rather than darkness.
Foresight mechanic: show thin semi-transparent lavender afterimages of the moving platform's future position and the cracked platform's collapsed state. These ghosts are reliable and geometrically clean, contrasting with the deceptive environment.
Subject: the same tiny white-haired girl in a pale dress stands near lower center-left, less than 4 percent of frame height. Her colors are slightly duller than the world. Add a soft lavender contact shadow so she remains readable.
Style/medium: rough painterly environment block-in, broad visible brushwork, simplified early concept shapes, luminous surreal biological horror, not polished final key art, not photorealistic, not pixel art.
Color palette: bright mint #B7F5D7, lavender #D8C2FF, pale peach, pearl white, very restrained fluorescent cyan. No dominant black or dark horror lighting.
Mood: serene, immaculate, warm and welcoming at first, then deeply wrong and inhuman; horror through beauty, biological imitation, perfect symmetry and incorrect physics, no gore.
Constraints: no obvious monster standing in the scene, no explicit flesh, blood, organs or wounds; no simple fantasy flower meadow; no text, signature, watermark, logo, UI or border; no copied film scene, artist composition or recognizable architecture; preserve clear side-scrolling gameplay readability.
```

---

## 04-3. 균열 V3 — Liminal Paradise

**결과:** `docs/concept-art/04-fracture-liminal-paradise-v3.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`
3. `docs/concept-art/04-fracture-living-paradise-v2.png`

```text
Use case: stylized-concept
Asset type: rough early-stage 16:9 game environment concept art, Fracture zone V3 for an original 2D side-scrolling metroidvania
Input images: Image 1 defines the mint-lavender-pale-apricot regional palette; Image 2 defines only the tiny white-haired girl protagonist; Image 3 is the previous Fracture concept and defines the luminous atmosphere and side-scrolling scale, but remove nearly all overt horror imagery, visible eyes, sky face, botanical humanoids and obvious biological architecture.
Primary request: redesign Fracture as 95 percent beautiful serene pastel liminal paradise and only 5 percent subtly biological and wrong. Use the psychological spatial principles of liminal backrooms-like spaces—repetition, emptiness, transitional architecture, missing purpose, impossible distance and looping geometry—but do not copy yellow wallpaper, fluorescent office corridors, any specific Backrooms level, logo or recognizable layout.
Scene/backdrop: an immaculate pastel garden plaza and shallow reflecting pool under endless soft midday light. Mint sky, lavender flowers, pearl-white stone, pale apricot sunlight. Empty arcades, staircases, doorways, covered walkways, benches and small waiting areas appear designed for people but have no inhabitants and no clear function. Indoor and outdoor space blend without a visible boundary.
Liminal spatial wrongness: repeat the same doorway and bench at exact intervals far into the horizon. A corridor begins outdoors and continues into the sky. Two identical staircases rise to the same landing from incompatible directions. A path loops back to its starting point while appearing straight. The central doorway becomes slightly smaller as the protagonist approaches. Distant architecture repeats at the same visual size instead of shrinking with perspective. There is no sun, yet the whole world is evenly lit.
Wrong symmetry and physics: the composition is almost perfectly bilateral, but one tower is missing from reality and appears only in the water reflection. Left-side flowers cast shadows to the right, right-side flowers cast shadows upward, and the protagonist's shadow is displaced slightly ahead of her. Reflections lag behind physical objects by one position. Use one hair-thin vertical cyan fracture in the far sky as the only bright anomaly.
Biological hint — maximum 5 percent: most flowers must look completely normal and beautiful. Only a tiny cluster near one broken platform has centers resembling closed pores rather than eyes. Roots under one platform connect like very pale nerves. The meadow surface rises in one almost imperceptible broad breathing wave. One white stone wall has a subtle shell or skin-like seam, but no flesh color, organs, face or explicit anatomy.
Foresight mechanic: include clean semi-transparent lavender afterimages of a moving platform's future location and one cracked platform's future collapsed state. These future ghosts are geometrically precise and trustworthy, contrasting with the spatially deceptive world.
Subject: the same tiny white-haired girl in a pale dress stands alone near lower center-left, less than 4 percent of frame height. Her colors are slightly duller than the environment, with a soft lavender contact shadow for readability.
Style/medium: rough painterly environment block-in, broad visible brushwork, simplified early concept shapes, luminous surreal liminal fantasy with restrained cosmic unease, not polished final key art, not photorealistic, not pixel art.
Composition/framing: wide 16:9 side-scrolling view, clear continuous foreground route with practical ledges and one platform gap, empty midground plaza and repeating corridors, impossible serene horizon. Preserve game-level readability.
Mood: silent, safe, warm, immaculate and empty at first; gradually uncomfortable because the space repeats, perspective fails and nothing has a purpose. Horror must come from spatial logic rather than monsters or grotesque imagery.
Constraints: no visible eyes except no eyes at all, no sky face, no humanoid plants, no obvious monster, no explicit biological building, no blood, flesh, organs or gore; no yellow-office Backrooms imitation; no text, signature, watermark, logo, UI or border; no copied film scene, artist composition or recognizable architecture.
```

---

## 05. 엔딩 — False Awakening Bedroom

**결과:** `docs/concept-art/05-false-awakening-bedroom.png`

**참고 이미지**

1. `docs/mood_img.png`
2. `docs/concept-art/02-residue-ruins.png`
3. `docs/concept-art/03-gaze-cathedral.png`
4. `docs/concept-art/04-fracture-pastel-abyss.png`

```text
Use case: stylized-concept
Asset type: 16:9 wide game environment concept art, False Awakening ending scene for the same original psychological cosmic-horror game
Input images: Image 1 is the project palette reference; Images 2, 3 and 4 define the approved painterly visual language and the subtle visual traces of Residue, Gaze and Fracture. Create a new original first-person bedroom scene, not another side-scrolling landscape.
Primary request: a quiet ordinary bedroom that appears safe at first glance but is undeniably impossible and deeply frightening on closer inspection, the false-awakening climax
Scene/backdrop: first-person view from someone lying in bed at night, old plaster wall, simple window with pale moonlight, bedside table, curtain, candle, wooden floor, rumpled blanket at the bottom edge; believable human scale and calm composition
Key anomalies: the candle flame burns downward beneath the wick; the bed and furniture shadows climb vertically toward the ceiling despite side moonlight; wall cracks bend like articulated finger joints; part of the ceiling silently opens into a vast star-filled abyss with a faint incomplete eye; the window reflection shows a room with subtly different geometry; the wall texture very faintly resembles staring eyelids
Traces of the three zones: a few desaturated navy fragments suspended near a dark corner for Residue; a faint purple-teal eye-shaped stain behind the curtain for Gaze; one hair-thin mint-lavender fluorescent fracture in the ceiling for Fracture. Keep these subtle, not decorative symbols.
Style/medium: original high-detail painterly 2D game concept art, cinematic first-person illustration, restrained surreal cosmic horror, same elegant brushwork and atmospheric rendering as the references, not photorealistic, no explicit gore
Composition/framing: wide 16:9 view from the pillow, bed and hands/blanket as foreground framing, room furniture in the middle ground, window and impossible ceiling as background; the scene should read as normal for one second before the anomalies become visible
Lighting/mood: near-silent blue-gray moonlight, tiny muted orange candle accent, deep indigo-violet abyss, calm and intimate yet increasingly unbearable; strong psychological dread
Constraints: no visible face of the protagonist, no monster standing in the room, no text, no title, no logo, no UI, no border, no watermark, no copied game imagery; anomalies must remain visually legible but integrated into a believable bedroom
```

