using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using SolutionIdentifier.IntegrationTests.Screen;
using SolutionIdentifier.IntegrationTests.VisualStudio;

namespace SolutionIdentifier.IntegrationTests.Tests
{
    /// <summary>Thhe identifier dialog's window title and AutomationIds. Must match IdentifierDialog.xaml.</summary>
    internal static class IdentifierDialogNames
    {
        public const string DialogName = "Solution identifier";
        public const string TitleTextBoxId = "TitleTextBox";
        public const string OkButtonId = "OkButton";
        public const string CancelButtonId = "CancelButton";
        public const string ResetButtonId = "ResetButton";
    }

    /// <summary>Interact with Identifier Dialog using the mouse and UI Automation.</summary>
    internal static class IdentifierDialogController
    {
        public static AutomationElement OpenByClickingIdentifierButton(VisualStudioSession vs, string currentTitle)
        {
            ScreenElementsController.BringWindowToFront(vs.MainWindowHandle);
            AutomationElement identifierMenuButton = GetIdentifierMenuButton(vs, currentTitle);

            // Note - avoid clicking the drop down button on the right side
            Rect bounds = ScreenElementsController.GetBoundsInPixels(identifierMenuButton);
            var iconCentre = new Point(bounds.Left + (bounds.Height / 2), bounds.Top + (bounds.Height / 2));
            MouseController.ThrowIfPointIsNotOverProcessWindow(iconCentre, vs.ProcessId, "the identifier button");
            MouseController.LeftClick(iconCentre);

            return vs.ScreenElementsController.GetWindow(IdentifierDialogNames.DialogName, Wait.WaitToHappenDuration);
        }

        public static void ChooseAndPressOk(VisualStudioSession vs, AutomationElement dialog, string title, string colourName, string iconShapeName)
        {
            ScreenElementsController.ReplaceTextBoxText(ScreenElementsController.GetDescendantByAutomationId(dialog, IdentifierDialogNames.TitleTextBoxId, Wait.WaitToHappenDuration), title);
            ScreenElementsController.SelectListItem(ScreenElementsController.GetDescendantByNameAndType(dialog, colourName, ControlType.ListItem, Wait.WaitToHappenDuration));
            ScreenElementsController.SelectListItem(ScreenElementsController.GetDescendantByNameAndType(dialog, iconShapeName, ControlType.ListItem, Wait.WaitToHappenDuration));
            ScreenElementsController.PressButton(ScreenElementsController.GetDescendantByAutomationId(dialog, IdentifierDialogNames.OkButtonId, Wait.WaitToHappenDuration));
            WaitForDialogToClose(vs);
        }

        public static void ReplaceTitleAndPressCancel(VisualStudioSession vs, AutomationElement dialog, string title)
        {
            ScreenElementsController.ReplaceTextBoxText(ScreenElementsController.GetDescendantByAutomationId(dialog, IdentifierDialogNames.TitleTextBoxId, Wait.WaitToHappenDuration), title);
            ScreenElementsController.PressButton(ScreenElementsController.GetDescendantByAutomationId(dialog, IdentifierDialogNames.CancelButtonId, Wait.WaitToHappenDuration));
            WaitForDialogToClose(vs);
        }

        public static void PressReset(VisualStudioSession vs, AutomationElement dialog)
        {
            ScreenElementsController.PressButton(ScreenElementsController.GetDescendantByAutomationId(dialog, IdentifierDialogNames.ResetButtonId,Wait.WaitToHappenDuration));
            WaitForDialogToClose(vs);
        }

        private static void WaitForDialogToClose(VisualStudioSession vs)
        {
            Wait.PollUntilTrueOrThrow(() => vs.ScreenElementsController.CannotFindWindow(IdentifierDialogNames.DialogName), Wait.WaitToHappenDuration, () => "the identifier dialog to close");
        }

        /// <summary>The identifier's split button on the Menu Bar, found by its title.</summary>
        private static AutomationElement GetIdentifierMenuButton(VisualStudioSession vs, string currentTitle)
        {
            IReadOnlyList<AutomationElement> allElementsWithName = new List<AutomationElement>();

            Wait.PollUntilTrueOrThrow(
                () => (allElementsWithName = vs.ScreenElementsController.FindVisibleElementsNamed(currentTitle) ?? new List<AutomationElement>()).Count > 0,
                Wait.WaitToHappenDuration,
                () => $"an element named '{currentTitle}' on screen");

            // Only menu items count, so another element with the same name can't be mistaken for the button.
            List<AutomationElement> menuItems = allElementsWithName.Where(ScreenElementsController.IsMenuItem).ToList();
            if (menuItems.Count != 1)
            {
                throw new InvalidOperationException(
                $"Expected 1 menu item named '{currentTitle}' but found {menuItems.Count}. Elements with that name: {ScreenElementsController.ElementsToString(allElementsWithName)}");
            }

            return menuItems[0];
        }
    }
}
