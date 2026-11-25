using SolutionIdentifier.Appearance;
using Xunit;

namespace SolutionIdentifier.UnitTests
{
    [Trait("Category", "Unit")]
    public sealed class SolutionAppearanceTests
    {
        private const string SolutionPath = @"E:\Work\Dev\Repo\Repo.sln";

        [Fact]
        public void IsSolutionOpen_NoSolution_ReturnsFalse()
        {
            Assert.False(SolutionAppearance.NoSolutionAppearance.IsSolutionOpen);
            Assert.Null(SolutionAppearance.NoSolutionAppearance.SolutionPath);
        }

        [Fact]
        public void Appearance_NoSolution_IsCloudSquareWithNoCustomTitle()
        {
            IdentifierAppearance appearance = SolutionAppearance.NoSolutionAppearance.Appearance;

            // expected hardcoded default
            Assert.Equal("Cloud", IdentifierPalette.Colours[appearance.ColourIndex].Name);

            // expected hardcoded default
            Assert.Equal(IdentifierIconShape.Square, appearance.IconShape);

            Assert.False(appearance.HasCustomTitle);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void BuildTitleToShow_NoSolution_ReturnsNoSolutionTitle(int foldersToShow)
        {
            // expected hardcoded default
            Assert.Equal("No solution", SolutionAppearance.NoSolutionAppearance.BuildTitleToShow(foldersToShow));
        }

        [Fact]
        public void CreateForSolution_PathAndAppearanceGiven_ReturnsOpenSolutionWithBoth()
        {
            var appearance = new IdentifierAppearance("My title", colourIndex: 0, IdentifierIconShape.Square);

            SolutionAppearance solutionAppearance = SolutionAppearance.CreateForSolution(SolutionPath, appearance);

            Assert.True(solutionAppearance.IsSolutionOpen);
            Assert.Equal(SolutionPath, solutionAppearance.SolutionPath);
            Assert.Same(appearance, solutionAppearance.Appearance);
        }

        [Fact]
        public void BuildTitleToShow_CustomTitleEntered_ReturnsCustomTitle()
        {
            var appearance = new IdentifierAppearance("My title", colourIndex: 0, IdentifierIconShape.Square);

            SolutionAppearance solutionAppearance = SolutionAppearance.CreateForSolution(SolutionPath, appearance);

            Assert.Equal("My title", solutionAppearance.BuildTitleToShow(foldersToShow: 1));
        }

        [Fact]
        public void BuildTitleToShow_FoldersToShowChanged_ReturnsTitleForNewFolderCount()
        {
            var appearance = new IdentifierAppearance(customTitle: string.Empty, colourIndex: 0, IdentifierIconShape.Square);

            SolutionAppearance solutionAppearance = SolutionAppearance.CreateForSolution(SolutionPath, appearance);

            Assert.Equal("Repo.sln", solutionAppearance.BuildTitleToShow(foldersToShow: 0));
            Assert.Equal(@"...\Repo\Repo.sln", solutionAppearance.BuildTitleToShow(foldersToShow: 1));
        }
    }
}
