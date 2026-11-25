using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SolutionIdentifier.IntegrationTests.Com;
using SolutionIdentifier.IntegrationTests.VisualStudio;
using SolutionIdentifier.Storage;
using Xunit.Abstractions;

namespace SolutionIdentifier.IntegrationTests.Run
{
    /// <summary>
    /// Every test uses this to run its code in Visual Studio's Exp instance, on a copy of a sample solution.
    /// All failures are logged in the results folder, the test throws at the end with these failures
    /// </summary>
    internal static class VisualStudioTestRun
    {
        /// <summary>The running test's output in Test Explorer. Use to report warnings and errors during the test run.<</summary>
        public static ITestOutputHelper? CurrentTestOutput { get; set; }

        /// <summary>Runs a test inside a running Visual Studio Exp instance.</summary>
        public static void RunTestInVisualStudio(VisualStudioTestSetup setup, Action<VisualStudioSession> test, [CallerMemberName] string testName = "")
        {
            // create a place for the test results
            string resultsFolder = TestResultsFolder.CreateResultsFolder(testName, setup.VisualStudioVersion);
            string logFile = TestResultsFolder.GetActivityLogFile(resultsFolder);

            // prepare visual studio settings for the run
            VisualStudioVersionInfo versionInfo = VisualStudioVersionFinder.FindInstallationForMajorVersion(setup.VisualStudioVersion);
            ExperimentalInstanceConfiguration.PrepareInstanceForVersion(versionInfo);

            // create a copy of the solution to run the test on
            string solutionDirectory = SampleSolutionCopy.CreateTemporaryCopyOfSolution(setup.SampleSolutionToCopy);

            // Each step that finds a problem adds it here instead of throwing, so the one failure at the end reports
            // all of them.
            var failureMessages = new List<string>();

            string? solutionFile = null;
            try
            {
                // find the solution file for the copy
                solutionFile = Directory.GetFiles(solutionDirectory, "*.sln").Single();

                // run the test in Visual Studio
                StartVisualStudioAndRunTest(versionInfo, solutionDirectory, solutionFile, resultsFolder, logFile, test, failureMessages);
            }
            catch (Exception ex)
            {
                // Steps that throw instead of adding to the list: starting devenv, waiting for DTE.
                failureMessages.Add(ex.ToString());
            }

            try
            {
                SampleSolutionCopy.DeleteTemporaryCopyOfSolution(solutionDirectory);
            }
            catch (Exception deleteFailure)
            {
                failureMessages.Add(deleteFailure.Message);
            }

            if (solutionFile != null)
            {
                DeleteSavedIdentifierFile(solutionFile, failureMessages);
            }

            if (failureMessages.Count > 0)
            {
                string failureMessage = string.Join(Environment.NewLine + Environment.NewLine, failureMessages);
                TestResultsFolder.LogFailure(resultsFolder, failureMessage);
                throw new InvalidOperationException(failureMessage);                
            }
        }

        private static void StartVisualStudioAndRunTest(VisualStudioVersionInfo versionInfo, string solutionDirectory, string solutionFile, string resultsFolder, string logFile, Action<VisualStudioSession> test, List<string> failureMessages)
        {
            // start the Visual Studio process with the solution
            // rootsuffix sets which the process to run in the Exp instance
            // log enables the ActivityLog and writes it to logFile
            Process devenv = Process.Start(new ProcessStartInfo(
                versionInfo.DevenvPath,
                $"\"{solutionFile}\" /rootsuffix {ExperimentalInstanceConfiguration.FolderRootSuffix} /log \"{logFile}\"")
            {
                UseShellExecute = false, // so Process.Start always returns the devenv.exe process (shell can return null or existing processes)
            });

            try
            {
                StaThread.RunOnStaThreadWithMessageFilter(() =>
                {
                    // attach dte to Visual Studio
                    EnvDTE80.DTE2 dte = RunningObjectTable.WaitForDteRegistration(devenv, TimeSpan.FromMinutes(3));

                    // get the session info
                    var session = new VisualStudioSession(versionInfo, dte, devenv.Id, solutionDirectory, solutionFile);
                    try
                    {
                        // load and reset the extensions, then run the test
                        session.WaitForSolutionAndLoadExtension();
                        session.DeleteSavedIdentifiersAndReopenIfAny();
                        test(session);
                    }
                    catch (Exception ex)
                    {
                        failureMessages.Add(ex.ToString());
                    }

                    try
                    {
                        VisualStudioCloser.CloseOrKillAfterTimeout(devenv, dte, versionInfo, resultsFolder);
                    }
                    catch (TimeoutException closeFailure)
                    {
                        failureMessages.Add(closeFailure.Message);
                    }
                });
            }
            finally
            {
                if (devenv.HasExited == false)
                {
                    devenv.Kill();
                }

                devenv.Dispose();
            }

            AddActivityLogErrors(failureMessages, logFile);
        }

        /// <summary>Deletes the identifier file the test saved for its solution copy so it is not left in app data.</summary>
        private static void DeleteSavedIdentifierFile(string solutionFile, List<string> failureMessages)
        {
            string identifierFilePath = IdentifierStore.CalculateJsonFilePath(solutionFile);
            try
            {
                if (File.Exists(identifierFilePath))
                {
                    File.Delete(identifierFilePath);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                failureMessages.Add($"Couldn't delete the test's identifier file ({identifierFilePath}): {ex.Message}");
            }
        }

        private static void AddActivityLogErrors(List<string> failureMessages, string logFile)
        {
            try
            {
                IReadOnlyList<string> errors = ActivityLog.AllLoggedExtensionErrors(logFile);
                if (errors.Count > 0)
                {
                    failureMessages.Add($"The ActivityLog has errors that mention the extension ({logFile}):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
                }
            }
            catch (Exception ex) when (ex is TimeoutException || ex is InvalidOperationException)
            {
                // For example a log Visual Studio never finished writing because it was killed.
                failureMessages.Add($"Couldn't read the ActivityLog ({logFile}): {ex.Message}");
            }
        }
    }
}
