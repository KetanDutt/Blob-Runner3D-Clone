# Changelog

## 1.0.0 – production pass

Everything below was added or changed on top of the original prototype. Details and rationale: [docs/](docs/README.md).

### Added
- Game flow: main menu, HUD, pause, win / lose popups, retry / next level, auto-pause, back button, scene transitions.
- Level progression: level 1 stays hand authored; levels ≥ 2 are generated deterministically with a measured difficulty curve (undodgeable walls, needle gaps, sliding bars, six colour themes).
- Score, 1 – 3 stars, best score and settings persisted in a validated, versioned save game.
- Procedural sound effects and music (`ProceduralSynth`), haptics, particle effects, camera shake / FOV kicks, hit-stop, floating texts, contact shadow, patterned floor, hazard-striped bars, visible finish gate.
- Adaptive resolution for the ray marched character, 60 fps cap, frame pacing.
- `BlobRunner.Core` assembly (no Unity dependency) with 107 unit test executions, `tools/validate_project.py` and GitHub Actions workflows.
- Documentation in `docs/`, rewritten README, this changelog.

### Fixed
- 23 gameplay / data defects (wrong body state in `Player.prefab`, endless death loop, instant restart on finish, biased randomness, leaked / shared materials, loot prefab without renderer, `Joystick.AxisOptions` recursion, …) and 10 configuration defects – see [docs/BUGS_FIXED.md](docs/BUGS_FIXED.md).
- Ray marching shaders no longer use Windows path separators in `#include`.

### Changed
- Player settings: portrait only, IL2CPP + ARMv7 / ARM64 on Android, application id / company / version, cloud ids of the sample project removed.
- Canvas scaler: 1080 × 1920 reference instead of constant pixel size.
- Camera HDR and the (caster-less) realtime shadows are off.
- `GameManager`, `Player`, `PController`, `BodyPart`, `LootContainer`, `MergeController`, `JellyContainer`, `TransformProvider`, `ShaderSetting*` rewritten (same serialized field names, so the scene and prefabs keep their data); scripts are namespaced under `BlobRunner`.

### Removed
- Unused packages (URP + dependencies, Post Processing, Timeline, Collab, 2D sprite packages) and their asset files.
- Example scenes / PDFs of third party packs, `.DS_Store`, tracked `UserSettings/`, 23 orphaned `.meta` files, stale generator asset, empty scripts, the `Singleton<T>` helper, the unused `ASpeed` tag.
