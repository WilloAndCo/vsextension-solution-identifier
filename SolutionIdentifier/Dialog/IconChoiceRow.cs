#nullable enable
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SolutionIdentifier.Appearance;

namespace SolutionIdentifier.Dialog
{
    /// <summary>The dialog's row of icon shapes, all drawn in one colour.</summary>
    internal sealed class IconChoiceRow
    {
        // Loads the copy of the PNG that the .csproj's Resource item builds into this DLL.
        // pack = pack file address
        // application:,,, = fixed identifier for a WPF address for a file known at compile time
        // SolutionIdentifier = dll name
        // component = fixed identifier for the dll name
        // Resources/IdentifierIcons.png = path in the csproj
        private static readonly Uri IconStripUri = new Uri("pack://application:,,,/SolutionIdentifier;component/Resources/IdentifierIcons.png");
        private const int IconSizeInPixels = 16;
        
        private readonly BitmapSource _iconStrip = new BitmapImage(IconStripUri);        
        private readonly ListBox _iconChoicesList;

        public IconChoiceRow(ListBox iconChoicesList, IdentifierIconShape initialIconShape, int initialColourIndex)
        {
            _iconChoicesList = iconChoicesList;
            
            foreach (IdentifierIconShape iconShape in IdentifierPalette.IconShapes)
            {
                _iconChoicesList.Items.Add(CreateIconItem(iconShape, initialColourIndex));
            }

            _iconChoicesList.SelectedIndex = (int)initialIconShape;
            _iconChoicesList.SelectionChanged += OnIconSelectionChanged;
        }

        public event EventHandler? ChosenIconShapeChanged;

        public bool HasChosenIconShape => _iconChoicesList.SelectedIndex >= 0;
        public IdentifierIconShape ChosenIconShape => (IdentifierIconShape)_iconChoicesList.SelectedIndex;

        public void RedrawIconsInColour(int colourIndex)
        {
            // we can map shape index to position in list
            foreach (IdentifierIconShape iconShape in IdentifierPalette.IconShapes)
            {
                var listItem = (ListBoxItem)_iconChoicesList.Items[(int)iconShape];
                ((Image)listItem.Content).Source = ReadIconFromStrip(iconShape, colourIndex);
            }
        }

        private ListBoxItem CreateIconItem(IdentifierIconShape iconShape, int colourIndex)
        {
            var iconImage = new Image
            {
                Width = IconSizeInPixels,
                Height = IconSizeInPixels,
                Source = ReadIconFromStrip(iconShape, colourIndex),
                ToolTip = iconShape.ToString(),
            };

            // Sharper images if display scales them up
            RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);

            var item = new ListBoxItem { Content = iconImage };

            // automationName needed for UI tests to reference
            AutomationProperties.SetName(item, iconShape.ToString());

            return item;
        }

        private BitmapSource ReadIconFromStrip(IdentifierIconShape iconShape, int colourIndex)
        {
            int iconXPos = IdentifierPalette.CalculateColouredIconIndex(iconShape, colourIndex) * IconSizeInPixels;
            return new CroppedBitmap(_iconStrip, new Int32Rect(iconXPos, 0, IconSizeInPixels, IconSizeInPixels));
        }

        private void OnIconSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChosenIconShapeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
