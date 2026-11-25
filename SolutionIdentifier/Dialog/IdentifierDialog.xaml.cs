#nullable enable
using System;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using SolutionIdentifier.Appearance;

namespace SolutionIdentifier.Dialog
{
    /// <summary>Lets the user customise the IdentifierAppearance.</summary>
    public partial class IdentifierDialog : DialogWindow
    {
        private readonly ColourChoiceRow _colourChoiceRow;
        private readonly IconChoiceRow _iconChoiceRow;

        internal IdentifierDialog(IdentifierAppearance currentIdentifier)
        {
            InitializeComponent();

            TitleTextBox.Text = currentIdentifier.CustomTitle;
            EmptyTitleHint.Text = $"Leave empty to show the Solution path";

            _colourChoiceRow = new ColourChoiceRow(ColourChoices, currentIdentifier.ColourIndex);
            _iconChoiceRow = new IconChoiceRow(IconChoices, currentIdentifier.IconShape, currentIdentifier.ColourIndex);
            _colourChoiceRow.ChosenColourIndexChanged += OnChosenColourIndexChanged;
            _iconChoiceRow.ChosenIconShapeChanged += OnChosenIconShapeChanged;

            // put the cursor in the text box and selects the text
            Loaded += (sender, e) =>
            {
                TitleTextBox.Focus();
                TitleTextBox.SelectAll();
            };
        }

        internal IdentifierDialogAction ChosenAction { get; private set; } = IdentifierDialogAction.Cancel;
        internal IdentifierAppearance ChosenIdentifier => new IdentifierAppearance(TitleTextBox.Text.Trim(), _colourChoiceRow.ChosenColourIndex, _iconChoiceRow.ChosenIconShape);

        private void OnChosenColourIndexChanged(object sender, EventArgs e)
        {
            HideChoiceError();

            if (_colourChoiceRow.HasChosenColour == false)
            {
                return;
            }

            // draw icons in the currently selected colour
            _iconChoiceRow.RedrawIconsInColour(_colourChoiceRow.ChosenColourIndex);
        }

        private void OnChosenIconShapeChanged(object sender, EventArgs e)
        {
            HideChoiceError();
        }

        private void HideChoiceError()
        {
            ChoiceErrorText.Visibility = Visibility.Collapsed;
        }

        private void ChooseSaveAndCloseIfChoicesComplete(object sender, RoutedEventArgs e)
        {
            string? missingChoiceMessage = ReadMissingChoiceMessage();
            if (missingChoiceMessage != null)
            {
                ChoiceErrorText.Text = missingChoiceMessage;
                ChoiceErrorText.Visibility = Visibility.Visible;
                return;
            }

            ChooseActionAndCloseDialog(IdentifierDialogAction.Save);
        }

        private void ChooseResetAndCloseDialog(object sender, RoutedEventArgs e)
        {
            ChooseActionAndCloseDialog(IdentifierDialogAction.Reset);
        }

        private void ChooseOpenOptionsAndCloseDialog(object sender, RoutedEventArgs e)
        {
            ChooseActionAndCloseDialog(IdentifierDialogAction.OpenOptions);
        }

        private void ChooseActionAndCloseDialog(IdentifierDialogAction chosenAction)
        {
            ChosenAction = chosenAction;
            Close();
        }

        private string? ReadMissingChoiceMessage()
        {
            // WPF allows unselecting a ListBox item with Ctrl+click, so the user can unset these choices.

            if (_colourChoiceRow.HasChosenColour == false)
            {
                return "Choose a colour.";
            }

            if (_iconChoiceRow.HasChosenIconShape == false)
            {
                return "Choose an icon.";
            }

            return null;
        }
    }
}
