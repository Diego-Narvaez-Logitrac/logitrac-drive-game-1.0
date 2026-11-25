using System.Collections.Generic;
using UnityEngine;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>Prototype units (y down) → Unity world (y up). worldPos = (x*0.01, -y*0.01); rotation z = -heading.</summary>
    public static class TurboSpace
    {
        public const float Scale = 0.01f;

        public static Vector3 ToWorld(float x, float y) => new Vector3(x * Scale, -y * Scale, 0f);
        public static Vector3 ToWorldVector(float vx, float vy) => new Vector3(vx * Scale, -vy * Scale, 0f);
        public static Quaternion Rotation(float heading) => Quaternion.Euler(0f, 0f, -heading * Mathf.Rad2Deg);

        public static float WrapAngle(float a)
        {
            while (a > Mathf.PI) a -= Mathf.PI * 2f;
            while (a < -Mathf.PI) a += Mathf.PI * 2f;
            return a;
        }
    }

    /// <summary>Runtime-generated sprites, textures and materials for the race views.</summary>
    public static class TurboGfx
    {
        static Sprite _square, _flame;
        static Texture2D _circle;
        static readonly Dictionary<long, Texture2D> Dashes = new Dictionary<long, Texture2D>();
        static readonly Dictionary<long, Sprite> Rings = new Dictionary<long, Sprite>();

        /// <summary>1×1 world-unit white square (same as Unity's 2D "Square" sprite, but available in player builds).</summary>
        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    var tex = Texture2D.whiteTexture;
                    _square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
                    _square.name = "Square";
                }
                return _square;
            }
        }

        public static Material UnlitMaterial(Material assigned)
        {
            if (assigned != null) return assigned;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return new Material(shader) { name = "TurboUnlit (runtime)" };
        }

        /// <summary>Repeating dash texture: <paramref name="on"/> opaque texels then <paramref name="off"/> clear texels.</summary>
        public static Texture2D Dash(int on, int off)
        {
            long key = ((long)on << 32) | (uint)off;
            if (Dashes.TryGetValue(key, out var cached) && cached != null) return cached;
            var tex = new Texture2D(on + off, 1, TextureFormat.RGBA32, false)
            {
                name = $"Dash{on}_{off}", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat,
            };
            for (int i = 0; i < on + off; i++) tex.SetPixel(i, 0, i < on ? Color.white : Color.clear);
            tex.Apply(false, true);
            Dashes[key] = tex;
            return tex;
        }

        /// <summary>Solid white circle with a soft edge (smoke puffs).</summary>
        public static Texture2D Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 32;
                _circle = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Circle", wrapMode = TextureWrapMode.Clamp };
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    _circle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
                }
                _circle.Apply(false, true);
                return _circle;
            }
        }

        /// <summary>
        /// Boost flame: a triangle 1×1 world unit, pivot on its right edge (the car's rear). Colours follow the
        /// prototype gradient: blue-white at the base, blue, then transparent at the tip.
        /// </summary>
        public static Sprite Flame
        {
            get
            {
                if (_flame != null) return _flame;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Flame", wrapMode = TextureWrapMode.Clamp };
                var c0 = new Color(220 / 255f, 240 / 255f, 1f, 0.95f);
                var c1 = new Color(36 / 255f, 123 / 255f, 254 / 255f, 0.8f);
                var c2 = new Color(36 / 255f, 123 / 255f, 254 / 255f, 0f);
                for (int x = 0; x < size; x++)
                {
                    float u = 1f - (x + 0.5f) / size; // 0 at the base (right), 1 at the tip (left)
                    var col = u < 0.4f ? Color.Lerp(c0, c1, u / 0.4f) : Color.Lerp(c1, c2, (u - 0.4f) / 0.6f);
                    float half = (1f - u) * 0.5f * size;
                    for (int y = 0; y < size; y++)
                    {
                        float dy = Mathf.Abs(y + 0.5f - size / 2f);
                        var px = col;
                        px.a *= Mathf.Clamp01(half - dy + 0.5f);
                        tex.SetPixel(x, y, px);
                    }
                }
                tex.Apply(false, true);
                _flame = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(1f, 0.5f), size);
                _flame.name = "Flame";
                return _flame;
            }
        }

        /// <summary>Rounded-rect outline for 9-slicing (UI borders), radius and thickness in reference px.</summary>
        public static Sprite RoundedRing(int radius, int thickness)
        {
            long key = ((long)radius << 32) | (uint)thickness;
            if (Rings.TryGetValue(key, out var cached) && cached != null) return cached;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Ring{radius}_{thickness}", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                float dx = Mathf.Max(radius - cx, 0f, cx - (size - radius));
                float dy = Mathf.Max(radius - cy, 0f, cy - (size - radius));
                float sd = Mathf.Sqrt(dx * dx + dy * dy) - radius; // signed distance to the outer edge (negative inside)
                float outer = Mathf.Clamp01(0.5f - sd);
                float inner = Mathf.Clamp01(0.5f - (sd + thickness));
                px[y * size + x] = new Color32(255, 255, 255, (byte)((outer - inner) * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = tex.name;
            Rings[key] = sprite;
            return sprite;
        }

        public static Color Rgba(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);
    }
}
