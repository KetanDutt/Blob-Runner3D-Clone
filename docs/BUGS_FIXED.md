# Defects found in the original project and how they were fixed

Found by reading all code, prefabs, the scene and the project settings. "Verified by" tells how the fix is protected.

## Gameplay and data

| ID | Where | Symptom | Root cause | Fix | Verified by |
|---|---|---|---|---|---|
| BUG-01 | `Player.prefab` | the limp / crawl animation reacted asymmetrically (a shin hit on the left leg made the blob hop, the same hit on the right did not) | `_LeftLegLower` had `bodyState: LeftLegUpper` (duplicate), so `FindBodyPart(LeftLegUpper)` returned the *lower* leg | `bodyState: 1` | validator rule `PLAYER001`; `Player.ValidateSetup` logs an error for duplicate / missing states in editor and development builds |
| BUG-02 | `Player.ValidateState` | after the last part was lost `OnPlayerDead` fired every 0.2 s forever; each call started another scene reload | endless coroutine without `yield break`; public `Action` field | event driven `Player.Died`, raised exactly once | state machine rejects a second `Lost` |
| BUG-03 | `PController` → `GameManager` | crossing the finish line instantly reloaded the scene; there was no win, no score, no progression | `RestartGame(0)` in `OnTriggerEnter` | `FinishReached` → `GameManager`: score, stars, save, `Won` state, result popup, next level | `ScoreCalculatorTests`, `SaveDataTests`, `GameStateMachineTests` |
| BUG-04 | `PController` | judder at 60 / 90 / 120 Hz | movement in `FixedUpdate` (50 Hz) | movement in `Update` with smoothed speed | – |
| BUG-05 | `PController` | animator parameters rewritten every 0.1 s for no reason; coroutine running forever | polling coroutine | updates only when `Player.PartsChanged` fires | – |
| BUG-06 | `PController` | yaw snapped to ±45° for any non-zero input and flipped sign when the stick was pulled down | `atan2(x, y) * 90 / π` (half-degrees, no magnitude) | yaw proportional to the sideways velocity, max 28° | – |
| BUG-07 | `PController` | the stored steering input was thrown away whenever the blob touched the lane edge (a held finger did nothing until it moved again) | `ValidateLocation` zeroed the stored direction | clamp the position only, keep the input | – |
| BUG-08 | `BodyPart`, `LootContainer` | jelly wobble biased towards −x/−y/−z | `Random.Range(-1, 1)` with *ints* returns only −1 or 0 | float ranges (`Random.insideUnitSphere`, `Range(-1f, 1f)`) | – |
| BUG-09 | `BodyPart` | a part that was cut off a second time stopped wobbling; `Invoke(StopAllAnimation, 5)` could fire after the part was regrown / destroyed | `_shouldAnimate` never reset; fire-and-forget `Invoke` | per-part DOTween sequence with id, killed on regrow / destroy | – |
| BUG-10 | `LootContainer` | idle tweens of collected loot were never stopped | `_tweeners` list only ever contained `null` (the tween was assigned to a local after being added) | tweens tagged with an id and killed by id | – |
| BUG-11 | `BodyPart.relatedBodyPart` | property returned `requiredBodyParts` | copy-paste error | exposed as `RelatedBodyParts` / `RequiredBodyParts` | – |
| BUG-12 | `LootContainer` | shared material mutated: loot items sharing a material shrank together; every editor play session dirtied the `.mat` asset; `OnDestroy` threw if `Start` never ran | `sharedMaterial` + a "reset to 0.1" hack | per-instance material, `collected` guard, no reset hack | – |
| BUG-13 | `LootContainer` | the pickup could trigger twice | no guard | `_collected` flag, collider disabled | – |
| BUG-14 | `MergeController` | regrown parts still counted as broken (limp / crawl animation, death check) for 1.5 s; runtime lists were serialized into the prefab; the scale animation had no visible effect | list kept in a serialized field, `SetFloat("_…Scale")` on a shader that does not read it | parts count as attached immediately but are harmless until they arrived; growth is animated on the part transform (overshoot) | – |
| BUG-15 | `ShaderSetting` | `_TorsoLowerScale` returned 0 | the switch only knew the legacy `_TorsoMidScale` | both names handled | – |
| BUG-16 | `ShaderSettingApplier` | wrote default colours into the *shared material asset* in `OnDestroy` (editor-undo hack); the asset kept changing | single shared material | initialises the per-instance material in `Awake`, no `OnDestroy` hack | – |
| BUG-17 | `TransformProvider` | blob lagged the skeleton by one frame (matrices uploaded before the Animator ran); string-keyed `SetMatrix` + full `Matrix4x4.Inverse` per part per frame; edits to the shared material | `Update`, `sharedMaterial` | `LateUpdate`, cached ids, `worldToLocalMatrix`, per-instance material | – |
| BUG-18 | `LootT1.prefab` | a loot spawned from the prefab rendered nothing (only the four scene instances worked) | `TransformProvider.targetRenderer` was `null`; the scene overrode it per instance | prefab reference set; `TransformProvider` also falls back to the renderer on the same object | validator `REF*` rules |
| BUG-19 | `Joystick.AxisOptions` | `StackOverflowException` as soon as the property was read | getter returned itself | returns the backing field | – |
| BUG-20 | `Joystick` (static events) | with domain reload disabled, handlers of destroyed objects survived between play sessions | statics never reset | reset in `SubsystemRegistration` | – |
| BUG-21 | `EmptySphere.prefab` | physics cost: a static, non-convex mesh collider was moved every frame for each cut | collider nobody uses | removed at runtime (`JellyContainer.Awake`); colliders of cut parts are disabled | – |
| BUG-22 | `GameManager` | tweens survived scene reloads; two `RestartGame` overloads; `targetFrameRate = 120`; no pause / win / lose / menu | prototype code | rewritten (see ARCHITECTURE.md) | `GameStateMachineTests` |
| BUG-23 | `Singleton<T>` | `FindObjectOfType` + lock, no duplicate handling | prototype code | removed; explicit `GameManager.Instance` with duplicate guard and static reset | – |

