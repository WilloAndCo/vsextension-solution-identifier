using System.Windows.Automation;
using SolutionIdentifier.IntegrationTests.Run;
using SolutionIdentifier.IntegrationTests.VisualStudio;
using Xunit;
using Xunit.Abstractions;

namespace SolutionIdentifier.IntegrationTests.Tests
{
    [Trait("Category", "UI")]
    [Collection(ExperimentalInstanceTestCollection.Name)]
    public sealed class IdentifierDialogUITests
    {
        public IdentifierDialogUITests(ITestOutputHelper testOutput)
        {
            VisualStudioTestRun.CurrentTestOutput = testOutput;
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierDialog_TitleColourAndShapeChosen_ButtonShowsChoice(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile));

                AutomationElement dialog = IdentifierDialogController.OpenByClickingIdentifierButton(vs, IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile));
                IdentifierDialogController.ChooseAndPressOk(vs, dialog, title: "UI Test", colourName: "Sky", iconShapeName: "Circle");

                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeIdentifierDetails("Sky", "Circle", "UI Test", isEnabled: true));
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierDialog_Cancelled_ButtonUnchanged(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                string unchanged = IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, unchanged);

                AutomationElement dialog = IdentifierDialogController.OpenByClickingIdentifierButton(vs, IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile));
                IdentifierDialogController.ReplaceTitleAndPressCancel(vs, dialog, "Throw away title");

                IdentifierButtonController.WatchAndAssertDescriptionStillMatches(vs, unchanged);
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierDialog_ChoiceSaved_ShownAfterSolutionReopened(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                string chosen = IdentifierButtonController.DescribeIdentifierDetails("Lavender", "Lightning", "Saved Identifier", isEnabled: true);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile));

                AutomationElement dialog = IdentifierDialogController.OpenByClickingIdentifierButton(vs, IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile));
                IdentifierDialogController.ChooseAndPressOk(vs, dialog, title: "Saved Identifier", colourName: "Lavender", iconShapeName: "Lightning");
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, chosen);

                vs.CloseSolution();
                vs.OpenSolution();

                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, chosen);
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierDialog_ResetPressed_ButtonShowsDefaultAfterSolutionReopened(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                string shownDefault = IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, shownDefault);

                AutomationElement dialog = IdentifierDialogController.OpenByClickingIdentifierButton(vs, IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile));
                IdentifierDialogController.ChooseAndPressOk(vs, dialog, title: "Before Reset", colourName: "Butter", iconShapeName: "Star");
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeIdentifierDetails("Butter", "Star", "Before Reset", isEnabled: true));

                dialog = IdentifierDialogController.OpenByClickingIdentifierButton(vs, "Before Reset");
                IdentifierDialogController.PressReset(vs, dialog);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, shownDefault);

                // Check that reset cleared the solution stored data as well and it doesn't reappear on reload
                vs.CloseSolution();
                vs.OpenSolution();

                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, shownDefault);
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierDialog_TitleLeftEmpty_ButtonShowsPathWithChosenColourAndShape(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                string defaultTitle = IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile));

                AutomationElement dialog = IdentifierDialogController.OpenByClickingIdentifierButton(vs, defaultTitle);
                IdentifierDialogController.ChooseAndPressOk(vs, dialog, title: string.Empty, colourName: "Butter", iconShapeName: "Star");

                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeIdentifierDetails("Butter", "Star", defaultTitle, isEnabled: true));
            });
        }
    }
}
