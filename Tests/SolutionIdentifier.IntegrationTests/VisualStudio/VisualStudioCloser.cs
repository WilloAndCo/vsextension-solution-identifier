using SolutionIdentifier.IntegrationTests.Run;
using SolutionIdentifier.IntegrationTests.Screen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    internal struct CloseHangResult
    {
        public CloseHangResult(string windowsWhenStuck, string stackWhenStuck)
        {
            WindowsWhenStuck = windowsWhenStuck;
            StackWhenStuck = stackWhenStuck;
        }

        public string WindowsWhenStuck { get; }
        public string StackWhenStuck { get; }
    }

    /// <summary>Closes Visual Studio at the end of each test, and kills it if it doesn't close in time.</summary>
    internal static class VisualStudioCloser
    {  
        // Known Visual Studio hangs on close that aren't caused by the extension
        private static readonly IReadOnlyDictionary<string, string> KnownHangs = new Dictionary<string, string>
        {
            // Visual Studio 2022's built-in GitHub Copilot. Read from the cdb debugger from the main thread as casuing the hang.
            ["Microsoft.VisualStudio.Copilot.Common.CopilotJoinableTaskFactoryWrapper.Dispose"] = "GitHub Copilot shutdown",
        };

        private static readonly TimeSpan CloseTimeLimit = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Closes Visual Studio. If it hasn't exited in a set time, it is killed.
        /// Known hangs are reported as warnings, all others are reported as test failures.
        /// </summary>
        public static void CloseOrKillAfterTimeout(Process devenv, EnvDTE80.DTE2 dte, VisualStudioVersionInfo versionInfo, string resultsFolder)
        {
            var screenElements = new ScreenElementsController(devenv.Id);
            CloseHangResult? hangResult = null;

            // Timer callback fires if the using block does not exit in time
            using (var hangTimer = new Timer(
                _ =>
                {
                    // runs on another thread, so can query the hanging thread
                    hangResult = new CloseHangResult(
                        windowsWhenStuck: screenElements.WindowNamesToString(),
                        stackWhenStuck: StackReader.GetMainThreadStackWithDebugger(devenv.Id));
                    KillIgnoringErrors(devenv);
                },
                null,
                CloseTimeLimit,
                Timeout.InfiniteTimeSpan))
            {
                try
                {
                    // Don't save any changes to the solution
                    dte.Solution.Close(SaveFirst: false);
                    dte.Quit();
                }
                catch (COMException)
                {
                    // already closing
                }

                // Wait for exit, timer catches hangs
                devenv.WaitForExit();

                // Synced state across threads to check that the callback isn't mid-diagnosis when the process finally exits             
                using (var sharedHasCallbackFinished = new ManualResetEvent(false))
                {
                    hangTimer.Dispose(sharedHasCallbackFinished); // store reference inside the timer to change when callback is finished
                    sharedHasCallbackFinished.WaitOne(); // blocks until finished is true
                }
            }

            if (hangResult == null)
            {
                // no hang happened
                return;
            }

            string? knownHangName = CheckForKnownHangName(hangResult.Value.StackWhenStuck);
            if (knownHangName != null)
            {
                string warning = $"{versionInfo} didn't close within {CloseTimeLimit.TotalSeconds:0} s and was killed. It was stuck in a known " +
                    $"Visual Studio hang the extension doesn't cause ({knownHangName}). The test's own checks passed or failed as reported.";

                VisualStudioTestRun.CurrentTestOutput?.WriteLine("WARNING: " + warning);
                TestResultsFolder.LogWarning(resultsFolder, warning);

                return;
            }

            throw new TimeoutException(
                $"Visual Studio didn't close within {CloseTimeLimit.TotalSeconds:0} s and was killed. Windows it was showing: {hangResult.Value.WindowsWhenStuck}{Environment.NewLine}" +
                $"Main thread: {hangResult.Value.StackWhenStuck}");
        }

        private static string? CheckForKnownHangName(string hangCallStack)
        {
            foreach (KeyValuePair<string, string> knownHang in KnownHangs)
            {
                if (hangCallStack.Contains(knownHang.Key))
                {
                    return knownHang.Value;
                }
            }

            return null;
        }

        private static void KillIgnoringErrors(Process process)
        {
            try
            {
                if (process.HasExited == false)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // already exited
            }
            catch (Win32Exception)
            {
                // already ending
            }
        }
    }
}
