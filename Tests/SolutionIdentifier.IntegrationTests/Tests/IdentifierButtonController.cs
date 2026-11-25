using System.Collections.Generic;
using SolutionIdentifier.Appearance;
using SolutionIdentifier.IntegrationTests.Com;
using SolutionIdentifier.IntegrationTests.VisualStudio;
using SolutionIdentifier.MenuBar;
using SolutionIdentifier.Options;
using Xunit;

namespace SolutionIdentifier.IntegrationTests.Tests
{
    internal static class IdentifierButtonText
    {
        /// <summary>Must match the split button's ButtonText in SolutionIdentifierPackage.vsct, which C# can't read.</summary>
        public const string SplitButtonText = "My Solution Identifier";
    }

    /// <summary>OLECMDF flag values.</summary>
    internal static class OleCommandFlags
    {
        public const int SupportedFlag = 0x1;
        public const int EnabledFlag = 0x2;
        public const int LatchedFlag = 0x4;
        public const int InvisibleFlag = 0x10;
    }

    /// <summary>
    /// Reads the Identifier Menu Bar Button's state through the inspector extension and UI automation.
    /// </summary>
    internal static class IdentifierButtonController
    {
        // The inspector takes the command set GUID as a string
        private static readonly string ExtensionCmdSetGuidString = MenuBarSplitButton.ExtensionCmdSetGuid.ToString();

        private const string NoColourAndShapeButtonShownDescription = "(no colour-and-shape button shown)";

        public static string DescribeIdentifierDetails(string colourName, string iconShapeName, string title, bool isEnabled) =>
            $"{colourName} {iconShapeName} '{title}' {(isEnabled ? "enabled" : "disabled")}";

        /// <summary>What the button shows, enabled, for a solution with no saved identifier.</summary>
        public static string DescribeDefaultForSolution(string solutionFile) =>
            DescribeDefaultColouredIconWithTitle(solutionFile, ReadDefaultTitle(solutionFile));

        /// <summary>The colour and shape the extension picks from the solution's path</summary>
        public static string DescribeDefaultColouredIconWithTitle(string solutionFile, string title)
        {
            IdentifierAppearance defaultAppearance = IdentifierAppearance.CreateDefaultForSolution(solutionFile);
            return DescribeIdentifierDetails(
                IdentifierPalette.Colours[defaultAppearance.ColourIndex].Name,
                defaultAppearance.IconShape.ToString(),
                title,
                isEnabled: true);
        }

        public static string DescribeNoColourAndShapeButtonShown() => NoColourAndShapeButtonShownDescription;

        public static string ReadDefaultTitle(string solutionFile) =>
            SolutionPathDisplayText.ShortenPathToLastFolders(solutionFile, SolutionIdentifierOptionsPage.DefaultFoldersToShow);

        public static void WaitAndAssertNameVisibleOnScreen(VisualStudioSession vs, string name)
        {
            bool isShown = Wait.ReadUntilMatchOrTimeout(() => vs.ScreenElementsController.FindVisibleElementsNamed(name)?.Count > 0, true, Wait.WaitToHappenDuration);
            Assert.True(isShown, $"No element named '{name}' is on screen.");
        }

        public static void WaitAndAssertNameNotVisibleOnScreen(VisualStudioSession vs, string name)
        {
            bool isNotShown = Wait.ReadUntilMatchOrTimeout(() => vs.ScreenElementsController.FindVisibleElementsNamed(name)?.Count == 0, true, Wait.WaitToHappenDuration);
            Assert.True(isNotShown, $"An element named '{name}' is still on screen, or the screen couldn't be read.");
        }

        public static void WaitAndAssertIdentifierButtonVisible(VisualStudioSession vs)
        {
            bool isVisible = Wait.ReadUntilMatchOrTimeout(() => IsIdentifierButtonVisible(vs.VsInspector), true, Wait.WaitToHappenDuration);
            Assert.True(isVisible, "The button's own command isn't supported and visible.");
        }

