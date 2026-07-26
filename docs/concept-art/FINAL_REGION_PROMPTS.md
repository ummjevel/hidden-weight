# Hidden Weight 최종 지역 이미지 프롬프트

잔재·응시·균열에서 최종 선택한 콘셉트 이미지와 생성 프롬프트만 정리한 문서다.

| 지역 | 최종 이미지 |
| --- | --- |
| 잔재 | `docs/concept-art/02-residue-cloud-face-v6.png` |
| 응시 | `docs/concept-art/03-gaze-sky-observer-v2.png` |
| 균열 | `docs/concept-art/04-fracture-liminal-paradise-v3.png` |

최종 3장 외 중간 버전 이미지(프롤로그, 잔재 V1~V5, 응시 V1/V3, 균열 V1/V2, 엔딩)는 삭제했다. 아래 프롬프트의 입력 이미지 중 삭제된 파일은 `(삭제됨)`으로 표시했다.

---

## 1. 잔재 — Cloud Face V6

**결과 이미지:** `docs/concept-art/02-residue-cloud-face-v6.png`

**입력 이미지:** `docs/concept-art/02-residue-crouching-presence-v5.png` (삭제됨)

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

## 2. 응시 — Sky Observer V2

**결과 이미지:** `docs/concept-art/03-gaze-sky-observer-v2.png`

**입력 이미지**

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

## 3. 균열 — Liminal Paradise V3

**결과 이미지:** `docs/concept-art/04-fracture-liminal-paradise-v3.png`

**입력 이미지**

1. `docs/mood_img.png`
2. `docs/character_sprite_ref.png`
3. `docs/concept-art/04-fracture-living-paradise-v2.png` (삭제됨)

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
