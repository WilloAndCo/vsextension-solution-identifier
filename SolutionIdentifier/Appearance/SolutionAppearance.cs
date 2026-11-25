#nullable enable

namespace SolutionIdentifier.Appearance
{
    /// <summary>The open solution's identifier</summary>
    internal sealed class SolutionAppearance
    {
        private const string NoSolutionColourName = "Cloud";
        public const string NoSolutionTitle = "No solution";

        /// <summary>Used while no solution is open, when the split button is hidden.</summary>
        public static readonly SolutionAppearance NoSolutionAppearance = new SolutionAppearance(
            solutionPath: null,
            new IdentifierAppearance(customTitle: string.Empty, IdentifierPalette.ColourIndexFromString(NoSolutionColourName) ?? 0, IdentifierIconShape.Square));

        public static SolutionAppearance CreateForSolution(string solutionPath, IdentifierAppearance appearance)
        {
            return new SolutionAppearance(solutionPath, appearance);
        }

        public string? SolutionPath { get; }
        public IdentifierAppearance Appearance { get; }
        public bool IsSolutionOpen => SolutionPath != null;

        public string BuildTitleToShow(int foldersToShow)
        {
            if (SolutionPath == null)
            {
                return NoSolutionTitle;
            }

            return Appearance.BuildTitleToShow(SolutionPath, foldersToShow);
        }

        private SolutionAppearance(string? solutionPath, IdentifierAppearance appearance)
        {
            SolutionPath = solutionPath;
            Appearance = appearance;
        }
    }
}
