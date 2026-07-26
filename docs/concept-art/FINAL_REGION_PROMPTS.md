# Hidden Weight 최종 지역 이미지 프롬프트

잔재·응시·균열에서 최종 선택한 콘셉트 이미지와 생성 프롬프트만 정리한 문서다.

| 지역 | 최종 이미지 |
| --- | --- |
| 잔재 | `docs/concept-art/02-residue-cloud-face-v6.png` |
| 응시 | `docs/concept-art/03-gaze-eye-swarm-v3.png` |
| 균열 | `docs/concept-art/04-fracture-liminal-paradise-v3.png` |

---

## 1. 잔재 — Cloud Face V6

**결과 이미지:** `docs/concept-art/02-residue-cloud-face-v6.png`

**입력 이미지:** `docs/concept-art/02-residue-crouching-presence-v5.png`

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

## 2. 응시 — Eye Swarm V3

**결과 이미지:** `docs/concept-art/03-gaze-eye-swarm-v3.png`

**입력 이미지**

1. `docs/concept-art/03-gaze-sky-observer-v2.png`
2. 사용자 제공 눈 군집 참고 이미지
3. 사용자 제공 구름형 시각기관 참고 이미지
4. `docs/character_sprite_ref.png`

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

## 3. 균열 — Liminal Paradise V3

**결과 이미지:** `docs/concept-art/04-fracture-liminal-paradise-v3.png`

**입력 이미지**

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

