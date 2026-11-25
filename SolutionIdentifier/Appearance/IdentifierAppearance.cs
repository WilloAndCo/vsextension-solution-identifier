using System;
using System.Security.Cryptography;
using System.Text;

namespace SolutionIdentifier.Appearance
{
    /// <summary>What the menu bar shows for a solution identifier.</summary>
    internal sealed class IdentifierAppearance
    {
        public IdentifierAppearance(string customTitle, int colourIndex, IdentifierIconShape iconShape)
        {
            CustomTitle = customTitle;
            ColourIndex = colourIndex;
            IconShape = iconShape;
        }

        /// <summary>A custom title for the identifier, if empty use the solution path.</summary>
        public string CustomTitle { get; }

        /// <summary>Position in IdentifierPalette.Colours.</summary>
        public int ColourIndex { get; }
        public IdentifierIconShape IconShape { get; }

        public bool HasCustomTitle => CustomTitle.Length > 0;

        public static IdentifierAppearance CreateDefaultForSolution(string solutionPath)
        {
            int colouredIconIndexFromPath = (int)(CalculatePathHash(solutionPath) % (uint)IdentifierPalette.ColouredIconCount);
            
            int colourIndex = IdentifierPalette.CalculateColourIndexFromColouredIconIndex(colouredIconIndexFromPath);
            IdentifierIconShape iconShape = IdentifierPalette.CalculateIconShapeFromColouredIconIndex(colouredIconIndexFromPath);

            return new IdentifierAppearance(customTitle: string.Empty, colourIndex, iconShape);
        }

        /// <summary>The custom title, or the end of the solution's path when there is no custom title.</summary>
        public string BuildTitleToShow(string solutionPath, int foldersToShow)
        {
            if (HasCustomTitle)
            {
                return CustomTitle;
            }

            return SolutionPathDisplayText.ShortenPathToLastFolders(solutionPath, foldersToShow);
        }

        private static uint CalculatePathHash(string solutionPath)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] pathHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(solutionPath.ToUpperInvariant()));
            return BitConverter.ToUInt32(pathHash, 0);
        }
    }
}
