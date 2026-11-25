using System;
using System.Linq;
using SolutionIdentifier.Appearance;
using Xunit;

namespace SolutionIdentifier.UnitTests
{
    [Trait("Category", "Unit")]
    public sealed class IdentifierAppearanceTests
    {
        private const string AlphaSolutionPath = @"C:\Repos\Alpha\Alpha.sln";

        [Fact]
        public void CreateDefaultForSolution_SamePathTwice_ReturnsSameColouredIcon()
        {
            IdentifierAppearance first = IdentifierAppearance.CreateDefaultForSolution(AlphaSolutionPath);
            IdentifierAppearance second = IdentifierAppearance.CreateDefaultForSolution(AlphaSolutionPath);
            Assert.Equal(first.ColourIndex, second.ColourIndex);
            Assert.Equal(first.IconShape, second.IconShape);
        }

        [Fact]
        public void CreateDefaultForSolution_PathInDifferentCase_ReturnsSameColouredIcon()
        {
            IdentifierAppearance mixedCase = IdentifierAppearance.CreateDefaultForSolution(AlphaSolutionPath);
            IdentifierAppearance lowerCase = IdentifierAppearance.CreateDefaultForSolution(AlphaSolutionPath.ToLowerInvariant());
            Assert.Equal(mixedCase.ColourIndex, lowerCase.ColourIndex);
            Assert.Equal(mixedCase.IconShape, lowerCase.IconShape);
        }

        [Fact]
        public void CreateDefaultForSolution_AnyPath_HasNoCustomTitle()
        {
            IdentifierAppearance identifier = IdentifierAppearance.CreateDefaultForSolution(AlphaSolutionPath);
            Assert.Equal(string.Empty, identifier.CustomTitle);
            Assert.False(identifier.HasCustomTitle);
        }

        // Calculated from the hash of the paths. These should always stay constant of the hash is consistent.
        [Theory]
        [InlineData(AlphaSolutionPath, "Sand", "Star")]
        [InlineData(@"C:\Repos\Gamma\Gamma.sln", "Peach", "Diamond")]
        public void CreateDefaultForSolution_KnownPath_ReturnsSameColouredIconAsWhenWritten(string solutionPath, string expectedColourName, string expectedIconShapeName)
        {
            IdentifierAppearance identifier = IdentifierAppearance.CreateDefaultForSolution(solutionPath);
            Assert.Equal(expectedColourName, IdentifierPalette.Colours[identifier.ColourIndex].Name);
            Assert.Equal(expectedIconShapeName, identifier.IconShape.ToString());
        }

        [Fact]
        public void CreateDefaultForSolution_ManyFoldersNamedAfterTheirSolution_ReachesEveryColouredIcon()
        {
            int colouredIconsReached = CountDistinctColouredIconsForPaths(number => $@"C:\Repos\Project{number}\Project{number}.sln", 1000);
            
            // expect to reach all available
            Assert.Equal(IdentifierPalette.ColouredIconCount, colouredIconsReached);
        }

        [Fact]
        public void CreateDefaultForSolution_ManyFoldersWithOneSolutionName_ReachesEveryColouredIcon()
        {
            int colouredIconsReached = CountDistinctColouredIconsForPaths(number => $@"C:\Repos\Project{number}\App.sln", 1000);

            // expect to reach all available
            Assert.Equal(IdentifierPalette.ColouredIconCount, colouredIconsReached);
        }

        [Fact]
        public void HasCustomTitle_CustomTitleEntered_ReturnsTrue()
        {
            var identifier = new IdentifierAppearance("My title", colourIndex: 0, IdentifierIconShape.Square);
            Assert.True(identifier.HasCustomTitle);
        }

        [Fact]
        public void BuildTitleToShow_CustomTitleEntered_ReturnsCustomTitle()
        {
            var identifier = new IdentifierAppearance("My title", colourIndex: 0, IdentifierIconShape.Square);
            Assert.Equal("My title", identifier.BuildTitleToShow(AlphaSolutionPath, foldersToShow: 1));
        }

        [Theory]
        [InlineData(0, "Alpha.sln")]
        [InlineData(1, @"...\Alpha\Alpha.sln")]
        public void BuildTitleToShow_NoCustomTitle_ReturnsEndOfPath(int foldersToShow, string expected)
        {
            var identifier = new IdentifierAppearance(customTitle: string.Empty, colourIndex: 0, IdentifierIconShape.Square);
            Assert.Equal(expected, identifier.BuildTitleToShow(AlphaSolutionPath, foldersToShow));
        }

        /// <summary>Counts how many distinct colour and shape combinations were found in numTestPathCount.</summary>
        private static int CountDistinctColouredIconsForPaths(Func<int, string> buildSolutionPath, int numTestPathCount = 1000)
        {          
            return Enumerable.Range(1, numTestPathCount)
                .Select(number => IdentifierAppearance.CreateDefaultForSolution(buildSolutionPath(number)))
                .Select(identifier => IdentifierPalette.CalculateColouredIconIndex(identifier.IconShape, identifier.ColourIndex))
                .Distinct()
                .Count();
        }
    }
}
