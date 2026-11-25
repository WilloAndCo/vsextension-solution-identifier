using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SolutionIdentifier.Appearance
{
    /// <summary>The colours and icon shapes built into the extension.</summary>
    internal static class IdentifierPalette
    {
        // Must match the first guidExtensionCmdSet colour shape entry in .vsct. Values assumed to be incremental.
        private const int FirstColourAndShapeButtonId = 0x0300;

        // Same order and values as the icons in Resources\IdentifierIcons.png
        // Palette colours are modern pastel with an outline at 55% brightness of colour
        public static readonly (string Name, Color Fill, Color Outline)[] Colours =
        {
            ("Rose", Color.FromRgb(0xF4, 0xA6, 0xA6), Color.FromRgb(0x86, 0x5B, 0x5B)),
            ("Peach", Color.FromRgb(0xF8, 0xC2, 0x91), Color.FromRgb(0x88, 0x6B, 0x50)),
            ("Butter", Color.FromRgb(0xFF, 0xE0, 0x8A), Color.FromRgb(0x8C, 0x7B, 0x4C)),
            ("Mint", Color.FromRgb(0xB8, 0xE0, 0xA8), Color.FromRgb(0x65, 0x7B, 0x5C)),
            ("Aqua", Color.FromRgb(0xA8, 0xDA, 0xDC), Color.FromRgb(0x5C, 0x78, 0x79)),
            ("Sky", Color.FromRgb(0xA9, 0xC6, 0xF5), Color.FromRgb(0x5D, 0x6D, 0x87)),
            ("Lavender", Color.FromRgb(0xCD, 0xB4, 0xF0), Color.FromRgb(0x71, 0x63, 0x84)),
            ("Pink", Color.FromRgb(0xF5, 0xB3, 0xD3), Color.FromRgb(0x87, 0x62, 0x74)),
            ("Cloud", Color.FromRgb(0xD5, 0xD8, 0xDC), Color.FromRgb(0x75, 0x77, 0x79)),
            ("Sand", Color.FromRgb(0xE6, 0xD3, 0xB3), Color.FromRgb(0x7F, 0x74, 0x62)),
        };

        public static readonly IdentifierIconShape[] IconShapes = (IdentifierIconShape[])Enum.GetValues(typeof(IdentifierIconShape));

        /// <summary>One coloured icon for every combination of colour and icon shape.</summary>
        public static int ColouredIconCount => Colours.Length * IconShapes.Length;

        public static int? ColourIndexFromString(string colourName)
        {
            int colourIndex = Array.FindIndex(Colours, colour => colour.Name == colourName);
            if (colourIndex < 0)
            {
                return null;
            }

            return colourIndex;
        }

        public static IdentifierIconShape? IconShapeFromString(string iconShapeName)
        {
            // Checked as a string, because Enum.Parse also accepts numbers, such as "1", and lists, such as "Square,Circle".
            if (Enum.IsDefined(typeof(IdentifierIconShape), iconShapeName) == false)
            {
                return null;
            }

            return (IdentifierIconShape)Enum.Parse(typeof(IdentifierIconShape), iconShapeName);
        }

        /// <summary>Position in the icon strip</summary>
        public static int CalculateColouredIconIndex(IdentifierIconShape iconShape, int colourIndex)
        {
            return (int)iconShape * Colours.Length + colourIndex;
        }

        /// <summary>The colour part of a <see cref="CalculateColouredIconIndex"/> result.</summary>
        public static int CalculateColourIndexFromColouredIconIndex(int colouredIconIndex)
        {
            return colouredIconIndex % Colours.Length;
        }

        /// <summary>The icon shape part of a <see cref="CalculateColouredIconIndex"/> result.</summary>
        public static IdentifierIconShape CalculateIconShapeFromColouredIconIndex(int colouredIconIndex)
        {
            return (IdentifierIconShape)(colouredIconIndex / Colours.Length);
        }

        public static int CalculateColourAndShapeButtonId(IdentifierIconShape iconShape, int colourIndex)
        {            
            return FirstColourAndShapeButtonId + CalculateColouredIconIndex(iconShape, colourIndex);
        }

        /// <summary>The shape and colour of every coloured icon. A position in the list is a coloured icon index.</summary>
        public static List<(IdentifierIconShape IconShape, int ColourIndex)> BuildAllColouredIcons()
        {
            var colouredIcons = new List<(IdentifierIconShape IconShape, int ColourIndex)>();
            for (int colouredIconIndex = 0; colouredIconIndex < ColouredIconCount; colouredIconIndex++)
            {
                colouredIcons.Add((CalculateIconShapeFromColouredIconIndex(colouredIconIndex), CalculateColourIndexFromColouredIconIndex(colouredIconIndex)));
            }

            return colouredIcons;
        }
    }
}
