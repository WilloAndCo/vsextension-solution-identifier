using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    /// <summary>
    /// Visual Studio's activity log for one test (devenv must be started with /log). After each test, the test fails if the
    /// log has errors that mention the extension.
    /// </summary>
    internal static class ActivityLog
    {
        // "SolutionIdentifier" also matches the inspector SolutionIdentifier.VisualStudioInspector.
        private static readonly string[] ExtensionIdentifiers =
        {
            "SolutionIdentifier",
            SolutionIdentifierPackage.PackageGuidString,
        };

        /// <summary>All error entries that mention the extension.</summary>
        public static IReadOnlyList<string> AllLoggedExtensionErrors(string logFile)
        {
            // Visual Studio finishes writing the log as it exits.
            Wait.PollUntilTrueOrThrow(() => File.Exists(logFile), TimeSpan.FromSeconds(30), () => $"the ActivityLog {logFile}");

            XDocument log = LoadLogWithRetry(logFile);
            return log.Root.Elements("entry")
                .Where(IsErrorEntry)
                .Select(FormatEntryAsText)
                .Where(MentionsExtension)
                .ToList();
        }

        /// <summary> Log entry has type "Error" </summary>
        private static bool IsErrorEntry(XElement entry) =>
            string.Equals((string)entry.Element("type"), "Error", StringComparison.OrdinalIgnoreCase);

        /// <summary>"source: description errorinfo" includes the parts of an entry that can name the extension.</summary>
        private static string FormatEntryAsText(XElement entry) =>
            $"{(string)entry.Element("source")}: {(string)entry.Element("description")} {(string)entry.Element("errorinfo")}";

        /// <summary> Are any of the extension identifiers mentioned </summary>
        private static bool MentionsExtension(string entryText) =>
            ExtensionIdentifiers.Any(identifier => entryText.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0);

        private static XDocument LoadLogWithRetry(string logFile)
        {
            Exception? lastFailure = null;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    return XDocument.Load(logFile);
                }
                catch (Exception ex) when (ex is IOException || ex is XmlException)
                {
                    // unavailable
                    lastFailure = ex;
                    Thread.Sleep(500);
                }
            }

            throw new InvalidOperationException($"Couldn't read the ActivityLog {logFile}.", lastFailure);
        }
    }
}
