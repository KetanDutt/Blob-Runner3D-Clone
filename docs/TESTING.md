# Testing and verification

## Automated tests (Unity Test Runner, EditMode)

`Assets/_Main/Tests/EditMode` – NUnit tests for the pure logic in `BlobRunner.Core` (they need no scene, no physics, no rendering):

| Suite | What it protects |
|---|---|
| `GameStateMachineTests` | legal / illegal transitions, terminal states, events |
| `LevelGeneratorTests` | determinism, fairness rules for levels 1 – 80, growth of length / speed / density, walls limited and supplied, loot placement, needles and sliders only on later levels |
| `DifficultyModelTests` | cut masks per bar height, target curve, estimator behaviour, **generated levels follow the difficulty target**, difficulty rises over the levels |
| `ScoreCalculatorTests` | stars, score components, clamping, bad input |
| `SaveDataTests` | defaults, sanitising of corrupt saves, progression, best score |
| `AdaptiveQualityGovernorTests` | no change at 60 fps, steps down on slow devices, ignores hitches, recovers once, never flip-flops |
| `ProceduralSynthTests` | every sound finite / bounded / audible / click free / deterministic, correct pitches, music loop length and seam, WAV encoder |
| `RngTests`, `BodyPartStateTests` | reproducible random numbers; enum values serialized in `Player.prefab` never change |

Run: `Window → General → Test Runner → EditMode → Run All`, or headless:

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile -
```

## Project validator (no Unity needed)

```
python3 tools/validate_project.py [--strict]
```

Checks `.meta` files (missing / orphaned / duplicate GUIDs), Unity YAML structure, every GUID and fileID reference, script ↔ class names, the build scene list, junk files tracked by git, assembly definitions, `UnityEditor` usage in runtime code and the Player prefab rules (unique body states 1 – 11, cascades contain the part itself). Package GUIDs that live outside the repository are listed in `tools/known_external_guids.txt`. It runs in CI on every push / PR.

## How this change set was verified

No Unity Editor was available, so the work was verified with what could be run:

1. **Compiled against Unity's own API.** All C# was compiled with Roslyn against reference assemblies built from Unity's public C# reference source (2021.3: engine modules, plus uGUI and Cinemachine 2.8.6) and the project's `DOTween.dll`, modelling Unity's assembly rules (`Assembly-CSharp-firstpass`, `Assembly-CSharp`, the asmdef assemblies). `BlobRunner.Core` is compiled *without any Unity reference* to prove it is engine independent. Result: 0 errors, 0 warnings in the project's own assemblies (only the third party Graphy assembly could not be built in that setup).
2. **Executed the unit tests** on a .NET runtime with an NUnit-compatible shim: 107 test executions, all green.
3. **Looked at the audio**: every sound effect and the music loop were rendered and inspected as waveforms and spectrograms (pitches, envelopes, loop structure) – not by ear.
4. **Simulated the difficulty**: Monte-Carlo runs of the generated levels for casual / average / expert players tuned the generator and the loot density.
5. **Parsed and cross-checked all Unity YAML** that was edited (scene, prefabs, project settings) and mutation-tested the validator (it catches re-introduced prefab bug, missing / orphaned / duplicate metas, dangling GUIDs, class ↔ file mismatches, tracked junk).

What this does **not** cover is anything that needs a real Unity runtime or a device: visuals of the new effects and UI, feel / tuning, frame rate on hardware, audio taste. Use the checklist below for the first run.

## Manual QA checklist

**Flow** – menu shows level / best score; tap anywhere and PLAY both start; pause (button, Esc, home button) freezes the game and resumes cleanly; MENU / RESTART from pause; finish → stars + score count-up + NEXT LEVEL; death → TRY AGAIN; the level number and best score survive an app restart.

**Mechanics** – cut at every bar height (shin, thigh, hip, chest, head) removes the expected parts (see GAMEPLAY.md); limp / crawl animation follows the legs; loot regrows everything and recolours; loot spawned on generated levels works; sliding bars and needle rows are passable; the finish gate triggers.

**Feel** – shake / FOV punch / hit-stop are noticeable but not nauseating; sounds are not too loud together; haptics only on Android and only when enabled; particles are visible against every theme.

**UI** – no overlap on 9:16, 9:19.5, 3:4 and notch devices; text readable; buttons give feedback; the three switches work in menu and pause.

**Technical** – no console errors / warnings on a fresh clone; no `.meta` files change after the first import; `DOTween` shows no "target is null" messages after a restart; steady 60 fps on the target low-end device, resolution scale settles and stays.
