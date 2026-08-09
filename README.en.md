[한국어](README.md)

# Hidden Weight (Waking Dream)

> A metroidvania where you explore a dream world, experience different emotions, and those emotions
> reshape both play and world. You grow through emotion and ultimately journey to find your "self"
> and return to reality.

## Play & Video

| Item | Link |
|---|---|
| Play link / installer | https://ummjevel.itch.io/hidden-weight |
| Gameplay video | https://youtu.be/6jfNqsfcH8g?si=b2SN3ac4RHLJu3vk |

## About

*Hidden Weight* is a single-player 2D side-scrolling action-adventure (metroidvania). Instead of collecting emotions, the player uses emotion-born abilities to reinterpret space and combat, revisiting places already explored to piece together scattered memories and the meaning of the world.

The world is split into three regions along a timeline.

| Region | Emotion / time | Color |
|---|---|---|
| **Residue** | Past · Guilt | Dark, heavy amber |
| **Gaze** | Present · Shame | Tension of gaze and teal |
| **Fracture** | Future · Anxiety | Bright but subtly misaligned colors and shapes |

## Core Systems

The emotion abilities gained while moving through the regions simultaneously change movement, combat, and how the environment is read.

| Ability | Key | Effect |
|---|---|---|
| Rewind | K (auto-mapped per region) | Reverts a target to a previous state, opening a path |
| Hush | K (auto-mapped per region) | Lowers your presence to avoid being detected by threats |
| Foresight | K (auto-mapped per region) | Reveals a location or state ahead of time |

See [`docs/EMOTION_SYSTEM.md`](docs/EMOTION_SYSTEM.md) for exact numbers and gating rules.

### Controls

| Action | Keyboard |
|---|---|
| Move | A / D or arrow keys |
| Run | Hold Shift |
| Jump / wall jump | Space |
| Dash | Left Ctrl |
| Attack | J |
| Emotion skill | K |
| Pause | Esc |

Keyboard and gamepad are equally supported.

## Tech Stack

- **Engine**: Unity 6000.5.4f1
- **Rendering**: URP (Universal Render Pipeline) 17.5.0, 2D Renderer
- **Language**: C#
- **Input**: Legacy `Input` system (the Input System package is installed but unused)

## Project Structure

```
hidden-weight/
├── CLAUDE.md              Design context (users, brand personality, tone)
├── PROJECT_STRUCTURE.md   Unity project settings, module structure, batch-mode commands
├── docs/                  Full design, level, art, audio, and submission documentation (see below)
└── HiddenWeight/          Unity project root
    └── Assets/Scripts/    Game code (9 modules)
```

Code under `Assets/Scripts/` is split into 9 modules with a strictly unidirectional dependency graph.

```
Data ← Core ← Player ← { World, Enemies } ← Emotions ← { UI, Ending }
```

| Module | Responsibility |
|---|---|
| `Core` | Global game state, scene flow, save/load, audio |
| `Data` | ScriptableObject-based balance data tables |
| `Player` | Movement, combat, and survival state machine |
| `World` | Rooms, platforms, gates, emotion-interaction interfaces, portal-based room system |
| `Emotions` | Emotion skills (Rewind / Hush / Foresight) |
| `Enemies` | Base enemy class and per-region behavior patterns |
| `Ending` | Ending sequence direction |
| `UI` | Code-built HUD, menus, fragment log, etc. |
| `Editor` | Batch-mode tooling for project setup and builds |

See [`docs/code/README.md`](docs/code/README.md) for detailed per-module architecture, and [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md) for full project settings and dependency rules.

## Documentation Map (`docs/`)

| Category | Documents |
|---|---|
| Design | [`GAME_DESIGN.md`](docs/GAME_DESIGN.md), [`EMOTION_SYSTEM.md`](docs/EMOTION_SYSTEM.md), [`WORLD_MAP.md`](docs/WORLD_MAP.md), [`CONTENT_SYSTEM.md`](docs/CONTENT_SYSTEM.md), [`NARRATIVE_CONTENT.md`](docs/NARRATIVE_CONTENT.md), [`DESIGN_IMPLEMENTATION_GAP_ANALYSIS.md`](docs/DESIGN_IMPLEMENTATION_GAP_ANALYSIS.md) |
| Level design | [`LEVEL_00_INDEX.md`](docs/LEVEL_00_INDEX.md) and per-region docs (`LEVEL_10~50_*.md`) |
| UI / Art / Audio | [`UI_UX_DESIGN.md`](docs/UI_UX_DESIGN.md), [`ANIMATION_ART_SPEC.md`](docs/ANIMATION_ART_SPEC.md), [`concept-art/`](docs/concept-art/), per-region audio generation docs |
| Code architecture | [`code/README.md`](docs/code/README.md) |
| Production / submission | `PRODUCTION_*.md`, [`submission/`](docs/submission/) |

## Build & Run

1. Open the `HiddenWeight/` folder as a project in Unity Hub. (Required version: **6000.5.4f1**)
2. Batch-mode compile, test, and build commands are available (substitute paths for your environment).

```bash
# Verify compilation
<UNITY_PATH> -batchmode -quit -nographics -projectPath <PROJECT_PATH>/HiddenWeight \
  -executeMethod HiddenWeight.EditorTools.BuildScript.Compile

# macOS build
<UNITY_PATH> -batchmode -quit -nographics -projectPath <PROJECT_PATH>/HiddenWeight \
  -executeMethod HiddenWeight.EditorTools.BuildScript.BuildMac
```

To regenerate the project from scratch, run the `Editor` module's batch-mode tools in order: `ProjectSetup.Run` → `DataAssetBuilder.Run` → `PlaceholderArtBuilder.Run` → `PrefabBuilder.Run` → `ZoneSceneBuilder.Run`. See [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md) for the full procedure and exact commands.

To preview a WebGL build locally, run `HiddenWeight/WebBuild/run_local_server.sh` (or `.bat`). It's a minimal local server that avoids the CORS/MIME issues that occur when opening a WebGL build via `file://`.

## Tests

`HiddenWeight/Assets/Tests/` contains two kinds of tests.

- **EditMode**: pure logic tests (`ProgressStateTests`, `SaveServiceTests`, etc.)
- **PlayMode**: tests that drive actual scenes, including zone traversal and full playthroughs

Use the `-runTests` flag to run them in Unity batch mode. See [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md) §6 for exact commands.

## Team

**IF98** (working name)

Members: Kim Seunghyeok, Lim Chaewon, Jeon Minjeong

See [`docs/submission/SUBMISSION_00_FACTS.md`](docs/submission/SUBMISSION_00_FACTS.md) for roles and AI tool usage disclosure.
