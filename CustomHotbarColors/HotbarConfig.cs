using BepInEx.Configuration;
using UnityEngine;

namespace CustomHotbarColors
{
    /// <summary>
    /// Binds the hotbar appearance settings to the BepInEx config file. Colors are typed
    /// <see cref="Color"/>, so the configuration window shows a color picker instead of a
    /// hex string. Every feature has its own switch, and the refresh timer reads the entries
    /// on every pass, so a change is applied without restarting the game.
    /// </summary>
    internal static class HotbarConfig
    {
        internal const string SectionGeneral = "1 - General";
        internal const string SectionBackgrounds = "2 - Backgrounds";
        internal const string SectionNumbers = "3 - Numbers";
        internal const string SectionBorder = "4 - Border";
        internal const string SectionBar = "5 - Bar";

        /// <summary>The color the game gives the hotbar numbers (#98D0FF).</summary>
        internal static readonly Color GameNumberColor = new(152f / 255f, 208f / 255f, 1f, 1f);

        /// <summary>Master switch: while false the hotbar is left exactly as the game draws it.</summary>
        internal static ConfigEntry<bool> Enabled;

        /// <summary>How many frames wait between two passes over the hotbar.</summary>
        internal static ConfigEntry<int> RefreshFrames;

        /// <summary>Whether <see cref="AllCellsBackgroundColor"/> is drawn on every cell.</summary>
        internal static ConfigEntry<bool> EnableAllCellsBackground;

        /// <summary>Background drawn under the content of every hotbar cell.</summary>
        internal static ConfigEntry<Color> AllCellsBackgroundColor;

        /// <summary>Whether <see cref="EquippedBackgroundColor"/> is used on the equipped cell.</summary>
        internal static ConfigEntry<bool> EnableEquippedBackground;

        /// <summary>Background of the cell holding the item you currently have equipped.</summary>
        internal static ConfigEntry<Color> EquippedBackgroundColor;

        /// <summary>Whether <see cref="AllCellsNumberColor"/> is used on every cell.</summary>
        internal static ConfigEntry<bool> EnableAllCellsNumberColor;

        /// <summary>Color of the slot number of every hotbar cell.</summary>
        internal static ConfigEntry<Color> AllCellsNumberColor;

        /// <summary>Whether <see cref="EquippedNumberColor"/> is used on the equipped cell.</summary>
        internal static ConfigEntry<bool> EnableEquippedNumberColor;

        /// <summary>Color of the slot number of the cell holding the equipped item.</summary>
        internal static ConfigEntry<Color> EquippedNumberColor;

        /// <summary>Whether a frame is drawn around the equipped cell.</summary>
        internal static ConfigEntry<bool> EnableEquippedBorderColor;

        /// <summary>Color of the frame drawn around the equipped cell.</summary>
        internal static ConfigEntry<Color> EquippedBorderColor;

        /// <summary>Width of that frame, in UI units (about one screen pixel at 1080p).</summary>
        internal static ConfigEntry<float> EquippedBorderThickness;

        /// <summary>How far the frame sits from the cell edge, in UI units, outward positive.</summary>
        internal static ConfigEntry<float> EquippedBorderOffset;

        /// <summary>
        /// Whether the frame lengthens with <see cref="EquippedBorderOffset"/>, so its four
        /// edges keep meeting at the corners.
        /// </summary>
        internal static ConfigEntry<bool> EquippedBorderGrowsWithOffset;

        /// <summary>
        /// Whether the frame is drawn outside the cell instead of over its edge. The frame
        /// starts one thickness beyond the edge, then <see cref="EquippedBorderOffset"/>
        /// slides it further out, and it wins over <see cref="EquippedBorderGrowsWithOffset"/>.
        /// </summary>
        internal static ConfigEntry<bool> EquippedBorderOutsideCell;

        /// <summary>Whether a bar is drawn along one side of the equipped cell.</summary>
        internal static ConfigEntry<bool> EnableEquippedBar;

        /// <summary>Color of that bar.</summary>
        internal static ConfigEntry<Color> EquippedBarColor;

