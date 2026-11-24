using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LogitracDrive
{
    /// <summary>Logitrac palette (from logitrac.com).</summary>
    public static class Palette
    {
        public static readonly Color Primary = Hex(0x247BFE);
        public static readonly Color Background = Hex(0x171717);
        public static readonly Color Surface = Hex(0x28292B);
        public static readonly Color Border = Hex(0x3A3B3E);
        public static readonly Color TextPrimary = Hex(0xFFFFFF);
        public static readonly Color TextSecondary = Hex(0xC1CACF);
        public static readonly Color Link = Hex(0x8FC3FF);
        public static readonly Color Scrim = new Color(0f, 0f, 0f, 0.7f);
        public static readonly Color BadgeBg = new Color(23f / 255f, 23f / 255f, 23f / 255f, 0.85f);

        public static Color Hex(int rgb, float a = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
        }
    }

    /// <summary>Small helpers for building uGUI from code.</summary>
    public static class UiKit
    {
        static readonly Dictionary<int, Sprite> RoundedCache = new Dictionary<int, Sprite>();
        static Sprite _fadeSprite;

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>
        /// Sets anchors, pivot and both offsets in one call. Use this for anything stretched along an axis:
        /// a new RectTransform starts at sizeDelta (100,100), so setting anchors alone leaves it 100 units
        /// larger than its parent on every stretched axis.
        /// </summary>
        public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            Place(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(left, bottom), new Vector2(-right, -top));
        }

        public static Image Panel(RectTransform rt, Color color, int radius = 0, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (radius > 0)
            {
                img.sprite = Rounded(radius);
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(Transform parent, string name, string content, Font font, int size, Color color,
            FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft, float lineSpacing = 1.1f)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.lineSpacing = lineSpacing;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static VerticalLayoutGroup VStack(GameObject go, int left, int right, int top, int bottom, float spacing,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(left, right, top, bottom);
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HStack(GameObject go, float spacing, TextAnchor align = TextAnchor.MiddleLeft,
            int left = 0, int right = 0, int top = 0, int bottom = 0)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(left, right, top, bottom);
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Size(GameObject go, float width = -1, float height = -1, float flexibleWidth = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>(); // no ?? : Unity's fake-null objects break it
            if (width >= 0) { le.minWidth = width; le.preferredWidth = width; }
            if (height >= 0) { le.minHeight = height; le.preferredHeight = height; }
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
            return le;
        }

        /// <summary>Solid button with a centered label.</summary>
        public static Button Button(Transform parent, string name, string label, Font font, int fontSize, Color bg,
            Color fg, int radius, FontStyle style = FontStyle.Bold)
        {
            var rt = Node(name, parent);
            var img = Panel(rt, bg, radius, raycast: true);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            btn.colors = colors;
            var text = Label(rt, "Label", label, font, fontSize, fg, style, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return btn;
        }

        /// <summary>
        /// "object-fit: cover" for an Image: the image fills its parent, centered, keeping its aspect ratio.
        /// The parent should clip (RectMask2D).
        /// </summary>
        public static Image CoverImage(RectTransform parent, string name)
        {
            var rt = Node(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            var fitter = rt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            return img;
        }

        public static void SetCoverSprite(Image img, Sprite sprite)
        {
            img.sprite = sprite;
            img.enabled = sprite != null;
            if (sprite != null)
            {
                var fitter = img.GetComponent<AspectRatioFitter>();
                if (fitter != null) fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
        }

        /// <summary>Procedural rounded-rect sprite for 9-slicing (radius in reference px).</summary>
        public static Sprite Rounded(int radius)
        {
            if (RoundedCache.TryGetValue(radius, out var cached) && cached != null) return cached;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Rounded" + radius,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                float dx = Mathf.Max(radius - cx, 0f, cx - (size - radius));
                float dy = Mathf.Max(radius - cy, 0f, cy - (size - radius));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.DontSave;
            RoundedCache[radius] = sprite;
            return sprite;
        }

        /// <summary>Vertical fade sprite: transparent at the top, opaque at the bottom.</summary>
        public static Sprite BottomFade()
        {
            if (_fadeSprite != null) return _fadeSprite;
            const int h = 64;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false)
            {
                name = "BottomFade",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            for (int y = 0; y < h; y++) tex.SetPixel(0, y, new Color(1f, 1f, 1f, 1f - y / (float)(h - 1)));
            tex.Apply(false, true);
            _fadeSprite = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
            _fadeSprite.hideFlags = HideFlags.DontSave;
            return _fadeSprite;
        }
    }
}
