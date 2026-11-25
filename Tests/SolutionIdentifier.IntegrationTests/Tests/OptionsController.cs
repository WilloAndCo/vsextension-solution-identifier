using System;
using SolutionIdentifier.IntegrationTests.VisualStudio;
using SolutionIdentifier.Options;

namespace SolutionIdentifier.IntegrationTests.Tests
{
    /// <summary>
    /// Changes the extension's Tools > Options settings using DTE.Properties
    /// Available due to "supportsAutomation: true" on SolutionIdentifierPackage
    /// </summary>
    internal static class OptionsController
    {
        public static void SetShowSolutionIdentifier(VisualStudioSession vs, bool isShown)
        {
            SetPageProperty(vs, nameof(SolutionIdentifierOptionsPage.ShowSolutionIdentifier), isShown);
        }

        public static void SetFoldersToShow(VisualStudioSession vs, int foldersToShow)
        {
            SetPageProperty(vs, nameof(SolutionIdentifierOptionsPage.FoldersToShow), foldersToShow);
        }

        public static void RestoreDefaults(VisualStudioSession vs)
        {
            SetShowSolutionIdentifier(vs, SolutionIdentifierOptionsPage.DefaultShowSolutionIdentifier);
            SetFoldersToShow(vs, SolutionIdentifierOptionsPage.DefaultFoldersToShow);
        }

        private static void SetPageProperty(VisualStudioSession vs, string propertyName, object value)
        {            
            EnvDTE.Properties? page = vs.Dte.get_Properties(SolutionIdentifierOptionsPage.OptionsEntryName, SolutionIdentifierOptionsPage.OptionsSubPageName);
            if (page == null)
            {
                throw new InvalidOperationException($"Tools > Options > {SolutionIdentifierOptionsPage.OptionsEntryName} > {SolutionIdentifierOptionsPage.OptionsSubPageName} isn't available through DTE.Properties. Is the extension loaded?");
            }

            page.Item(propertyName).Value = value;

            // read after setting to validate it worked
            object readValue = page.Item(propertyName).Value;
            if (Equals(readValue, value) == false)
            {
                throw new InvalidOperationException($"Set {propertyName} to {value} through DTE, but Visual Studio reads it back as {readValue}.");
            }
        }
    }
}