## Rendering, UI and project configuration

| ID | Where | Problem | Fix |
|---|---|---|---|
| CFG-01 | `SmallGroupT1.shader` (loot), `Mobile/BlobCharacterShader.shader` | `#include "Assets\uRaymarching\…"` with backslashes – fails to compile on macOS / Linux editors | forward slashes |
| CFG-02 | Canvas | `ConstantPixelSize` 800 × 600: joystick radius and sensitivity were tiny / inconsistent on high-DPI phones | `ScaleWithScreenSize` 1080 × 1920, match 0.5 |
| CFG-03 | Player settings | auto-rotation enabled although the game and camera are designed for portrait | portrait only |
| CFG-04 | Player settings | `DefaultCompany`, version 0.1, uRaymarching sample project name / organisation / cloud project id | `KetanDutt`, `com.KetanDutt.BlobRunner`, 1.0.0, ids cleared |
| CFG-05 | Android | ARMv7 + x86 with the Mono backend (no ARM64 → rejected by Google Play); no frame pacing | IL2CPP, ARMv7 + ARM64, Swappy frame pacing |
| CFG-06 | Release builds | Graphy statistics overlay active | removed in non-development builds |
| CFG-07 | Rendering | camera HDR on, realtime shadows on a light with no casters, editor ran at quality *Ultra* while devices use *Very Low* | HDR off, shadows off, editor quality aligned with the mobile default, anisotropic filtering per texture on the mobile levels |
| CFG-08 | Packages | URP, ShaderGraph, Burst, Mathematics, Post Processing, Timeline, Collab, 2D sprite packages installed but unused (the project is Built-in RP, the ray marching shaders are legacy forward); `UNITY_POST_PROCESSING_STACK_V2` define; URP asset files in `Assets/` | removed (manifest, lock file, defines, assets, `m_SRPDefaultSettings`) |
| CFG-09 | `ShaderSettingStorer.asset` | six stale `*Mid*` fields that no longer exist in the class | removed |
| CFG-10 | Repository | `.DS_Store`, 23 orphaned `.meta` files (folders that were never committed, plus a Graphy sample package that is not in the repository), tracked `UserSettings/`, stale `ShaderGenerator.asset`, empty `Interfaces.cs` / `Utilities.cs`, example scenes and PDFs of third party packs, unused tag `ASpeed` | deleted / untracked, `.gitignore` extended, `tools/validate_project.py` + CI keep it that way |

## Known quirks that were *not* changed

* `Standard/BlobCharacterShader.shader` ignores the `*Scale` properties and contains an extra torso term in the albedo blend (see [PERFORMANCE.md](PERFORMANCE.md#shader-notes-for-whoever-edits-them)).
* The `FinishArea` trigger is invisible by design; `LevelBuilder` adds a visible gate next to it.
* `Assets/_Main/Shader/Mobile/BlobCharacterShader.shader` is not referenced by any material (kept as the basis for a low-end tier).
