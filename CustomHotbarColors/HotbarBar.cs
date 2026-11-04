using UnityEngine;
using UnityEngine.UI;

namespace CustomHotbarColors
{
    /// <summary>
    /// A single bar hung along one side of a cell: one thin image. Built once per cell,
    /// then only recolored, moved or resized when the configuration changes. It is
    /// independent of the frame <see cref="HotbarBorder"/> draws around the cell.
    /// </summary>
    internal sealed class HotbarBar : MonoBehaviour
    {
        internal const string ObjectName = "CustomHotbarColorsBar";

        /// <summary>The side of the cell the bar sits on.</summary>
        internal enum Side
        {
            Top,
            Bottom,
            Left,
            Right,
        }

        private Image _image;
        private bool _visible;
        private Color _color;
        private float _thickness;
        private float _width;
        private float _offset;
        private float _along;
        private Side _side;

        /// <summary>Shows, hides or moves the bar, and reports whether anything changed.</summary>
        internal bool Apply(
            bool visible,
            Color color,
            float thickness,
            float width,
            float offset,
            float along,
            Side side
        )
        {
            var wrote = false;

            if (_visible != visible)
            {
                _visible = visible;
                gameObject.SetActive(visible);
                wrote = true;
            }

            if (!visible)
            {
                return wrote;
            }

            // A sprite that survived a script reload may point at a destroyed one and
            // would draw nothing.
            if (_image.sprite == null)
            {
                _image.sprite = HotbarStyler.WhiteSprite;
            }

            if (!HotbarStyler.SameColor(_color, color))
            {
                _image.color = color;
                _color = color;
                wrote = true;
            }

            if (
                !Mathf.Approximately(_thickness, thickness)
                || !Mathf.Approximately(_width, width)
                || !Mathf.Approximately(_offset, offset)
                || !Mathf.Approximately(_along, along)
                || _side != side
            )
            {
                SetGeometry(thickness, width, offset, along, side);
                _thickness = thickness;
                _width = width;
                _offset = offset;
                _along = along;
                _side = side;
                wrote = true;
            }

            return wrote;
        }

        /// <summary>Builds the bar of a cell, without showing a color yet.</summary>
        internal static HotbarBar Create(Transform cell)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform));
            go.transform.SetParent(cell, false);
            // Last sibling: the bar is drawn over the icon, along the cell edge.
            go.transform.SetAsLastSibling();

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bar = go.AddComponent<HotbarBar>();
            bar._image = CreateImage(rect);
            bar._visible = true;
            return bar;
        }

        private static Image CreateImage(Transform parent)
        {
            var go = new GameObject("bar", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = HotbarStyler.WhiteSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Places the bar: which side it hangs on, its thickness, its length along that
        /// side, its distance from the cell edge and its slide along the side. The
        /// perpendicular offset follows the frame's convention, outward positive.
        /// </summary>
        private void SetGeometry(float thickness, float width, float offset, float along, Side side)
        {
            var size = ((RectTransform)transform).rect.size;

            Vector2 anchorMin;
            Vector2 anchorMax;
            Vector2 pivot;
            Vector2 delta;
            Vector2 position;

            switch (side)
            {
                case Side.Bottom:
                    anchorMin = new Vector2(0f, 0f);
                    anchorMax = new Vector2(1f, 0f);
                    pivot = new Vector2(0.5f, 0f);
                    delta = new Vector2(Stretch(width, size.x), thickness);
                    position = new Vector2(along, -offset);
                    break;
                case Side.Left:
                    anchorMin = new Vector2(0f, 0f);
                    anchorMax = new Vector2(0f, 1f);
                    pivot = new Vector2(0f, 0.5f);
                    delta = new Vector2(thickness, Stretch(width, size.y));
                    position = new Vector2(-offset, along);
                    break;
                case Side.Right:
                    anchorMin = new Vector2(1f, 0f);
                    anchorMax = new Vector2(1f, 1f);
                    pivot = new Vector2(1f, 0.5f);
                    delta = new Vector2(thickness, Stretch(width, size.y));
                    position = new Vector2(offset, along);
                    break;
                default:
                    anchorMin = new Vector2(0f, 1f);
                    anchorMax = new Vector2(1f, 1f);
                    pivot = new Vector2(0.5f, 1f);
                    delta = new Vector2(Stretch(width, size.x), thickness);
                    position = new Vector2(along, offset);
                    break;
            }

            var rect = _image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = delta;
            rect.anchoredPosition = position;
        }

        /// <summary>
        /// The size delta that gives the bar the wanted length: 0 stretches it over the
        /// whole side, otherwise the difference to that side's length.
        /// </summary>
        private static float Stretch(float width, float sideLength)
        {
            return width <= 0f ? 0f : width - sideLength;
        }
    }
}
