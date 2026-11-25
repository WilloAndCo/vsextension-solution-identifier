#nullable enable
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionIdentifier.Appearance;

namespace SolutionIdentifier.Storage
{
    /// <summary>The contents of one identifier file.</summary>
    [DataContract]
    internal sealed class IdentifierFileContents
    {
        [DataMember(Name = "solutionPath", Order = 1)]
        public string? SolutionPath { get; set; }

        [DataMember(Name = "title", Order = 2)]
        public string? Title { get; set; }

        [DataMember(Name = "colour", Order = 3)]
        public string? Colour { get; set; }

        [DataMember(Name = "iconShape", Order = 4)]
        public string? IconShape { get; set; }
    }

    /// <summary>
    /// Saves each solution's identifier as one file in %LOCALAPPDATA%\SolutionIdentifier, named by a hash of the solution path.
    /// Shared by every supported vs version.
    /// </summary>
    internal sealed class IdentifierStore
    {
        private const string FolderName = "SolutionIdentifier";
        private const string FileExtension = ".json";

        private static readonly DataContractJsonSerializer Serialiser = new DataContractJsonSerializer(typeof(IdentifierFileContents));

        /// <summary>The folder that holds every solution's identifier file.</summary>
        public static string AppDataFolderPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

        /// <summary>The solution's json file for storage.</summary>
        public static string CalculateJsonFilePath(string solutionPath)
        {
            return Path.Combine(AppDataFolderPath, HashSolutionPath(solutionPath) + FileExtension);
        }

        /// <summary>The saved identifier or the default when none is saved or the file can't be read.</summary>
        public IdentifierAppearance ReadIdentifierOrDefault(string solutionPath)
        {
            IdentifierAppearance defaultIdentifier = IdentifierAppearance.CreateDefaultForSolution(solutionPath);

            IdentifierFileContents? fileContents = ReadFileContentsIgnoringErrors(CalculateJsonFilePath(solutionPath));
            if (fileContents == null)
            {
                // nothing saved for this solution, or the file couldn't be read
                return defaultIdentifier;
            }

            string customTitle = fileContents.Title ?? defaultIdentifier.CustomTitle;
            int colourIndex = IdentifierPalette.ColourIndexFromString(fileContents.Colour ?? string.Empty) ?? defaultIdentifier.ColourIndex;
            IdentifierIconShape iconShape = IdentifierPalette.IconShapeFromString(fileContents.IconShape ?? string.Empty) ?? defaultIdentifier.IconShape;

            return new IdentifierAppearance(customTitle, colourIndex, iconShape);
        }

        public void SaveIdentifier(string solutionPath, IdentifierAppearance identifier)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string filePath = CalculateJsonFilePath(solutionPath);
            var fileContents = new IdentifierFileContents
            {
                SolutionPath = solutionPath,
                Title = identifier.CustomTitle,
                Colour = IdentifierPalette.Colours[identifier.ColourIndex].Name,
                IconShape = identifier.IconShape.ToString(),
            };

            try
            {
                Directory.CreateDirectory(AppDataFolderPath);
                using FileStream stream = File.Create(filePath);
                Serialiser.WriteObject(stream, fileContents);
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                ShowCouldNotUpdateError(filePath, ex);
            }
        }

        public void DeleteIdentifier(string solutionPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string filePath = CalculateJsonFilePath(solutionPath);
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                ShowCouldNotUpdateError(filePath, ex);
            }
        }

        public void DeleteAllIdentifiers()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string folderPath = AppDataFolderPath;
            try
            {
                if (Directory.Exists(folderPath) == false)
                {
                    return;
                }

                foreach (string filePath in Directory.GetFiles(folderPath, "*" + FileExtension))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                ShowCouldNotUpdateError(folderPath, ex);
            }
        }

        private static IdentifierFileContents? ReadFileContentsIgnoringErrors(string filePath)
        {
            try
            {
                if (File.Exists(filePath) == false)
                {
                    return null;
                }

                using FileStream stream = File.OpenRead(filePath);
                return (IdentifierFileContents?)Serialiser.ReadObject(stream);
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                return null;
            }
        }

        /// <summary>True for the exceptions that reading, writing or deleting the files can throw.</summary>
        private static bool IsExpectedFileException(Exception ex) =>
            ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is SerializationException;

        /// <summary>Popup box if saves couldn't update</summary>
        private static void ShowCouldNotUpdateError(string path, Exception ex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, ex.Message, $"Solution Identifier: could not update {path}",
                OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static string HashSolutionPath(string solutionPath)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] pathHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(solutionPath.ToUpperInvariant()));
            return BitConverter.ToString(pathHash);
        }
    }
}
