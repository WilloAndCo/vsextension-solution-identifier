using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SolutionIdentifier.Appearance;

namespace SolutionIdentifier.MenuBar
{
    /// <summary>The identifier split button's commands: the split button itself, one button per colour and icon shape, and "Change identifier...".</summary>
    /// <remarks>
    /// Only the shown button is visible and latched, so the split button's face shows that button's icon and the title.
    /// With no solution open, or "Show solution identifier" off, the split button and all its buttons are hidden.
    /// </remarks>
    internal sealed class MenuBarSplitButton
    {
        // Must match guidExtensionCmdSet and its MenuBarSplitButton and ChangeIdentifierButton in SolutionIdentifierPackage.vsct.
        // The colour-and-shape buttons' IDs come from IdentifierPalette.CalculateColourAndShapeButtonId.
        public static readonly Guid ExtensionCmdSetGuid = new Guid("707578c8-f679-472b-b9db-d03fa17b0d46");
        public const int SplitButtonId = 0x1000;
        public const int ChangeIdentifierButtonId = 0x0200;

        private readonly IdentifierController _controller;

        public MenuBarSplitButton(IdentifierController controller)
        {
            _controller = controller;
        }

        public void RegisterButtons(OleMenuCommandService commandService)
        {
            AddSplitButton(commandService);

            foreach ((IdentifierIconShape iconShape, int colourIndex) in IdentifierPalette.BuildAllColouredIcons())
            {
                AddColourAndShapeButton(commandService, iconShape, colourIndex);
            }

            AddChangeIdentifierButton(commandService);
        }

        private void AddSplitButton(OleMenuCommandService commandService)
        {
            var splitButton = new OleMenuCommand(OnClickOpenDialog, new CommandID(ExtensionCmdSetGuid, SplitButtonId));
            splitButton.BeforeQueryStatus += (sender, e) => UpdateSplitButtonOnQuery((OleMenuCommand)sender);
            commandService.AddCommand(splitButton);
        }

        private void AddColourAndShapeButton(OleMenuCommandService commandService, IdentifierIconShape iconShape, int colourIndex)
        {
            int colourAndShapeButtonId = IdentifierPalette.CalculateColourAndShapeButtonId(iconShape, colourIndex);
            var colourAndShapeButton = new OleMenuCommand(OnClickOpenDialog, new CommandID(ExtensionCmdSetGuid, colourAndShapeButtonId));

            colourAndShapeButton.BeforeQueryStatus += (sender, e) => UpdateColourAndShapeButtonOnQuery((OleMenuCommand)sender, iconShape, colourIndex);
            commandService.AddCommand(colourAndShapeButton);
        }

        private void AddChangeIdentifierButton(OleMenuCommandService commandService)
        {
            var changeIdentifierButton = new OleMenuCommand(OnClickOpenDialog, new CommandID(ExtensionCmdSetGuid, ChangeIdentifierButtonId));
            changeIdentifierButton.BeforeQueryStatus += (sender, e) => UpdateChangeIdentifierButtonOnQuery((OleMenuCommand)sender);
            commandService.AddCommand(changeIdentifierButton);
        }

        private void UpdateSplitButtonOnQuery(OleMenuCommand splitButton)
        {
            splitButton.Visible = _controller.IsIdentifierShown;
        }

        private void UpdateColourAndShapeButtonOnQuery(OleMenuCommand colourAndShapeButton, IdentifierIconShape iconShape, int colourIndex)
        {
            IdentifierAppearance currentIdentifier = _controller.CurrentIdentifier;
            
            // only one will match
            bool isThisButtonShown = _controller.IsIdentifierShown
                && iconShape == currentIdentifier.IconShape
                && colourIndex == currentIdentifier.ColourIndex;
                        
            // always update text to latest
            colourAndShapeButton.Text = _controller.BuildCurrentTitle();
            colourAndShapeButton.Enabled = _controller.IsSolutionOpen;
            colourAndShapeButton.Visible = isThisButtonShown;
            colourAndShapeButton.Checked = isThisButtonShown;
        }

        private void UpdateChangeIdentifierButtonOnQuery(OleMenuCommand changeIdentifierButton)
        {
            changeIdentifierButton.Visible = _controller.IsIdentifierShown;
            changeIdentifierButton.Enabled = _controller.IsSolutionOpen;
        }

        private void OnClickOpenDialog(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _controller.ShowIdentifierDialogAndSaveResult();
        }
    }
}
