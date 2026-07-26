# Hidden Weight Region Concept Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate and validate five cohesive 16:9 cosmic-horror environment concept images for Hidden Weight.

**Architecture:** Generate each scene as an independent raster concept while reusing the same two project reference images, protagonist description, composition rules, and recurring motifs. Save accepted outputs under `docs/concept-art/` with stable scene-based filenames.

**Tech Stack:** Built-in ImageGen, PNG raster output, local visual inspection.

## Global Constraints

- Generate five scenes: Prologue, Residue, Gaze, Fracture, False Awakening.
- Use psychological cosmic horror 70%, organic grotesque detail 20%, beautiful surrealism 10%.
- Use a wide side-scrolling game composition with foreground, traversable ground, midground, and background.
- Preserve a tiny white-haired girl protagonist with a pale dress and readable silhouette.
- No text, logo, UI, watermark, copied characters, or copied architecture.
- Keep the brightness progression from dark Prologue/Residue to bright Fracture without reducing horror intensity.
- Use `docs/mood_img.png` as the regional palette and atmosphere reference.
- Use `docs/character_sprite_ref.png` as the protagonist reference.

---

### Task 1: Prologue Concept

**Files:**
- Create: `docs/concept-art/01-prologue-cosmic-awakening.png`

- [ ] Generate a wide cosmic-womb environment with a planetary closed eye, crescent platforms, upward-hanging threads, and a tiny awakening protagonist.
- [ ] Inspect for side-scroller readability, scale, original design, and absence of text.
- [ ] Save the accepted PNG using the specified filename.

### Task 2: Residue Concept

**Files:**
- Create: `docs/concept-art/02-residue-ruins.png`

- [ ] Generate dark navy ruins with rib-like arches, suspended bridge fragments, a hollow monumental statue, and dim orange accents.
- [ ] Inspect for the guilt/rewind theme, foreground-to-background separation, and protagonist readability.
- [ ] Save the accepted PNG using the specified filename.

### Task 3: Gaze Concept

**Files:**
- Create: `docs/concept-art/03-gaze-cathedral.png`

- [ ] Generate a purple-teal cathedral-prison-theater with mismatched eyes, hanging cages, faceless spectators, a narrow passage, and a tiny protagonist.
- [ ] Inspect for the shame/surveillance theme, clear gameplay ground, and controlled grotesque detail.
- [ ] Save the accepted PNG using the specified filename.

### Task 4: Fracture Concept

**Files:**
- Create: `docs/concept-art/04-fracture-pastel-abyss.png`

- [ ] Generate a bright mint-lavender flower field and floating city with impossible symmetry, a vertical sky fracture, wrong-way shadows, and a colossal face behind clouds.
- [ ] Inspect for bright cosmic horror, readable protagonist shadow, and distinct foreground platforms.
- [ ] Save the accepted PNG using the specified filename.

### Task 5: False Awakening Concept

**Files:**
- Create: `docs/concept-art/05-false-awakening-bedroom.png`

- [ ] Generate a first-person bedroom with an inverted candle flame, upward shadow, joint-like wall cracks, and a ceiling opening into a star abyss.
- [ ] Inspect for a deceptively normal first read, subtle traces of all three regions, and no explicit gore.
- [ ] Save the accepted PNG using the specified filename.

### Task 6: Set Validation

**Files:**
- Inspect: `docs/concept-art/*.png`

- [ ] Confirm all five images exist and are readable PNG files.
- [ ] Confirm the scenes share a painterly visual language and recurring eye/circle, impossible gravity, and wrong-shadow motifs.
- [ ] Confirm the three main zones retain their documented regional palettes.
- [ ] Confirm every image contains no text, logos, UI, or watermark.
