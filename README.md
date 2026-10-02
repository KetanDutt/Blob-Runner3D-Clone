# Blob Runner 3D

[![Validate project](https://github.com/KetanDutt/Blob-Runner3D-Clone/actions/workflows/validate.yml/badge.svg)](https://github.com/KetanDutt/Blob-Runner3D-Clone/actions/workflows/validate.yml)
![Unity 2021.3.5f1](https://img.shields.io/badge/Unity-2021.3.5f1-black?logo=unity)
![Platforms](https://img.shields.io/badge/platforms-Android%20%7C%20iOS%20%7C%20Desktop-blue)

A clone of ***Blob Runner*** for mobile, built in Unity. You run as a **jelly**, steer through hazard bars that **cut your body apart**, collect **loot that makes the lost parts grow back**, and try to reach the finish line with as much jelly left as possible.

The main technical challenge is the jelly look on mobile hardware: the character is **ray marched** (signed distance functions blended with a smooth minimum) inside a proxy cube. It was created with the **Blob Character Creator Tool** by tunchasan:
🌟 <https://github.com/tunchasan/Blob-Character-Creator>

<p float="left">
  <img src="https://user-images.githubusercontent.com/39636292/115233919-29127400-a121-11eb-835a-96055a738751.gif" width="405">
  <img src="https://user-images.githubusercontent.com/39636292/115234979-4f84df00-a122-11eb-80b7-0f03414e7dbb.gif" width="405">
</p>

## Features

**Core mechanics** (from the original prototype)

* 👉 Dynamic animation system that depends on the broken parts of the body (run → hop → crawl)
* 👉 Soft-cut system that breaks the player's body parts that are hit, including cascades (a chest hit takes the arms and head)
* 👉 Loot system that regrows the broken parts from the place where the loot was collected

**Added in this version**

* 🎮 Complete game flow: main menu, HUD, pause, win and lose popups, retry / next level, back button and auto-pause
* 🗺️ **Endless level progression**: hand-authored level 1, then generated levels with a smooth, *measured* difficulty curve (hazard rows, undodgeable walls, needle gaps, sliding bars; six colour themes)
* ⭐ Score, 1 – 3 stars, best score and progress saved between sessions; sound / music / vibration settings
* 🔊 Fully procedural **sound effects and music** (no audio files in the repository, replaceable with recordings)
* ✨ Particle effects (jelly splats, sparkles, confetti, run dust), contact shadow, camera shake / FOV kicks, hit-stop, floating texts, Android haptics
* 📱 Portrait UI that scales to every phone (1080 × 1920 reference, safe-area aware)
* ⚡ Performance work for the ray marched character: 60 fps cap, adaptive resolution, no per-frame allocations, cheaper render settings
* 🧪 Unit tests for the game logic, a project validator and CI ([TESTING.md](docs/TESTING.md))
* 🐞 A long list of fixed defects, see [docs/BUGS_FIXED.md](docs/BUGS_FIXED.md)

## Getting started

1. Install **Unity 2021.3.5f1** (any 2021.3 LTS patch works) – for Android add the *Android Build Support* module with *IL2CPP*.
2. Clone the repository and open the folder with Unity Hub.
3. Open `Assets/_Main/Scenes/Scene_001.unity` and press **Play** (use a portrait Game view, e.g. 1080 × 1920).

### Controls

| | |
|---|---|
| Touch | press anywhere and drag left / right |
| Keyboard | `A` / `D` or ← / → |
| Pause | pause button, `Esc` / Android back button |

## Project layout

```
Assets/_Main
├── Scripts/Core       engine-independent logic (state machine, level generator, difficulty model, score, save data, audio synthesis) – assembly BlobRunner.Core
├── Scripts/GamePlay   player, body parts, loot, merge / regrow
├── Scripts/Managers   GameManager (flow, scoring, progression)
├── Scripts/Services   save game, audio, haptics, performance (persist across restarts)
├── Scripts/Effects    particles, camera feel, feedback hub, contact shadow
├── Scripts/Levels     level builder, themes, moving obstacles
├── Scripts/UI         code-built UI (menu, HUD, pause, results)
├── Shader             ray marching shaders and the helpers that feed them
├── Prefabs · Materials · Motions · 3D · Scenes · SO
└── Tests/EditMode     NUnit tests
docs/                  full documentation
tools/                 validate_project.py (+ known package GUIDs)
```

## Documentation

Everything is in [`docs/`](docs/README.md):
[Architecture](docs/ARCHITECTURE.md) · [Gameplay & level generation](docs/GAMEPLAY.md) · [Audio, VFX & UI](docs/AUDIO_VFX_UI.md) · [Performance](docs/PERFORMANCE.md) · [Bugs fixed](docs/BUGS_FIXED.md) · [Build & release](docs/BUILD_AND_RELEASE.md) · [Testing](docs/TESTING.md) · [Roadmap](docs/ROADMAP.md) · [Third party](docs/THIRD_PARTY.md)

## Tests and validation

* Unity: `Window → General → Test Runner → EditMode → Run All`
* Anywhere (also runs in CI): `python3 tools/validate_project.py --strict`

## Building

See [docs/BUILD_AND_RELEASE.md](docs/BUILD_AND_RELEASE.md) – the project is configured for portrait, IL2CPP and ARMv7 + ARM64 on Android. **Set your own company name / package id before publishing.**

## Credits and licence

Third party components are listed in [docs/THIRD_PARTY.md](docs/THIRD_PARTY.md). The game code is © 2026 Ketan Dutt, all rights reserved – see [LICENSE](LICENSE).
