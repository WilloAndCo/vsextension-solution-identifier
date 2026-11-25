#nullable enable
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using SolutionIdentifier.Appearance;

namespace SolutionIdentifier.Dialog
{
    /// <summary>The dialog's row of colour swatches, one for each colour in IdentifierPalette.</summary>
    internal sealed class ColourChoiceRow
    {
        private readonly ListBox _colourChoicesList;

        public ColourChoiceRow(ListBox colourChoicesList, int initialColourIndex)
        {
            _colourChoicesList = colourChoicesList;

            foreach ((string colourName, Color fill, Color outline) in IdentifierPalette.Colours)
            {
                _colourChoicesList.Items.Add(CreateColourSwatch(colourName, fill, outline));
            }

            _colourChoicesList.SelectedIndex = initialColourIndex;
            _colourChoicesList.SelectionChanged += OnColourSelectionChanged;
        }

        public event EventHandler? ChosenColourIndexChanged;

        public bool HasChosenColour => _colourChoicesList.SelectedIndex >= 0;
        public int ChosenColourIndex => _colourChoicesList.SelectedIndex;

        private static ListBoxItem CreateColourSwatch(string colourName, Color fill, Color outline)
        {
            const double SwatchSizeInPixels = 22;
            const double SwatchCornerRadiusInPixels = 4;

            var swatch = new Border
            {
                Width = SwatchSizeInPixels,
                Height = SwatchSizeInPixels,
                CornerRadius = new CornerRadius(SwatchCornerRadiusInPixels),
                Background = new SolidColorBrush(fill),
                BorderBrush = new SolidColorBrush(outline),
                BorderThickness = new Thickness(1),
                ToolTip = colourName,
            };
            var item = new ListBoxItem { Content = swatch };

            // automationName needed for UI tests to reference
            AutomationProperties.SetName(item, colourName);

            return item;
        }

        private void OnColourSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChosenColourIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
