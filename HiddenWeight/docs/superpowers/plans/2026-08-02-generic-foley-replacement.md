# Generic Foley Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace gong-like runtime feedback with ordinary CC0 foley without breaking Unity asset references.

**Architecture:** A deterministic Python builder decodes selected Kenney OGG files, trims and mixes dry layers, and overwrites the existing runtime WAV paths after making an external backup. Existing filenames and Unity metadata remain stable, so no gameplay code change is required.

**Tech Stack:** Python 3.11, NumPy, SciPy, macOS `afconvert`, Unity WAV assets

## Global Constraints

- Use only official Kenney CC0 packs and retain their license files.
- Never modify BGM, ambience, rewind, healing, checkpoint, pickup, or UI audio.
- Player hurt and death must contain no vocal source.
- Preserve every overwritten WAV in the dated external backup.

---

### Task 1: Build deterministic generic foley masters

**Files:**
- Create: `/Users/ksh/Desktop/sound/tools/build_generic_foley.py`
- Create: `/Users/ksh/Desktop/sound/tools/test_build_generic_foley.py`
- Create: `/Users/ksh/Desktop/sound/CC0_Kenney/`
- Create: `/Users/ksh/Desktop/sound/Generic_Foley_Replacement/`

**Interfaces:**
- Consumes: Kenney OGG paths and a replacement manifest
- Produces: 48 kHz mono PCM16 WAV masters and `REPLACEMENT_REPORT.csv`

- [x] **Step 1:** Write failing unit tests for trimming, layer mixing, peak normalization, source-policy rejection, and PCM16 output.
- [x] **Step 2:** Run the unit tests and confirm failure because the builder module does not exist.
- [x] **Step 3:** Implement the minimal builder and replacement manifest.
- [x] **Step 4:** Run all builder tests and confirm they pass.
- [x] **Step 5:** Build all staged replacement WAVs and validate their format and clipping gates.

### Task 2: Back up and install runtime replacements

**Files:**
- Create: `/Users/ksh/Desktop/sound/Backups/Unity_SFX_PreGenericFoley_2026-08-02/`
- Modify: existing WAV files under `Assets/Resources/Audio/SFX/`
- Modify: `Assets/Audio/SFX_SOURCES.md`

**Interfaces:**
- Consumes: staged WAV masters from Task 1
- Produces: runtime clips loaded by the existing `SfxCue` folders

- [x] **Step 1:** Copy every target runtime WAV and its relative path into the dated backup.
- [x] **Step 2:** Verify the backup count and hashes before overwriting anything.
- [x] **Step 3:** Install staged masters using the existing runtime filenames.
- [x] **Step 4:** Update provenance documentation with Kenney URLs, CC0 status, and the exact replaced cue groups.
- [x] **Step 5:** Verify Unity metadata paths are unchanged, audio format gates pass, protected cue folders are untouched, and project tests still pass.
