using SolutionIdentifier.Appearance;
using SolutionIdentifier.IntegrationTests.Run;
using SolutionIdentifier.IntegrationTests.VisualStudio;
using Xunit;
using Xunit.Abstractions;

namespace SolutionIdentifier.IntegrationTests.Tests
{
    [Trait("Category", "Integration")]
    [Collection(ExperimentalInstanceTestCollection.Name)]
    public sealed class IdentifierButtonTests
    {
        public IdentifierButtonTests(ITestOutputHelper testOutput)
        {
            VisualStudioTestRun.CurrentTestOutput = testOutput;
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierButton_SolutionWithNoSavedIdentifier_ShowsPathWithColourAndShapeFromPath(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile));
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierButton_SolutionClosed_HidesButton(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                string defaultTitle = IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile);
                IdentifierButtonController.WaitAndAssertNameVisibleOnScreen(vs, defaultTitle);
                IdentifierButtonController.WaitAndAssertIdentifierButtonVisible(vs);

                vs.CloseSolution();

                IdentifierButtonController.WaitAndAssertIdentifierButtonHidden(vs);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeNoColourAndShapeButtonShown());
                Assert.False(IdentifierButtonController.IsChangeIdentifierVisible(vs.VsInspector), "Change identifier button is visible with no solution open.");

                // Every button's text becomes "No solution" on closing, so a split button left on screen could show any of these.
                IdentifierButtonController.WaitAndAssertNameNotVisibleOnScreen(vs, defaultTitle);
                IdentifierButtonController.WaitAndAssertNameNotVisibleOnScreen(vs, SolutionAppearance.NoSolutionTitle);
                IdentifierButtonController.WaitAndAssertNameNotVisibleOnScreen(vs, IdentifierButtonText.SplitButtonText);
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierButton_SolutionReopened_ShowsPathAgain(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                vs.CloseSolution();
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeNoColourAndShapeButtonShown());

                vs.OpenSolution();

                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile));
                IdentifierButtonController.WaitAndAssertNameVisibleOnScreen(vs, IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile)); // the hidden split button came back
                Assert.True(IdentifierButtonController.IsChangeIdentifierVisible(vs.VsInspector), "Change identifier is hidden with a solution open.");
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierButton_ShowSolutionIdentifierTurnedOffThenOn_HidesThenShowsButton(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                string shownDefault = IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile);
                string defaultTitle = IdentifierButtonController.ReadDefaultTitle(vs.SolutionFile);
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, shownDefault);
                IdentifierButtonController.WaitAndAssertNameVisibleOnScreen(vs, defaultTitle);
                IdentifierButtonController.WaitAndAssertIdentifierButtonVisible(vs);

                try
                {
                    OptionsController.SetShowSolutionIdentifier(vs, isShown: false);

                    IdentifierButtonController.WaitAndAssertIdentifierButtonHidden(vs);
                    IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeNoColourAndShapeButtonShown());
                    Assert.False(IdentifierButtonController.IsChangeIdentifierVisible(vs.VsInspector), "Change identifier is visible with the identifier turned off.");

                    IdentifierButtonController.WaitAndAssertNameNotVisibleOnScreen(vs, defaultTitle);
                    IdentifierButtonController.WaitAndAssertNameNotVisibleOnScreen(vs, IdentifierButtonText.SplitButtonText);

                    OptionsController.SetShowSolutionIdentifier(vs, isShown: true);

                    IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, shownDefault);
                    IdentifierButtonController.WaitAndAssertNameVisibleOnScreen(vs, defaultTitle);
                }
                finally
                {
                    OptionsController.RestoreDefaults(vs);
                }
            });
        }

        [Theory]
        [VisualStudioVersions]
        public void IdentifierButton_FoldersToShowChanged_TitleChangesWithoutReopening(int visualStudioVersion)
        {
            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), vs =>
            {
                IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultForSolution(vs.SolutionFile));

                try
                {
                    OptionsController.SetFoldersToShow(vs, 0);
                    IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultColouredIconWithTitle(vs.SolutionFile, SolutionPathDisplayText.ShortenPathToLastFolders(vs.SolutionFile, 0)));

                    OptionsController.SetFoldersToShow(vs, 1);
                    IdentifierButtonController.WaitAndAssertDescriptionMatches(vs, IdentifierButtonController.DescribeDefaultColouredIconWithTitle(vs.SolutionFile, SolutionPathDisplayText.ShortenPathToLastFolders(vs.SolutionFile, 1)));
                }
                finally
                {
                    OptionsController.RestoreDefaults(vs);
                }
            });
        }
    }
}
