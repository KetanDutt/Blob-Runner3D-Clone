using UnityEngine;

namespace BlobRunner.Effects
{
    /// <summary>
    /// Creates and plays the particle effects (all built in code, one world space system per effect type).
    ///
    /// Every system is "playing" with a zero emission rate and is fired with <see cref="ParticleSystem.Emit(int)"/>,
    /// so a burst costs one call and no instantiation. Particles use the always-included <c>Sprites/Default</c> shader.
    /// </summary>
    public sealed class VfxDirector : MonoBehaviour
    {
        private ParticleSystem _splat;
        private ParticleSystem _sparkle;
        private ParticleSystem _death;
        private ParticleSystem _confetti;
        private ParticleSystem _dust;
        private Material _softMaterial;
        private Material _starMaterial;
        private Material _squareMaterial;

        private void Awake()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[VFX] Shader 'Sprites/Default' not found; particles are disabled.");
                enabled = false;
                return;
            }

            _softMaterial = CreateMaterial(shader, ProceduralTextures.SoftCircle);
            _starMaterial = CreateMaterial(shader, ProceduralTextures.Star);
            _squareMaterial = CreateMaterial(shader, ProceduralTextures.Square);

            BuildSplat();
            BuildSparkle();
            BuildDeath();
            BuildConfetti();
            BuildDust();
        }

        private void OnDestroy()
        {
            Destroy(_softMaterial);
            Destroy(_starMaterial);
            Destroy(_squareMaterial);
        }

        // ---- Public API -----------------------------------------------------------------------------

        /// <summary>Jelly droplets when parts are cut off.</summary>
        public void Splat(Vector3 position, Color color, int partsLost)
        {
            if (_splat == null)
                return;

            Fire(_splat, position, new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.4f)), 10 + partsLost * 5);
        }

        /// <summary>Stars when loot is collected.</summary>
        public void Sparkle(Vector3 position, Color color)
        {
            if (_sparkle == null)
                return;

            Fire(_sparkle, position, new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.7f)), 18);
        }

        /// <summary>Big burst when the last body part is lost.</summary>
        public void DeathBurst(Vector3 position, Color color)
        {
            if (_death == null)
                return;

            Fire(_death, position, new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.3f)), 56);
        }

        /// <summary>Colourful confetti falling from above <paramref name="center"/> (finish line).</summary>
        public void Confetti(Vector3 center)
        {
            if (_confetti == null)
                return;

            _confetti.transform.position = center + new Vector3(0f, 4.2f, 2.5f);
            _confetti.Emit(110);
        }

        /// <summary>Attaches the running dust to the player's feet.</summary>
        public void AttachDust(Transform feet)
        {
            if (_dust == null || feet == null)
                return;

            _dust.transform.SetParent(feet, false);
            _dust.transform.localPosition = new Vector3(0f, 0.2f, -0.1f); // the floor surface is at y = 0.11
        }

        public void SetDust(bool active)
        {
            if (_dust == null)
                return;

            var emission = _dust.emission;
            emission.rateOverTime = active ? 14f : 0f;
        }

        // ---- Construction ---------------------------------------------------------------------------

        private static Material CreateMaterial(Shader shader, Texture texture)
        {
            var material = new Material(shader) { mainTexture = texture, hideFlags = HideFlags.DontSave };
            return material;
        }

        private static void Fire(ParticleSystem system, Vector3 position, ParticleSystem.MinMaxGradient color, int count)
        {
            system.transform.position = position;
            var main = system.main;
            main.startColor = color;
            system.Emit(count);
        }

        private ParticleSystem Create(string objectName, Material material, int maxParticles)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // a fresh system starts emitting immediately

            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        private static void FadeAndShrink(ParticleSystem system, float shrinkTo)
        {
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, shrinkTo));
        }

        private void BuildSplat()
        {
            _splat = Create("Splat", _softMaterial, 160);
            var main = _splat.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
            main.gravityModifier = 2.4f;

            var shape = _splat.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 58f;
            shape.radius = 0.12f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // emit upwards

            FadeAndShrink(_splat, 0.2f);
            _splat.Play();
        }

        private void BuildSparkle()
        {
            _sparkle = Create("Sparkle", _starMaterial, 96);
            var main = _sparkle.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.30f);
            main.gravityModifier = -0.25f;

            var shape = _sparkle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;

            var rotation = _sparkle.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            FadeAndShrink(_sparkle, 0.1f);
            _sparkle.Play();
        }

        private void BuildDeath()
        {
            _death = Create("Death", _softMaterial, 120);
            var main = _death.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
            main.gravityModifier = 1.6f;

            var shape = _death.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            FadeAndShrink(_death, 0.15f);
            _death.Play();
        }

        private void BuildConfetti()
        {
            _confetti = Create("Confetti", _squareMaterial, 200);
            var main = _confetti.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.gravityModifier = 0.9f;

            var palette = new Gradient();
            palette.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.35f, 0.35f), 0f),
                    new GradientColorKey(new Color(1f, 0.82f, 0.25f), 0.2f),
                    new GradientColorKey(new Color(0.35f, 0.95f, 0.5f), 0.4f),
                    new GradientColorKey(new Color(0.3f, 0.75f, 1f), 0.6f),
                    new GradientColorKey(new Color(0.75f, 0.45f, 1f), 0.8f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.85f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(palette) { mode = ParticleSystemGradientMode.RandomColor };

            var shape = _confetti.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6f, 0.4f, 3.5f);

            var rotation = _confetti.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            var noise = _confetti.noise;
            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.6f;

            var colorOverLifetime = _confetti.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            _confetti.Play();
        }

        private void BuildDust()
        {
            _dust = Create("RunDust", _softMaterial, 48);
            var main = _dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.gravityModifier = -0.05f;

            var shape = _dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.45f, 0.02f, 0.3f);

            var colorOverLifetime = _dust.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            var sizeOverLifetime = _dust.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

            _dust.Play();
        }
    }
}
