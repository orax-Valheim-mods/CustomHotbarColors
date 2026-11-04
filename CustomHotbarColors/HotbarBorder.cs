using UnityEngine;
using UnityEngine.UI;

namespace CustomHotbarColors
{
    /// <summary>
    /// A frame drawn around a cell: four thin images hugging the cell rectangle. Built once
    /// per cell, then only recolored or resized when the configuration changes.
    /// </summary>
    internal sealed class HotbarBorder : MonoBehaviour
    {
        internal const string ObjectName = "CustomHotbarColorsBorder";

        private Image[] _edges;
        private bool _visible;
        private Color _color;
        private float _thickness;
        private float _offset;
        private bool _grow;

        /// <summary>Shows the frame or hides it, and reports whether anything changed.</summary>
        internal bool Apply(bool visible, Color color, float thickness, float offset, bool grow)
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

            if (!HotbarStyler.SameColor(_color, color))
            {
                for (var i = 0; i < _edges.Length; i++)
                {
                    _edges[i].color = color;
                }

                _color = color;
                wrote = true;
            }

            if (
                !Mathf.Approximately(_thickness, thickness)
                || !Mathf.Approximately(_offset, offset)
                || _grow != grow
            )
            {
                SetGeometry(thickness, offset, grow);
                _thickness = thickness;
                _offset = offset;
                _grow = grow;
                wrote = true;
            }

            return wrote;
        }

        /// <summary>Builds the frame of a cell, without showing a color yet.</summary>
        internal static HotbarBorder Create(Transform cell)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform));
            go.transform.SetParent(cell, false);
            // Last sibling: the frame is drawn over the icon and only covers the cell edges.
            go.transform.SetAsLastSibling();

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var border = go.AddComponent<HotbarBorder>();
            border._edges = new[]
            {
                CreateEdge(
                    rect,
                    "top",
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f)
                ),
                CreateEdge(
                    rect,
                    "bottom",
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f)
                ),
                CreateEdge(
                    rect,
                    "left",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f)
                ),
                CreateEdge(
                    rect,
                    "right",
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0.5f)
                ),
            };
            border._visible = true;
            return border;
        }

        private static Image CreateEdge(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot
        )
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = HotbarStyler.WhiteSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Places the four edges: their thickness, their distance from the cell edge and,
        /// when the frame follows the offset, their length. Lengthening each edge by the
        /// offset on both ends is what keeps the corners closed around the whole cell.
        /// </summary>
        private void SetGeometry(float thickness, float offset, bool grow)
        {
            var extra = grow ? 2f * offset : 0f;
            // The four edges hang on the frame rectangle, which covers the whole cell.
            var size = ((RectTransform)_edges[0].transform.parent).rect.size;
            // A deeply negative offset could ask for a negative size, which draws nothing.
            var horizontal = new Vector2(Mathf.Max(0f, size.x + extra) - size.x, thickness);
            var vertical = new Vector2(thickness, Mathf.Max(0f, size.y + extra) - size.y);

            _edges[0].rectTransform.sizeDelta = horizontal;
            _edges[1].rectTransform.sizeDelta = horizontal;
            _edges[2].rectTransform.sizeDelta = vertical;
            _edges[3].rectTransform.sizeDelta = vertical;

            _edges[0].rectTransform.anchoredPosition = new Vector2(0f, offset);
            _edges[1].rectTransform.anchoredPosition = new Vector2(0f, -offset);
            _edges[2].rectTransform.anchoredPosition = new Vector2(-offset, 0f);
            _edges[3].rectTransform.anchoredPosition = new Vector2(offset, 0f);
        }
    }
}
