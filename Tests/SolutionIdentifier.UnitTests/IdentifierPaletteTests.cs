using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using SolutionIdentifier.Appearance;
using Xunit;

namespace SolutionIdentifier.UnitTests
{
    /// <summary>Files in this repository needed for tests</summary>
    internal static class RepositoryFiles
    {
        public static string IconStripPath => Path.Combine(FindRepositoryRoot(), @"SolutionIdentifier\Resources\IdentifierIcons.png");
        public static string CommandTablePath => Path.Combine(FindRepositoryRoot(), @"SolutionIdentifier\SolutionIdentifierPackage.vsct");

        private static string FindRepositoryRoot()
        {
            // Walks up from the output folder until it finds the solution file. The files can be referenced from there.
            for (string? directory = AppDomain.CurrentDomain.BaseDirectory; directory != null; directory = Path.GetDirectoryName(directory))
            {
                if (File.Exists(Path.Combine(directory, "SolutionIdentifier.sln")))
                {
                    return directory;
                }
            }

            throw new InvalidOperationException($"SolutionIdentifier.sln not found above {AppDomain.CurrentDomain.BaseDirectory}.");
        }
    }

    [Trait("Category", "Unit")]
    public sealed class IdentifierPaletteTests
    {
        private const int IconSizeInPixels = 16;

        [Fact]
        public void BuildAllColouredIcons_EveryColourAndShape_ReturnsEachInShapeThenColourOrder()
        {
            Assert.Equal(ListEveryIconShapeAndColourIndex(), IdentifierPalette.BuildAllColouredIcons());
        }

        [Fact]
        public void CalculateColouredIconIndex_EveryColourAndShape_ReturnsItsPositionInTheList()
        {
            List<(IdentifierIconShape IconShape, int ColourIndex)> everyShapeAndColour = ListEveryIconShapeAndColourIndex().ToList();

            for (int position = 0; position < everyShapeAndColour.Count; position++)
            {
                (IdentifierIconShape iconShape, int colourIndex) = everyShapeAndColour[position];
                Assert.Equal(position, IdentifierPalette.CalculateColouredIconIndex(iconShape, colourIndex));
            }
        }

