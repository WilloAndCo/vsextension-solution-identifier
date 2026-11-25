using System;
using System.IO;
using System.Linq;

namespace SolutionIdentifier.Appearance
{
    /// <summary>What the menu bar shows for a solution with no title: the end of its file path.</summary>
    internal static class SolutionPathDisplayText
    {
        /// <summary>For C:\Dev\Parent\MySolution.sln - 0 returns MySolution.sln, 1 returns ...\Parent\MySolution.sln, etc.</summary>
        public static string ShortenPathToLastFolders(string solutionPath, int foldersToShow)
        {
            string fileName = Path.GetFileName(solutionPath);
            if (foldersToShow <= 0)
            {
                return fileName;
            }

            string root = Path.GetPathRoot(solutionPath) ?? string.Empty;
            string folderPath = Path.GetDirectoryName(solutionPath) ?? string.Empty;
            string[] folders = folderPath.Substring(Math.Min(root.Length, folderPath.Length))
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            if (folders.Length < foldersToShow)
            {
                return solutionPath;
            }

            return "..." + Path.DirectorySeparatorChar
                + string.Join(Path.DirectorySeparatorChar.ToString(), folders.Skip(folders.Length - foldersToShow))
                + Path.DirectorySeparatorChar + fileName;
        }
    }
}
