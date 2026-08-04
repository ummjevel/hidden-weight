# Fracture BGM Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Import the generated Fracture music unchanged and assign it to the Fracture zone.

**Architecture:** Follow the existing `Residue_BGM.mp3` asset pattern. The zone's `ZoneData.bgm` reference overrides the procedural fallback already implemented by `AudioManager.PlayZoneBgm()`.

**Tech Stack:** Unity YAML assets, Unity AudioImporter metadata, MP3

## Global Constraints

- Do not re-encode or edit the source music.
- Do not change any zone other than Fracture.
- Preserve the existing source file in Downloads.

---

### Task 1: Import and assign the Fracture BGM

**Files:**
- Create: `Assets/Audio/Fracture_BGM.mp3`
- Create: `Assets/Audio/Fracture_BGM.mp3.meta`
- Modify: `Assets/ScriptableObjects/Zone_Fracture.asset`

**Interfaces:**
- Consumes: `ZoneData.bgm`, `AudioManager.PlayZoneBgm(ZoneData, float)`
- Produces: A valid Unity `AudioClip` reference for the Fracture zone

- [x] **Step 1:** Record the source SHA-256 hash and verify it is a readable stereo MP3.
- [x] **Step 2:** Copy the source bytes to `Assets/Audio/Fracture_BGM.mp3`.
- [x] **Step 3:** Add AudioImporter metadata matching `Residue_BGM.mp3.meta`, using a unique GUID.
- [x] **Step 4:** Set `Zone_Fracture.asset`'s `bgm` to `{fileID: 8300000, guid: 126c76e0d50c4ebea36a3836a05fc6fc, type: 3}`.
- [x] **Step 5:** Verify matching hashes, matching GUID references, and unchanged Gaze/Residue zone assignments.
