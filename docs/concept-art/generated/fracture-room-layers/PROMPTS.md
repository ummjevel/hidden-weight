# 균열 룸 패럴랙스 생성 프롬프트

## 공통 기준

- 룸 구도: `docs/concept-art/generated/fracture-map-v1/rooms/*.png`
- 팔레트: 파스텔 민트, 빙청색, 라벤더, 옅은 살구색, 백색 대리석
- 출력: 1672×941
- 캐릭터, 적, 아이템, UI, 텍스트, 실제 발판, 활성 기물 제외

## BG_Far

```text
Use case: stylized-concept.
Asset type: opaque far-background layer for a wide 2D side-scrolling liminal-fantasy metroidvania.
The referenced Fracture room concept is the authoritative composition, palette and landmark source.
Create only luminous sky, distant reflective water, farthest pale-marble ruins, repeating arches,
distant greenhouse silhouettes, remote watchtower and sky fracture when visible.
Remove the near gameplay floor, close platforms, foreground flowers, nearby columns, doors,
interactive devices and every character or creature.
Keep contrast quiet and depth atmospheric. Fill the entire frame with no transparency.
No text, UI, labels, watermark, wireframes or explicit horror.
```

## BG_Mid

```text
Use case: stylized-concept.
Asset type: transparent middle-background layer for a wide 2D side-scrolling liminal-fantasy metroidvania.
Use the referenced room concept for exact architecture and viewpoint.
Create separated middle-distance pale-marble arches, greenhouse frames, towers, inactive bridges,
waterline ruins and lavender vine clusters over one perfectly flat solid #00FF00 background.
Do not include sky, a full scene, gameplay floor, walkable platform, active door, device, character,
enemy, item, foreground framing, text, UI or watermark.
The green background is perfectly uniform with no shadow, gradient, texture or reflection.
Do not use green inside objects.
```

## FG_Overlay

```text
Use case: stylized-concept.
Asset type: transparent foreground edge-framing layer for a wide 2D side-scrolling liminal-fantasy metroidvania.
Use the referenced room concept for palette and viewpoint.
Create only cropped close lavender flowers, pale-marble column edges, partial glass ribs,
soft refracted-light ribbons and small lower-corner ruins over one perfectly flat solid #00FF00 background.
Every object remains near a frame edge; keep the central 65 percent open.
Never form a continuous walkable top edge or gameplay object.
No character, enemy, item, UI, text, label, watermark, sky or full-frame scene.
The green background is perfectly uniform with no shadow, gradient, texture or reflection.
Do not use green inside objects.
```

## 룸 매핑

| 적용 폴더 | 콘셉트 |
| --- | --- |
| Room01 | `01-glass-garden.png` |
| Room02 | `02-misaligned-promenade.png` |
| Room03 | `03-possibility-plaza.png` |
| Room04 | `04-swaying-lower-garden.png` |
| Room05 | `05-foresight-sanctuary.png` |
| Room06 | `06-time-lag-greenhouse.png` |
| Room07 | `07-floating-architecture.png` |
| Room08 | `08-reversing-elevator-shaft.png` |
| Room09 | `09-mirrored-possibility-hall.png` |
| Room10 | `10-second-hand-watchtower.png` |
| Room11 | `11-not-yet-ruins.png` |
| Room12 | `12-tomorrows-fracture.png` |
| Secret01 | `S1-abandoned-possibility.png` |
| Secret02 | `S2-still-afternoon.png` |
| Secret03 | `S3-unselected-door.png` |