        [Fact]
        public void FindIconShape_NameOfEveryShape_ReturnsThatShape()
        {
            foreach (IdentifierIconShape iconShape in IdentifierPalette.IconShapes)
            {
                Assert.Equal(iconShape, IdentifierPalette.IconShapeFromString(iconShape.ToString()));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("Triangle")]
        [InlineData("star")]
        [InlineData("7")]
        public void FindIconShape_NotAShapeName_ReturnsNull(string iconShapeName)
        {
            Assert.Null(IdentifierPalette.IconShapeFromString(iconShapeName));
        }

        [Fact]
        public void FindColourIndex_NameNotInPalette_ReturnsNull()
        {
            // Grey not expected in palette
            Assert.Null(IdentifierPalette.ColourIndexFromString("Grey"));
        }

        [Fact]
        public void IconStrip_Size_HoldsEveryColouredIcon()
        {
            BitmapSource iconStrip = ReadIconStrip();

            Assert.Equal(IdentifierPalette.ColouredIconCount * IconSizeInPixels, iconStrip.PixelWidth);
            Assert.Equal(IconSizeInPixels, iconStrip.PixelHeight);
        }

        [Fact]
        public void IconStrip_SquareIcons_AreFilledWithPaletteColoursInOrder()
        {
            BitmapSource iconStrip = ReadIconStrip();

            for (int colourIndex = 0; colourIndex < IdentifierPalette.Colours.Length; colourIndex++)
            {
                int iconLeft = IdentifierPalette.CalculateColouredIconIndex(IdentifierIconShape.Square, colourIndex) * IconSizeInPixels;
                Color centre = ReadPixel(iconStrip, x: iconLeft + (IconSizeInPixels / 2), y: IconSizeInPixels / 2);

                Assert.Equal(IdentifierPalette.Colours[colourIndex].Fill, centre);
            }
        }

        [Fact]
        public void IconStrip_EveryIcon_HasCyanAtAlpha1InTopRightPixel()
        {
            BitmapSource iconStrip = ReadIconStrip();

            // The undocumented marker that stops Visual Studio recolouring the icons on dark themes - Cyan alpha 1
            Color cyanAtAlpha1 = Color.FromArgb(1, 0x00, 0xFF, 0xFF);

            for (int colouredIconIndex = 0; colouredIconIndex < IdentifierPalette.ColouredIconCount; colouredIconIndex++)
            {
                Color topRight = ReadPixel(iconStrip, x: (colouredIconIndex * IconSizeInPixels) + IconSizeInPixels - 1, y: 0);
                Assert.True(topRight == cyanAtAlpha1, $"Icon {colouredIconIndex}'s top-right pixel is {topRight}, not cyan at alpha 1.");
            }
        }

        [Fact]
        public void CommandTable_ColourAndShapeButtons_OnePerColouredIconInPaletteOrder()
        {
            XDocument commandTable = XDocument.Load(RepositoryFiles.CommandTablePath);

            // Every element in a .vsct file is in this namespace
            XNamespace commandTableNamespace = "http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable";

            // The colour-and-shape buttons, in the order they appear in the file
            List<XElement> colourAndShapeButtons = commandTable.Descendants(commandTableNamespace + "Button")
                .Where(button => (string?)button.Element(commandTableNamespace + "Parent")?.Attribute("id") == "ColourAndShapeButtonsGroup")
                .ToList();

            // The ID numbers listed in the .vsct, e.g. "RoseSquareButton" -> 0x0300 and "bmpRoseSquare" -> 1
            Dictionary<string, int> commandIdsByName = ReadIdSymbols(commandTable, commandTableNamespace, "guidExtensionCmdSet");
            Dictionary<string, int> iconIdsByName = ReadIdSymbols(commandTable, commandTableNamespace, "guidIdentifierIcons");

            // One button for every combination of colour and shape
            Assert.Equal(IdentifierPalette.ColouredIconCount, colourAndShapeButtons.Count);

            for (int colouredIconIndex = 0; colouredIconIndex < IdentifierPalette.ColouredIconCount; colouredIconIndex++)
            {
                // The colour and shape the palette expects at this position
                int colourIndex = IdentifierPalette.CalculateColourIndexFromColouredIconIndex(colouredIconIndex);
                string colourName = IdentifierPalette.Colours[colourIndex].Name;
                IdentifierIconShape iconShape = IdentifierPalette.CalculateIconShapeFromColouredIconIndex(colouredIconIndex);

                // What the .vsct has at this position
                string buttonName = (string)colourAndShapeButtons[colouredIconIndex].Attribute("id");
                string iconName = (string)colourAndShapeButtons[colouredIconIndex].Element(commandTableNamespace + "Icon").Attribute("id");

                // The button is named after its colour and shape, and has the command ID MenuBarSplitButton registers for it
                Assert.Equal($"{colourName}{iconShape}Button", buttonName);
                Assert.Equal(IdentifierPalette.CalculateColourAndShapeButtonId(iconShape, colourIndex), commandIdsByName[buttonName]);

                // The button's icon is named after the same colour and shape
                Assert.Equal($"bmp{colourName}{iconShape}", iconName);

                // The icon points at this position in the strip. Icon IDs count from 1: the first icon in the strip is 1.
                Assert.Equal(colouredIconIndex + 1, iconIdsByName[iconName]);
            }
        }

        private static IEnumerable<(IdentifierIconShape IconShape, int ColourIndex)> ListEveryIconShapeAndColourIndex()
        {
            foreach (IdentifierIconShape iconShape in IdentifierPalette.IconShapes)
            {
                for (int colourIndex = 0; colourIndex < IdentifierPalette.Colours.Length; colourIndex++)
                {
                    yield return (iconShape, colourIndex);
                }
            }
        }

        private static BitmapSource ReadIconStrip()
        {            
            var iconStrip = new BitmapImage();
            iconStrip.BeginInit();
            iconStrip.CacheOption = BitmapCacheOption.OnLoad; // read whole image during end init, then close it
            iconStrip.UriSource = new Uri(RepositoryFiles.IconStripPath);
            iconStrip.EndInit();

            // Bgra32 keeps the colours unscaled at alpha to easily identify expected colours
            return new FormatConvertedBitmap(iconStrip, PixelFormats.Bgra32, null, 0);
        }

        private static Color ReadPixel(BitmapSource bitmap, int x, int y)
        {
            byte[] pixel = new byte[4];

            // No GetPixel(), have to copy
            bitmap.CopyPixels(new System.Windows.Int32Rect(x, y, 1, 1), pixel, stride: 4, offset: 0);

            // Bgra32 order
            return Color.FromArgb(a: pixel[3], r: pixel[2], g: pixel[1], b: pixel[0]);
        }

        /// <summary>The IDSymbols under one GuidSymbol, by name. Values are hex ("0x0200") or decimal ("1").</summary>
        private static Dictionary<string, int> ReadIdSymbols(XDocument commandTable, XNamespace commandTableNamespace, string guidSymbolName)
        {
            // Find the one <GuidSymbol name="..."> block, e.g. guidIdentifierIcons. Single() fails the test if it is missing or repeated.
            XElement guidSymbol = commandTable.Descendants(commandTableNamespace + "GuidSymbol").Single(symbol => (string)symbol.Attribute("name") == guidSymbolName);

            // Turn each <IDSymbol name="bmpRoseSquare" value="1" /> inside it into an entry
            return guidSymbol.Elements(commandTableNamespace + "IDSymbol").ToDictionary(
                idSymbol => (string)idSymbol.Attribute("name"),
                idSymbol => ParseIdSymbolValue((string)idSymbol.Attribute("value")));
        }

        /// <summary>Turns an IDSymbol's value text into a number: "0x0300" is read as hex (768), "1" as decimal.</summary>
        private static int ParseIdSymbolValue(string value)
        {
            // int.Parse can't read the "0x" prefix, so strip it and parse the rest as hex
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
                        
            return int.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
