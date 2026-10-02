using System;
using UnityEngine;

namespace BlobRunner.Effects
{
    /// <summary>
    /// Small textures generated in code (soft circle, star, checker, stripes, ...). The project ships no texture
    /// assets for particles / UI, which keeps the repository tiny and the look easy to tweak.
    /// </summary>
    public static class ProceduralTextures
    {
        private static Texture2D _softCircle;
        private static Texture2D _star;
        private static Texture2D _square;

        // Static caches survive between play sessions when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _softCircle = null;
            _star = null;
            _square = null;
        }

        /// <summary>White disc with a soft edge (alpha only).</summary>
        public static Texture2D SoftCircle
        {
            get
            {
                if (_softCircle == null)
                {
                    _softCircle = Create("SoftCircle", 64, 64, (u, v) =>
                    {
                        float r = Radius(u, v);
                        float a = Mathf.Clamp01(1f - r);
                        return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
                    });
                }

                return _softCircle;
            }
        }

        /// <summary>White five pointed star with a slightly soft edge.</summary>
        public static Texture2D Star
        {
            get
            {
                if (_star == null)
                {
                    _star = Create("Star", 64, 64, (u, v) =>
                    {
                        float x = u * 2f - 1f;
                        float y = v * 2f - 1f;
                        float distance = Mathf.Sqrt(x * x + y * y);
                        float angle = Mathf.Atan2(y, x);
                        float edge = 0.55f + 0.38f * Mathf.Cos(5f * (angle - Mathf.PI * 0.5f));
                        float a = Mathf.Clamp01((edge - distance) / 0.08f);
                        return new Color(1f, 1f, 1f, a);
                    });
                }

                return _star;
            }
        }

        /// <summary>Plain white square (confetti).</summary>
        public static Texture2D Square
        {
            get
            {
                if (_square == null)
                    _square = Create("Square", 4, 4, (u, v) => Color.white);

                return _square;
            }
        }

        /// <summary>
        /// Floor pattern: 2 x 2 cells of two tones across the width of the track plus a coloured line on both edges.
        /// The texture is meant to be stretched across the whole width (u) and repeated along the track (v).
        /// </summary>
        public static Texture2D FloorTexture(Color a, Color b, Color edge)
        {
            var texture = Create("Floor", 64, 64, (u, v) =>
            {
                if (u < 0.03f || u > 0.97f)
                    return edge;

                bool odd = ((int)(u * 2f) + (int)(v * 2f)) % 2 == 0;
                return odd ? a : b;
            }, true, TextureWrapMode.Repeat);
            texture.anisoLevel = 4;
            return texture;
        }

        /// <summary>Plain checkerboard with the given number of cells (finish line).</summary>
        public static Texture2D Checkerboard(Color a, Color b, int cellsU, int cellsV)
        {
            var texture = Create("Checkerboard", 128, 64, (u, v) =>
            {
                bool odd = ((int)(u * cellsU) + (int)(v * cellsV)) % 2 == 0;
                return odd ? a : b;
            }, true, TextureWrapMode.Clamp);
            texture.anisoLevel = 4;
            return texture;
        }

        /// <summary>Diagonal two colour stripes (hazard bars). Repeats; not cached because the colours vary per theme.</summary>
        public static Texture2D Stripes(Color a, Color b)
        {
            var texture = Create("Stripes", 64, 64, (u, v) =>
            {
                float d = (u + v) * 4f; // four stripe pairs across the diagonal
                return (d - Mathf.Floor(d)) < 0.5f ? a : b;
            }, true, TextureWrapMode.Repeat);
            texture.anisoLevel = 4;
            return texture;
        }

        /// <summary>Builds a texture by evaluating <paramref name="pixel"/> for every texel (u, v in 0..1).</summary>
        public static Texture2D Create(string name, int width, int height, Func<float, float, Color> pixel,
            bool mipmaps = false, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipmaps)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    pixels[y * width + x] = pixel(u, v);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(mipmaps, false);
            return texture;
        }

        private static float Radius(float u, float v)
        {
            float x = u * 2f - 1f;
            float y = v * 2f - 1f;
            return Mathf.Sqrt(x * x + y * y);
        }
    }
}