        public static void WaitAndAssertIdentifierButtonHidden(VisualStudioSession vs)
        {
            bool isHidden = Wait.ReadUntilMatchOrTimeout(() => IsIdentifierButtonHidden(vs.VsInspector), true, Wait.WaitToHappenDuration);
            Assert.True(isHidden, "The button's own command isn't supported and invisible.");
        }

        // Both check Supported, so neither passes if the extension has no handler for the split button.
        private static bool IsIdentifierButtonVisible(VsInspectorClient inspector)
        {
            int flags = inspector.ReadButtonFlags(ExtensionCmdSetGuidString, MenuBarSplitButton.SplitButtonId);
            return (flags & OleCommandFlags.SupportedFlag) != 0 && (flags & OleCommandFlags.InvisibleFlag) == 0;
        }

        private static bool IsIdentifierButtonHidden(VsInspectorClient inspector)
        {
            int flags = inspector.ReadButtonFlags(ExtensionCmdSetGuidString, MenuBarSplitButton.SplitButtonId);
            return (flags & OleCommandFlags.SupportedFlag) != 0 && (flags & OleCommandFlags.InvisibleFlag) != 0;
        }

        public static bool IsChangeIdentifierVisible(VsInspectorClient inspector) =>
            (inspector.ReadButtonFlags(ExtensionCmdSetGuidString, MenuBarSplitButton.ChangeIdentifierButtonId) & OleCommandFlags.InvisibleFlag) == 0;

        public static void WaitAndAssertDescriptionMatches(VisualStudioSession vs, string expectedDescription)
        {
            string shown = Wait.ReadUntilMatchOrTimeout(() => ReadVisibleButtonDescriptions(vs.VsInspector), expectedDescription, Wait.WaitToHappenDuration);
            Assert.Equal(expectedDescription, shown);
        }

        public static void WatchAndAssertDescriptionStillMatches(VisualStudioSession vs, string expectedDescription)
        {
            string shown = Wait.ReadUntilChangedOrTimeout(() => ReadVisibleButtonDescriptions(vs.VsInspector), expectedDescription, Wait.CheckNothingChangesDuration);
            Assert.Equal(expectedDescription, shown);
        }

        /// <summary>
        /// Describes the colour-and-shape button that is visible and latched. When several are, lists them all.
        /// The expected behaviour is that only one is visisble - returning multiple will fail the tests.
        /// </summary>
        public static string ReadVisibleButtonDescriptions(VsInspectorClient inspector)
        {
            var visibleAndLatchedDescriptions = new List<string>();
            foreach (IdentifierIconShape iconShape in IdentifierPalette.IconShapes)
            {
                for (int colourIndex = 0; colourIndex < IdentifierPalette.Colours.Length; colourIndex++)
                {
                    int colourAndShapeButtonId = IdentifierPalette.CalculateColourAndShapeButtonId(iconShape, colourIndex);
                    int flags = inspector.ReadButtonFlags(ExtensionCmdSetGuidString, colourAndShapeButtonId);
                    if (IsVisibleAndLatched(flags) == false)
                    {
                        continue;
                    }

                    string title = inspector.ReadButtonText(ExtensionCmdSetGuidString, colourAndShapeButtonId);
                    visibleAndLatchedDescriptions.Add(DescribeIdentifierDetails(IdentifierPalette.Colours[colourIndex].Name, iconShape.ToString(), title, isEnabled: (flags & OleCommandFlags.EnabledFlag) != 0));
                }
            }

            if (visibleAndLatchedDescriptions.Count == 0)
            {
                return NoColourAndShapeButtonShownDescription;
            }
            else if (visibleAndLatchedDescriptions.Count == 1)
            {
                return visibleAndLatchedDescriptions[0];
            }
            else
            {
                return $"({visibleAndLatchedDescriptions.Count} visible and latched buttons: {string.Join(", ", visibleAndLatchedDescriptions)})";
            }

        }

        // SupportedFlag = is registered, InvisibleFlag = is not visible, LatchedFlag = on/off button is on
        private static bool IsVisibleAndLatched(int flags) =>
            (flags & OleCommandFlags.SupportedFlag) != 0 && (flags & OleCommandFlags.InvisibleFlag) == 0 && (flags & OleCommandFlags.LatchedFlag) != 0;
    }
}
