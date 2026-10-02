# Suggested improvements and known limitations

## Do first (first run in the Unity Editor / on a device)

1. **Eyeball the new visuals** – floor pattern orientation, hazard stripes on the bars, finish gate, particle look, UI layout on several aspect ratios. All are code-generated and easy to tweak (`LevelBuilder`, `VfxDirector`, `UIFactory`, `LevelThemes`).
2. **Tune the feel constants** – shake amplitude, hit-stop, FOV punches (`GameFeedback`, `CameraShakeExtension`), steering responsiveness (`PController`: `lateralAcceleration`, `maxYaw`), part regrow timing (`BodyPart`, `MergeController`).
3. **Measure on a low-end phone** and decide the quality tiers (see [PERFORMANCE.md](PERFORMANCE.md)).
4. **App icon and splash** – none ships with the project.

## Gameplay ideas

* Power-ups: shield (ignore one hit), magnet, speed boost (the `Whoosh` sound effect is already available), "regrow only the head" mini loot.
* New obstacle kinds: swinging / rotating bars, gates that open and close, narrow tunnels the blob must crouch through (use the crawl animation on purpose).
* Boss / special levels every 10th level with a hand authored layout; collectible skins (the six colour properties of `ShaderSetting` already allow it).
* Daily challenge using a date-based seed (`LevelGenerator` is deterministic by design).
* Combo / streak score for consecutive dodges, ghost of the best run.

## Technical debt and next engineering steps

* **Input System**: the joystick pack and the project use the legacy Input Manager. Migrating to the new Input System would allow gamepads and proper multi-touch handling.
* **TextMesh Pro** + a custom font (and localisation through `UIStrings`) instead of Arial.
* **Addressables** for the content once the game grows beyond a single scene.
* **Quality tiers for the ray marcher**: `_Loop` / `_MinDistance`, the Mobile shader, shrinking the render volume when parts are lost, GPU instancing for loot.
* **Authored audio and art** to replace the procedural sounds and textures (drop-in: see [AUDIO_VFX_UI.md](AUDIO_VFX_UI.md)).
* **Accessibility**: reduce-motion option (shake, flashes), colour-blind friendly palettes, larger text option.
* **PlayMode tests** for the scene flow (menu → run → win / lose → reload) once a CI Unity licence is available; the EditMode suite already covers the logic.
* **Analytics / ads / IAP** are intentionally absent (no data is collected). Add them behind interfaces in `Services` when needed.
* **Cinemachine 3 / Unity 6** upgrade path: `CameraShakeExtension` is the only class that depends on Cinemachine 2 internals.

## Known limitations

* The new systems are verified by compilation and unit tests, not by play testing (see [TESTING.md](TESTING.md)).
* The difficulty model assumes an upright blob and ignores the time needed to steer between rows; it is a balancing aid.
* Haptics on iOS are limited to `Handheld.Vibrate()` (a long buzz) and therefore only used for big moments.
* `Screen.SetResolution` based dynamic resolution may cause a tiny hitch when the scale changes; it happens at most a handful of times per session.
* The Mobile ray marching shader is not wired up; the `Standard` shader ignores the per-part `*Scale` properties.
* Portrait only; landscape would need a different camera offset and HUD layout.