        /// <summary>Thickness of that bar, its small dimension, perpendicular to its side.</summary>
        internal static ConfigEntry<float> EquippedBarThickness;

        /// <summary>Length of that bar along its side, 0 meaning the whole side.</summary>
        internal static ConfigEntry<float> EquippedBarWidth;

        /// <summary>Which side of the cell the bar sits on.</summary>
        internal static ConfigEntry<HotbarBar.Side> EquippedBarSide;

        /// <summary>How far the bar sits from the cell edge it hangs on, outward positive.</summary>
        internal static ConfigEntry<float> EquippedBarOffset;

        /// <summary>How far the bar slides along its side.</summary>
        internal static ConfigEntry<float> EquippedBarOffsetAlong;

        /// <summary>Binds every setting to the config file. Called once, from CustomHotbarColors.Awake.</summary>
        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                SectionGeneral,
                "Enabled",
                true,
                "Master switch. While false the mod restores the hotbar to the colors the "
                    + "game draws on its own."
            );

            RefreshFrames = config.Bind(
                SectionGeneral,
                "RefreshFrames",
                15,
                "How many frames to wait between two passes over the hotbar (15 frames is "
                    + "about 0.25 s at 60 FPS). Higher means less work, slower reaction to "
                    + "changes. Below 1 is treated as 1."
            );

            EnableAllCellsBackground = config.Bind(
                SectionBackgrounds,
                "EnableAllCellsBackground",
                false,
                "Whether a background is drawn under the content of every hotbar cell. While "
                    + "false the cells keep the background the game gives them."
            );

            AllCellsBackgroundColor = config.Bind(
                SectionBackgrounds,
                "AllCellsBackgroundColor",
                new Color(0f, 0f, 0f, 0.35f),
                "Background color of every hotbar cell. Pick a color with the swatch; the "
                    + "alpha channel sets the opacity. The default is a light black veil."
            );

            EnableEquippedBackground = config.Bind(
                SectionBackgrounds,
                "EnableEquippedBackground",
                false,
                "Whether the background of the cell holding the equipped item is recolored. "
                    + "While false that cell keeps the background the game gives it."
            );

            EquippedBackgroundColor = config.Bind(
                SectionBackgrounds,
                "EquippedBackgroundColor",
                new Color(1f, 0f, 0f, 0.35f),
                "Background color of the hotbar cell of the item you currently have equipped "
                    + "(weapon or shield). It wins over AllCellsBackgroundColor on that cell."
            );

            EnableAllCellsNumberColor = config.Bind(
                SectionNumbers,
                "EnableAllCellsNumberColor",
                false,
                "Whether AllCellsNumberColor is used. While false every slot number keeps the "
                    + "color the game gives it."
            );

            AllCellsNumberColor = config.Bind(
                SectionNumbers,
                "AllCellsNumberColor",
                GameNumberColor,
                "Color of the slot number of every hotbar cell. The default #98D0FF is the "
                    + "color the game uses on its own."
            );

            EnableEquippedNumberColor = config.Bind(
                SectionNumbers,
                "EnableEquippedNumberColor",
                true,
                "Whether EquippedNumberColor is used on the equipped cell. While false that "
                    + "number follows AllCellsNumberColor."
            );

            EquippedNumberColor = config.Bind(
                SectionNumbers,
                "EquippedNumberColor",
                new Color(1f, 215f / 255f, 0f, 1f),
                "Color of the slot number of the item you currently have equipped (weapon or "
                    + "shield). It wins over AllCellsNumberColor on that cell."
            );

            EnableEquippedBorderColor = config.Bind(
                SectionBorder,
                "EnableEquippedBorderColor",
                true,
                "Whether a frame is drawn around the cell holding the equipped item."
            );

            EquippedBorderColor = config.Bind(
                SectionBorder,
                "EquippedBorderColor",
                new Color(1f, 1f, 0f, 0.745f),
                "Color of the frame drawn around the hotbar cell of the item you currently "
                    + "have equipped (weapon or shield)."
            );

