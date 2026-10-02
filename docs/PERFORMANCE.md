# Performance

> **Status of the numbers:** no Unity Editor or device was available while this work was done, so nothing below is a measurement. The section "Cost model" explains *why* the changes help; verify with the Profiler (see "Measuring") on a real low-end phone.

## Cost model

The blob and every loot item are rendered by **ray marching inside a proxy cube**. Per covered pixel the fragment shader loops up to `_Loop` (25) steps and evaluates one signed distance function per body part (11 for the player, 5 per loot) with a smooth minimum. So:

* the cost is dominated by **GPU fill rate** (pixels covered × steps × parts), not by the CPU;
* the player's `RenderArea` is a 4 × 3 × 10 m box – it covers a large part of the screen;
* every extra pass over those pixels (shadow caster pass, HDR targets, MSAA, higher resolution) multiplies the cost.

## What was changed

| Area | Change | Why it helps |
|---|---|---|
| Frame rate | `targetFrameRate` 120 → **60**, v-sync off on mobile so the cap is honoured, Swappy frame pacing on | the old setting ran a ray marcher at up to 120 fps on 120 Hz phones: heat, battery and throttling |
| Resolution | **Adaptive resolution** (`AdaptiveQualityGovernor`): two slow 2-second windows (< 88 % of 60 fps) lower the render scale by 10 % down to 60 %; it only recovers once, after ~16 s of perfect frames, and never after two downgrades | fill rate scales with pixel count; unit tested against flip-flopping |
| Render settings | Camera **HDR off**; directional light **shadows off** (nothing casts shadows: player, loot, bars and floor are all set not to) | HDR targets and the shadow / screen-space-shadow passes cost bandwidth for no visible benefit |
| Floor | the lit floor cube is replaced by an unlit patterned strip (`Sprites/Default`) | one cheap flat quad instead of forward-lit PBR over the whole visible track |
| Movement | player moves in `Update` with smoothed speed instead of `FixedUpdate` at 50 Hz | no judder on 60+ Hz screens |
| Polling | removed the `WaitForSeconds(0.1)` animation polling and the endless `ValidateState` coroutine; everything is event driven | no allocations, no wasted wake-ups |
| Shader plumbing | `TransformProvider`: cached property ids, `worldToLocalMatrix` instead of `Matrix4x4.Inverse(TRS)`, runs in `LateUpdate` | no per-frame string hashing, no 1-frame lag |
| Physics | removed the unused static non-convex `MeshCollider` that was moved every frame on each cut-off container; colliders of cut parts are disabled | moving static colliders forces PhysX to rebuild acceleration structures |
| Materials | per-instance materials (created once, destroyed with the owner) | removes cross-talk between loot items and editor asset churn |
| GC | tween recycling on; HUD only touches text / anchors when a value changed; effects use `Emit` on existing systems; audio is synthesised once and cached across restarts | fewer GC spikes (the common cause of hitches on mobile) |
| Build | Graphy overlay removed from release builds; unused packages (URP + ShaderGraph + Burst, Post Processing, Timeline, 2D sprite packages, Collab) removed | smaller import / compile time and build; no dead code paths |

## Budgets worth keeping an eye on

* Every loot item is one more ray marched draw call (5 primitives each). Loot is at least 4 m apart (typically 10 – 25 m), so only a few are ever on screen; keep it that way when adding content.
* ≤ ~30 live tweens in gameplay; DOTween is configured for 300 tweens / 60 sequences without reallocation.
* Level generation takes ~15 ms on a desktop-class CPU (24 candidates × a few hundred simulated runs), once per scene load, hidden behind the black curtain.

## Measuring

1. Build a *Development Build* with *Autoconnect Profiler* (Graphy is kept in development builds and shows FPS / frame time on screen).
2. GPU-bound? In the Profiler look for `Gfx.WaitForPresentOnGfxThread` dominating the render thread; confirm with Android GPU Inspector / Xcode GPU capture / RenderDoc: fragment time of the `RenderArea` draw call.
3. Compare `Screen.SetResolution` scales manually (`PerformanceDirector.ResolutionScale`) to find the lowest acceptable scale per device class.

## Next steps (need on-device validation, therefore not done blindly)

* Expose `_Loop` (march steps) and `_MinDistance` as quality tiers (e.g. 25 → 16 on low-end) – a one-line material change, but needs a visual check for holes.
* Shrink / re-centre the `RenderArea` box when parts are cut off (fewer covered pixels).
* GPU-instance the loot (needs per-instance data in `UNITY_INSTANCING_BUFFER`; the shader currently reads plain uniforms).
* Wire up `Assets/_Main/Shader/Mobile/BlobCharacterShader.shader` (single colour, cheaper) as the lowest tier.
* Vulkan + Dynamic Resolution (`ScalableBufferManager`) instead of `Screen.SetResolution`, where supported.

## Shader notes (for whoever edits them)

* `Standard/BlobCharacterShader.shader` radii are compile-time constants; the `_…Scale` material properties are not read. The albedo post effect also contains an extra `_colorBlendResult3.z * _TorsoColor` term and an unused `.w` – harmless, but a candidate for clean-up when the shader is regenerated.
* Both ray marching shaders used Windows style backslashes in an `#include`, which fails on macOS / Linux editors; now fixed (forward slashes). If they are regenerated with *uShaderTemplate* on Windows, the backslashes may come back.
