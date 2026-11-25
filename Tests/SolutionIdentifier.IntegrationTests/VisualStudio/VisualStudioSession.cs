using System;
using System.IO;
using SolutionIdentifier.IntegrationTests.Com;
using SolutionIdentifier.IntegrationTests.Screen;
using SolutionIdentifier.Storage;

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    /// <summary>Properties for one Visual Studio launch.</summary>
    internal sealed class VisualStudioSession
    {
        public VisualStudioSession(VisualStudioVersionInfo versionInfo, EnvDTE80.DTE2 dte, int processId, string solutionDirectory, string solutionFile)
        {
            VersionInfo = versionInfo;
            Dte = dte;
            ProcessId = processId;
            SolutionDirectory = solutionDirectory;
            SolutionFile = solutionFile;
            ScreenElementsController = new ScreenElementsController(processId);
            VsInspector = new VsInspectorClient(dte);
        }

        public VisualStudioVersionInfo VersionInfo { get; }

        public EnvDTE80.DTE2 Dte { get; }

        /// <summary>devenv.exe's process ID.</summary>
        public int ProcessId { get; }

        /// <summary>The copy of the sample solution's folder that this session opened.</summary>
        public string SolutionDirectory { get; }

        /// <summary>The copied .sln file.</summary>
        public string SolutionFile { get; }

        /// <summary>Visual Studio's windows, buttons, and menus, through Windows UI Automation.</summary>
        public ScreenElementsController ScreenElementsController { get; }

        public VsInspectorClient VsInspector { get; }

        public IntPtr MainWindowHandle => (IntPtr)Dte.MainWindow.HWnd;

        /// <summary>Waits until the solution is open, then loads the extensions if Visual Studio hasn't already.</summary>
        public void WaitForSolutionAndLoadExtension()
        {
            WaitForSolutionToOpen();

            // loads both extensions (if not loaded already)
            // VsInspector loads on the first DTE.GetObject call made inside VsInspectorClient due to the [ProvideAutomationObject] attribute.
            // Solution Identifier loads because LoadPackageIfNotLoaded calls IVsShell.LoadPackage when the package isn't loaded yet.
            Wait.PollUntilTrueOrThrow(() => VsInspector.LoadPackageIfNotLoaded(SolutionIdentifierPackage.PackageGuidString), TimeSpan.FromSeconds(20), () => "Solution Identifier to load");
        }

        /// <summary>Deletes the identifier an earlier test saved for this session's solution.</summary>
        public void DeleteSavedIdentifiersAndReopenIfAny()
        {
            string identifierFilePath = IdentifierStore.CalculateJsonFilePath(SolutionFile);
            if (File.Exists(identifierFilePath) == false)
            {
                // nothing saved
                return;
            }

            File.Delete(identifierFilePath);

            // The file is deleted directly, not through the extension
            // Force a reload of the solution here so it correctly picks up the latest identifier data (should now be empty)
            CloseSolution();
            OpenSolution();
        }

        public void CloseSolution()
        {
            Dte.Solution.Close(SaveFirst: false);
            Wait.PollUntilTrueOrThrow(
                () => Dte.Solution.IsOpen == false, 
                Wait.WaitToHappenDuration, 
                () => "the solution to close");
        }

        public void OpenSolution()
        {
            Dte.Solution.Open(SolutionFile);
            WaitForSolutionToOpen();
        }

        private void WaitForSolutionToOpen()
        {
            Wait.PollUntilTrueOrThrow(
                () => Dte.Solution.IsOpen && Dte.Solution.Projects.Count > 0,
                TimeSpan.FromMinutes(3),
                () => $"the solution to open (IsOpen = {Dte.Solution.IsOpen})");
        }
    }
}