            EquippedBorderThickness = config.Bind(
                SectionBorder,
                "EquippedBorderThickness",
                3f,
                new ConfigDescription(
                    "Width of the frame drawn around the equipped cell, in UI units (about "
                        + "one screen pixel at 1080p).",
                    new AcceptableValueRange<float>(1f, 20f)
                )
            );

            EquippedBorderOffset = config.Bind(
                SectionBorder,
                "EquippedBorderOffset",
                -1f,
                new ConfigDescription(
                    "Where the frame sits relative to the edge of the equipped cell, in UI "
                        + "units. 0 draws it just inside the cell, a positive value pushes it "
                        + "outside (at +thickness it sits flush against the outside), a "
                        + "negative value pulls it further inside.",
                    new AcceptableValueRange<float>(-20f, 20f)
                )
            );

            EquippedBorderGrowsWithOffset = config.Bind(
                SectionBorder,
                "EquippedBorderGrowsWithOffset",
                true,
                "Whether the frame grows with EquippedBorderOffset, so it still surrounds "
                    + "the whole cell and its four edges keep meeting at the corners. While "
                    + "false the edges only slide, which opens the corners as soon as the "
                    + "offset gets bigger than EquippedBorderThickness."
            );

            EquippedBorderOutsideCell = config.Bind(
                SectionBorder,
                "EquippedBorderOutsideCell",
                true,
                "Draws the frame just outside the equipped cell, one thickness beyond its "
                    + "edge instead of over the edge. EquippedBorderOffset still applies "
                    + "on top of that, and EquippedBorderGrowsWithOffset is ignored so the "
                    + "corners stay closed."
            );

            EnableEquippedBar = config.Bind(
                SectionBar,
                "EnableEquippedBar",
                false,
                "Whether a single bar is drawn along one side of the cell holding the "
                    + "equipped item. It is independent of the frame drawn by "
                    + "EnableEquippedBorderColor, and both can be on at the same time."
            );

            EquippedBarColor = config.Bind(
                SectionBar,
                "EquippedBarColor",
                new Color(1f, 1f, 0f, 1f),
                "Color of the bar drawn on the equipped cell. The alpha channel sets the "
                    + "opacity."
            );

            EquippedBarThickness = config.Bind(
                SectionBar,
                "EquippedBarThickness",
                3f,
                new ConfigDescription(
                    "Thickness of the bar, its small dimension perpendicular to the side "
                        + "it sits on, in UI units (about one screen pixel at 1080p).",
                    new AcceptableValueRange<float>(1f, 20f)
                )
            );

            EquippedBarWidth = config.Bind(
                SectionBar,
                "EquippedBarWidth",
                0f,
                new ConfigDescription(
                    "Length of the bar along its side, in UI units. 0 draws it over the "
                        + "whole side; a shorter bar starts centered on that side, and "
                        + "EquippedBarOffsetAlong moves it along.",
                    new AcceptableValueRange<float>(-100f, 100f)
                )
            );

            EquippedBarSide = config.Bind(
                SectionBar,
                "EquippedBarSide",
                HotbarBar.Side.Bottom,
                "Which side of the equipped cell the bar sits on: Top, Bottom, Left or " + "Right."
            );

            EquippedBarOffset = config.Bind(
                SectionBar,
                "EquippedBarOffset",
                7f,
                new ConfigDescription(
                    "Where the bar sits relative to the cell edge it hangs on, in UI units. "
                        + "0 puts it just inside the cell, a positive value pushes it "
                        + "outside (at +thickness it sits flush against the outside), a "
                        + "negative value pulls it further inside.",
                    new AcceptableValueRange<float>(-20f, 20f)
                )
            );

            EquippedBarOffsetAlong = config.Bind(
                SectionBar,
                "EquippedBarOffsetAlong",
                0f,
                new ConfigDescription(
                    "How far the bar slides along its side, in UI units: positive means "
                        + "right for the top and bottom sides, upward for the left and "
                        + "right sides. 0 keeps it centered.",
                    new AcceptableValueRange<float>(-20f, 20f)
                )
            );
        }
    }
}
