using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomHotbarColors
{
    /// <summary>
    /// Restyles the hotbar cells: backgrounds, slot numbers and the highlight of the equipped
    /// item. The work runs on a timer instead of on every frame, the cells are resolved only
    /// when the bar itself changes, and a value is written only when it differs from what is
    /// already on screen, so a pass that finds nothing to do costs a few comparisons.
    /// </summary>
    internal sealed class HotbarStyler : MonoBehaviour
    {
        private const string BindingName = "binding";
        private const string EquipedName = "equiped";

        /// <summary>
        /// Name of the hotbar the game itself creates. Other mods clone it and give it
        /// their own name, so those bars are never restyled.
        /// </summary>
        private const string VanillaBarName = "HotKeyBar";

        /// <summary>Name of the background the mod draws under the content of a cell.</summary>
        private const string OverlayName = "CustomHotbarColorsBackground";

        private static Sprite _whiteSprite;

        private readonly List<CellState> _cells = new List<CellState>();

        private HotkeyBar _bar;
        private Coroutine _refresh;

        /// <summary>Whether anything of the mod is currently written on the bar.</summary>
        private bool _styled;

        private bool _originalsRead;
        private Color _originalBindingColor = Color.white;
        private Color _originalEquipedColor = Color.white;

        /// <summary>
        /// Styles <paramref name="bar"/>, starting the refresh timer once per bar. Called when
        /// the game switches the bar on, and after a script reload, which takes the component
        /// away from the bars that are already on screen.
        /// </summary>
        internal static void Ensure(HotkeyBar bar)
        {
            // Only the vanilla bar carries the name; a bar instantiated by another mod
            // keeps the component but not that name.
            if (!bar || bar.name != VanillaBarName)
            {
                return;
            }

            var styler = bar.GetComponent<HotbarStyler>();
            if (styler == null)
            {
                styler = bar.gameObject.AddComponent<HotbarStyler>();
            }

            styler.StartRefresh(bar);
        }

        private void StartRefresh(HotkeyBar bar)
        {
            _bar = bar;
            if (_refresh != null)
            {
                StopCoroutine(_refresh);
            }

            _refresh = StartCoroutine(RefreshLoop());
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                Refresh();
                // One yield per frame, so the delay follows the frame rate the player has.
                var frames = Mathf.Max(1, HotbarConfig.RefreshFrames.Value);
                for (var i = 0; i < frames; i++)
                {
                    yield return null;
                }
            }
        }

        private void Refresh()
        {
            var enabled = HotbarConfig.Enabled.Value;

            // Nothing was ever written on this bar, so there is nothing to put back and the
            // mod can stay silent while it is switched off.
            if (!enabled && !_styled)
            {
                return;
            }

            var bar = _bar;
            if (!bar)
            {
                _cells.Clear();
                _styled = false;
                return;
            }

            var root = bar.transform;
            if (!CacheMatches(root))
            {
                RebuildCache(root);
            }

            if (!_originalsRead)
            {
                ReadOriginals(bar);
            }

            var wrote = false;
            for (var i = 0; i < _cells.Count; i++)
            {
                wrote |= Apply(_cells[i], enabled);
            }

            if (enabled)
            {
                _styled = true;
            }
            else if (!wrote)
            {
                // Every cell went back to the colors of the game: the next passes can return
                // immediately until the mod is switched on again.
                _styled = false;
            }
        }

        /// <summary>Whether the cached cells are still the children the bar has right now.</summary>
        private bool CacheMatches(Transform root)
        {
            if (_cells.Count != root.childCount)
            {
                return false;
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Cell != root.GetChild(i))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves the parts of every cell again. The game rebuilds all of its cells at once
        /// when the number of bound items changes, which the cache comparison catches.
        /// </summary>
        private void RebuildCache(Transform root)
        {
            _cells.Clear();
            for (var i = 0; i < root.childCount; i++)
            {
                var cell = root.GetChild(i);
                var state = new CellState { Cell = cell };

                var binding = cell.Find(BindingName);
                if (binding != null)
                {
                    state.Binding = binding.GetComponent<TMP_Text>();
                }

                var marker = cell.Find(EquipedName);
                if (marker != null)
                {
                    state.Marker = marker;
                    state.MarkerImage = GetImage(marker);
                }

                var overlay = cell.Find(OverlayName);
                if (overlay != null)
                {
                    state.Overlay = overlay.GetComponent<Image>();
                }

                var border = cell.Find(HotbarBorder.ObjectName);
                if (border != null)
                {
                    state.Border = border.GetComponent<HotbarBorder>();
                    // A frame left behind by an unloaded script has no component left to
                    // recolor, so it is thrown away instead of being reused.
                    if (state.Border == null)
                    {
                        Destroy(border.gameObject);
                    }
                }

                var bar = cell.Find(HotbarBar.ObjectName);
                if (bar != null)
                {
                    state.Bar = bar.GetComponent<HotbarBar>();
                    // Same as the frame: a bar without its component cannot be moved or
                    // recolored, so it is thrown away.
                    if (state.Bar == null)
                    {
                        Destroy(bar.gameObject);
                    }
                }

                _cells.Add(state);
            }
        }

        /// <summary>
        /// Reads the colors the game gives the numbers and the equipped background, from the
        /// prefab the bar instantiates its cells from, so the mod can put them back.
        /// </summary>
        private void ReadOriginals(HotkeyBar bar)
        {
            var prefab = bar.m_elementPrefab;
            if (!prefab)
            {
                return;
            }

            var binding = prefab.transform.Find(BindingName);
            var text = binding == null ? null : binding.GetComponent<TMP_Text>();
            if (text != null)
            {
                _originalBindingColor = text.color;
            }

            var marker = prefab.transform.Find(EquipedName);
            var image = GetImage(marker);
            if (image != null)
            {
                _originalEquipedColor = image.color;
            }

            _originalsRead = true;
        }

        /// <summary>Writes one cell, and reports whether anything was written on it.</summary>
        private bool Apply(CellState cell, bool enabled)
        {
            // Only the hotbar cells carry a "binding" number: anything else the game hangs
            // under the bar is left alone.
            if (cell.Binding == null)
            {
                return false;
            }

            var wrote = false;
            var equipped = cell.Marker != null && cell.Marker.gameObject.activeSelf;

            // Slot number. A switch that is off writes nothing at all: the color only goes
            // back to the game's own once, for the cells the mod had colored itself.
            Color? numberColor = null;
            if (enabled && equipped && HotbarConfig.EnableEquippedNumberColor.Value)
            {
                numberColor = HotbarConfig.EquippedNumberColor.Value;
            }
            else if (enabled && HotbarConfig.EnableAllCellsNumberColor.Value)
            {
                numberColor = HotbarConfig.AllCellsNumberColor.Value;
            }

            if (numberColor.HasValue)
            {
                wrote |= Write(cell.Binding, numberColor.Value);
                cell.BindingStyled = true;
            }
            else if (cell.BindingStyled)
            {
                wrote |= Write(cell.Binding, _originalBindingColor);
                cell.BindingStyled = false;
            }

            // Background of the equipped cell, tinted on the marker the game itself draws
            // there, so the highlight the player already sees is the one being recolored.
            // Same rule as the number: while the switch is off the marker is not touched.
            var tintMarker = cell.MarkerImage != null;
            if (tintMarker)
            {
                if (enabled && equipped && HotbarConfig.EnableEquippedBackground.Value)
                {
                    wrote |= Write(cell.MarkerImage, HotbarConfig.EquippedBackgroundColor.Value);
                    cell.MarkerStyled = true;
                }
                else if (cell.MarkerStyled)
                {
                    wrote |= Write(cell.MarkerImage, _originalEquipedColor);
                    cell.MarkerStyled = false;
                }
            }

            // Background of every cell, drawn by the mod under the icon and the number. A
            // prefab whose equipped marker has no image of its own gets that color here
            // instead, which is the only way to show it at all.
            var equippedOnOverlay =
                !tintMarker && enabled && equipped && HotbarConfig.EnableEquippedBackground.Value;
            var wantOverlay =
                equippedOnOverlay || (enabled && HotbarConfig.EnableAllCellsBackground.Value);

            if (cell.Overlay == null && wantOverlay)
            {
                cell.Overlay = CreateOverlay(cell.Cell);
            }

            if (cell.Overlay != null)
            {
                if (cell.Overlay.gameObject.activeSelf != wantOverlay)
                {
                    cell.Overlay.gameObject.SetActive(wantOverlay);
                    wrote = true;
                }

                if (wantOverlay)
                {
                    var overlayColor = equippedOnOverlay
                        ? HotbarConfig.EquippedBackgroundColor.Value
                        : HotbarConfig.AllCellsBackgroundColor.Value;
                    // The sprite is made at runtime: an image that survived a script reload
                    // may point at a destroyed one and would draw nothing.
                    if (cell.Overlay.sprite == null)
                    {
                        cell.Overlay.sprite = WhiteSprite;
                    }

                    wrote |= Write(cell.Overlay, overlayColor);
                }
            }

            // Frame around the equipped cell.
            var wantBorder = enabled && equipped && HotbarConfig.EnableEquippedBorderColor.Value;

            if (cell.Border == null && wantBorder)
            {
                cell.Border = HotbarBorder.Create(cell.Cell);
            }

            if (cell.Border != null)
            {
                // Outside the cell the frame starts one thickness beyond the edge, then
                // EquippedBorderOffset slides it further out; it still lengthens to close
                // its corners at that new distance.
                var outside = HotbarConfig.EquippedBorderOutsideCell.Value;
                var thickness = HotbarConfig.EquippedBorderThickness.Value;
                var offset = HotbarConfig.EquippedBorderOffset.Value;
                wrote |= cell.Border.Apply(
                    wantBorder,
                    HotbarConfig.EquippedBorderColor.Value,
                    thickness,
                    outside ? thickness + offset : offset,
                    outside || HotbarConfig.EquippedBorderGrowsWithOffset.Value
                );
            }

            // Bar along one side of the equipped cell, independent of the frame.
            var wantBar = enabled && equipped && HotbarConfig.EnableEquippedBar.Value;

            if (cell.Bar == null && wantBar)
            {
                cell.Bar = HotbarBar.Create(cell.Cell);
            }

            if (cell.Bar != null)
            {
                wrote |= cell.Bar.Apply(
                    wantBar,
                    HotbarConfig.EquippedBarColor.Value,
                    HotbarConfig.EquippedBarThickness.Value,
                    HotbarConfig.EquippedBarWidth.Value,
                    HotbarConfig.EquippedBarOffset.Value,
                    HotbarConfig.EquippedBarOffsetAlong.Value,
                    HotbarConfig.EquippedBarSide.Value
                );
            }

            return wrote;
        }

        /// <summary>Writes the color only when it differs, so nothing is dirtied for nothing.</summary>
        private static bool Write(Graphic graphic, Color color)
        {
            if (SameColor(graphic.color, color))
            {
                return false;
            }

            graphic.color = color;
            return true;
        }

        internal static bool SameColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f
                && Mathf.Abs(a.g - b.g) < 0.002f
                && Mathf.Abs(a.b - b.b) < 0.002f
                && Mathf.Abs(a.a - b.a) < 0.002f;
        }

        /// <summary>The image of a transform, or null when it carries none.</summary>
        private static Image GetImage(Transform transform)
        {
            if (transform == null)
            {
                return null;
            }

            var image = transform.GetComponent<Image>();
            return image != null ? image : transform.GetComponentInChildren<Image>(true);
        }

        /// <summary>A background that covers the whole cell, drawn under everything in it.</summary>
        private static Image CreateOverlay(Transform cell)
        {
            var go = new GameObject(OverlayName, typeof(RectTransform));
            go.transform.SetParent(cell, false);
            // First sibling: the background is drawn under the icon, the number and the
            // highlights the game puts on the cell.
            go.transform.SetSiblingIndex(0);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A plain white pixel, stretched by the backgrounds and the frames.</summary>
        internal static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                return _whiteSprite;
            }
        }

        /// <summary>The parts of a hotbar cell the mod looks at.</summary>
        private sealed class CellState
        {
            internal Transform Cell;
            internal TMP_Text Binding;
            internal Transform Marker;
            internal Image MarkerImage;
            internal Image Overlay;
            internal HotbarBorder Border;
            internal HotbarBar Bar;

            /// <summary>
            /// Whether the mod itself colored the game's own number and marker, so a switch
            /// turned off knows it has something to put back and then leaves them alone.
            /// </summary>
            internal bool BindingStyled;
            internal bool MarkerStyled;
        }
    }
}
