# Audio, effects, camera, haptics and UI

All of this is **generated in code**: the repository ships no audio, texture or UI prefab binaries. That keeps it small, makes every value reviewable in a diff, and lets the logic be unit tested. The mapping from gameplay events to feedback is centralised in [`GameFeedback`](../Assets/_Main/Scripts/Effects/GameFeedback.cs).

## Audio

`ProceduralSynth` (pure C#, `BlobRunner.Core`) renders mono 22.05 kHz samples with additive / FM / filtered-noise recipes. `AudioDirector` builds one clip per frame at start-up (no hitch), keeps 8 voices for sound effects and one music source.

| Effect | Played when | Recipe |
|---|---|---|
| `Click` | any UI button is pressed | two short sine ticks (1.8 / 2.7 kHz) |
| `Start` | the run begins | rising band-limited noise + sine sweep + blip |
| `Cut` | body parts are cut off | falling sine (520 → 90 Hz) + dull noise burst + 68 Hz thud, soft clipped |
| `Regrow` | loot regrew parts | three rising "bubble" sweeps |
| `Collect` | loot picked up | E5 – G5 – B5 – E6 bell arpeggio |
| `Star` | each star on the result popup | glass bell with inharmonic partials, pitch rises per star |
| `Win` | finish line | C-major run + chord + sparkles |
| `Lose` | last part lost | falling "wah" slide G4 → G3 + thud |
| `Whoosh`, `Pop` | available for new features | filtered noise swell / short sine drop |
| Music | menu and run | 8 bars C – Am – F – G at 112 bpm: bass, plucked arpeggio, pad, kick / snare / hat; wraps seamlessly |

Tests check that every effect is finite, never clips (peak ≤ 1), is audible, starts / ends silently (no clicks), is deterministic, that the pitches are right (Goertzel analysis of the first note of `Collect` / `Star`) and that the music loop has the exact expected length and no click at the loop point.

**Using recordings instead:** put an `AudioClip` named like the effect in lower case (`click`, `cut`, `collect`, `win`, …) in any `Resources/Audio` folder – it replaces the synthesized one; `Resources/Audio/music` replaces the music loop.

Settings (sound, music, vibration) are persisted and exposed in the main menu and the pause menu. The music is ducked while paused and on the result screens.

## Particle effects

`VfxDirector` creates one world-space `ParticleSystem` per effect and fires bursts with `Emit` (no instantiation per hit). Particles use the always-included `Sprites/Default` shader with procedurally generated textures (`ProceduralTextures`: soft disc, star, square).

| Effect | Trigger | Notes |
|---|---|---|
| Jelly splat | cut | tinted with the colour of the cut part, count scales with the number of lost parts, gravity, fade + shrink |
| Sparkles | loot | star particles in the loot colour |
| Death burst | last part lost | large splat in the blob colour |
| Confetti | finish line | 110 multi-colour squares with noise and spin |
| Run dust | while running | continuous soft puffs at the feet |
| Contact shadow | always | `BlobShadow`: soft disc under the blob that shrinks as parts are lost (the ray marched blob casts no real shadow) |

## Camera and game feel

`CameraShakeExtension` is a Cinemachine extension (adds trauma-based shake and an FOV offset *inside* the camera pipeline – writing to the camera transform would be overwritten by the `CinemachineBrain`). `CameraDirector` drives it:

* cut: shake scales with the number of lost parts, FOV punch, red damage vignette, floating "OUCH!" / "-N" text, a 40 – 110 ms hit-stop;
* loot: FOV punch, "+N" text in the loot colour, sparkles;
* run: slightly wider FOV while running (speed feel); finish: wider and calmer; defeat: slow zoom-in.

Additional polish: the floor has a checker pattern with coloured edge lines (a plain floor gives no feeling of speed), hazard-striped bars, a visible chequered finish gate, six colour themes, the menu pose "breathes", body parts overshoot when they regrow.

## Haptics

`HapticsService` – Android: one-shot `VibrationEffect` pulses (falls back to `vibrate(ms)` below API 26 and to `Handheld.Vibrate()`, which also makes Unity add the `VIBRATE` permission); iOS: `Handheld.Vibrate()` for the heavy moments only (there is no public short-pulse API in Unity); editor / desktop: no-op. Can be switched off in the menus.

## User interface

Built by `UIManager` / `UIFactory` as a `ScreenSpaceOverlay` canvas (sorting order 20, above the steering canvas of the scene) with a `CanvasScaler` at **1080 × 1920, match 0.5**, plus a `SafeAreaFitter` per screen (notches / rounded corners). Sprites (rounded 9-slice, circle, star, vignette) are generated at runtime; the font is Unity's built-in Arial. Strings are collected in `UIStrings`.

| Screen | Shown in state | Content |
|---|---|---|
| Menu | Menu | title, level, best score, **PLAY**, "tap anywhere", sound / music / vibration switches |
| HUD | Playing | level, progress bar with runner marker and finish star, jelly meter (colour from green to red, punches when it drops), pause button, steering hint on the very first run |
| Pause | Paused | resume, restart, menu, switches |
| Result (win) | Won | stars revealed one by one with sound, count-up score, "NEW BEST!", next level / replay |
| Result (lose) | Lost | percentage of the track reached, try again / menu |
| Overlays | always | black curtain (fade in on load, fade out before a reload), damage vignette, floating texts |

All popups animate with DOTween in unscaled time (they work while the game is paused); every button gets press squash, a click sound and a haptic tick.
