# Build and release

## Requirements

* **Unity 2021.3.5f1** (`ProjectSettings/ProjectVersion.txt`). Newer 2021.3 LTS patch releases are fine.
* Android: *Android Build Support* with **OpenJDK, SDK & NDK and the IL2CPP module** (the project now builds with IL2CPP, see below).
* iOS: macOS with Xcode (iOS 11+).
* The only scene in the build is `Assets/_Main/Scenes/Scene_001.unity`.

## What the player settings say (and why)

| Setting | Value | Notes |
|---|---|---|
| Orientation | Portrait only | the camera rig, HUD and joystick are designed for portrait |
| Product / company / version | Blob Runner / KetanDutt / 1.0.0 | **change the company name and package id to your own** before publishing (`PlayerSettings → Other Settings → Package Name`, currently `com.KetanDutt.BlobRunner`) |
| Android scripting backend / architectures | **IL2CPP**, ARMv7 + ARM64 | Google Play requires 64 bit; ARM64 is only available with IL2CPP |
| Android min SDK | 22 (Unity 2021.3 minimum) | target API level is left on "highest installed": make sure it satisfies the current Google Play requirement |
| Frame pacing | Swappy ("Optimized Frame Pacing") on | smoother 60 fps on Android |
| Graphics APIs (Android) | OpenGL ES 3 first, Vulkan second (unchanged) | |
| Colour space | Linear (unchanged) | |
| Stripping | engine code stripping on (unchanged) | |
| Default quality | *Very Low* on every platform (unchanged) – no shadows, no MSAA, v-sync off | the game needs none of that; resolution is managed by `PerformanceDirector` |
| Splash | Unity splash screen (required by the Personal licence) | |

## Building

1. `File → Build Settings` → platform *Android* / *iOS* → *Switch Platform*.
2. Android: create a keystore in *Player Settings → Publishing Settings* (never commit it – `*.keystore` should stay outside the repository), increase **Bundle Version Code** for every upload, build *Android App Bundle* for Google Play.
3. For a profiling build tick *Development Build* and *Autoconnect Profiler*: the on-screen Graphy overlay is only present in development builds.

## Versioning

`bundleVersion` (semantic version shown to users) and `AndroidBundleVersionCode` / iOS build number (monotonic integers). The save format has its own version (`SaveData.version`).

## Continuous integration

* `.github/workflows/validate.yml` runs `tools/validate_project.py --strict` on every push / pull request (meta files, GUID and fileID references, script ↔ class names, build settings, Player prefab rules). No Unity licence needed.
* `.github/workflows/unity-tests.yml` runs the EditMode tests in the Unity Test Runner via GameCI. It is **manual** (`workflow_dispatch`) and needs the secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (see <https://game.ci/docs/github/activation>).

## Release checklist

- [ ] Open the project once with Unity 2021.3 and let it re-import (packages were pruned; Unity regenerates `Packages/packages-lock.json` entries if needed) – commit any `.meta` changes Unity makes.
- [ ] `Window → General → Test Runner → EditMode → Run All` is green.
- [ ] Play `Scene_001` in the editor with a portrait Game view (e.g. 1080 × 1920): menu → play → cut → loot → finish → next level → die → retry; pause / resume; toggle the three switches and restart the app (settings persist).
- [ ] Play through at least levels 1 – 12 on a **low-end Android phone**; check frame rate, heat, the adaptive resolution (`PerformanceDirector.ResolutionScale`), audio loudness, haptics.
- [ ] Check notch / safe-area devices and a tablet aspect ratio (UI is anchored to the safe area and scales with 1080 × 1920, match 0.5).
- [ ] Replace the default app icon and add a launch / splash image (none ships with the project).
- [ ] Set your own package id, company name, keystore; fill the store listing (privacy policy: the game collects no data and has no analytics / ads).
- [ ] Verify third party licences ([THIRD_PARTY.md](THIRD_PARTY.md)) for your distribution channel.
