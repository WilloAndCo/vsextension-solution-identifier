using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    /// <summary>
    /// Sets up the configuration folder for the Experimental Instance, one for each version of Visual Studio the tests run on.
    /// It ensures the extensions are installed and ready to use for the tests.
    /// </summary>
    internal static class ExperimentalInstanceConfiguration
    {
        /// <summary>To find the configuration folder for the Experimental Instance e.g. "%LocalAppData%\Microsoft\VisualStudio\18.0_0123456789Exp"</summary>
        /// <remarks>Exp is the default suffix used for Experimental Instance</remarks>
        public const string FolderRootSuffix = "Exp";

        /// <summary> Install the developed extension and the extension inspector in the Experimental Instance Folder </summary>
        private static readonly string[] ExtensionProjectFiles =
        {
            @"SolutionIdentifier\SolutionIdentifier.csproj",
            @"Tests\SolutionIdentifier.VisualStudioInspector\SolutionIdentifier.VisualStudioInspector.csproj",
        };

        /// <summary>Path the to exe than can create, reset, and delete the experimental instance.</summary>
        private const string CreateExpInstanceRelativePath = @"VSSDK\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe";

        /// <summary>Installations already set up in this test run</summary>
        private static readonly Dictionary<string, VisualStudioVersionInfo> PreparedVersionInstances = new Dictionary<string, VisualStudioVersionInfo>();

        public static void PrepareInstanceForVersion(VisualStudioVersionInfo versionInfo)
        {
            if (PreparedVersionInstances.ContainsKey(versionInfo.InstallationId))
            {
                // already has the correct setup
                return;
            }

            // Not safe to modify if an instance is already running somewhere
            ThrowIfAnyInstanceAlreadyRunning();

            BuildExtensions(versionInfo);
            ResetSettingsToDefault(versionInfo);

            // Only add after successfully prepared
            PreparedVersionInstances.Add(versionInfo.InstallationId, versionInfo);
        }

        /// <summary>Resets the Exp instance of every Visual Studio prepared in this run.</summary>
        public static void ResetAllPreparedInstances()
        {
            var failureMessages = new List<string>();
            foreach (VisualStudioVersionInfo versionInfo in PreparedVersionInstances.Values)
            {
                try
                {
                    ResetInstance(versionInfo);
                }
                // TimeoutException: the reset ran too long. Win32Exception: CreateExpInstance.exe couldn't be started.
                catch (Exception resetFailure) when (resetFailure is InvalidOperationException || resetFailure is TimeoutException || resetFailure is Win32Exception)
                {
                    failureMessages.Add(resetFailure.Message);
                }
            }

            PreparedVersionInstances.Clear();
            if (failureMessages.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine + Environment.NewLine, failureMessages));
            }
        }

        private static void ResetInstance(VisualStudioVersionInfo versionInfo)
        {
            // Cannot set up if an instance is already running somewhere - only one exp instance exists per version
            ThrowIfAnyInstanceAlreadyRunning();

            // VSInstance chooses which installation to reset the Exp folder to. Use the versionInfo to find the normal installation folder.
            ProcessResult reset = ProcessRunner.RunProcessAndCaptureOutput(
                Path.Combine(versionInfo.InstallationPath, CreateExpInstanceRelativePath),
                arguments: $"/Reset /VSInstance={versionInfo.Version.Major}.0_{versionInfo.InstallationId} /RootSuffix={FolderRootSuffix}",
                TimeSpan.FromMinutes(5));

            if (reset.ExitCode != 0)
            {
                throw new InvalidOperationException($"Resetting {versionInfo}'s Exp at the end of the test run failed with exit code {reset.ExitCode}:{Environment.NewLine}{reset.Output}");
            }
        }

        private static void ThrowIfAnyInstanceAlreadyRunning()
        {
            int? processId = FindInstanceProcessId();
            if (processId == null)
            {
                return;
            }

            throw new InvalidOperationException(
                $"An Exp window is open (process {processId}). Close it before running the integration tests: they reset Exp.");
        }

        /// <summary>A devenv.exe from any installation is running with /rootsuffix Exp</summary>
        private static int? FindInstanceProcessId()
        {            
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process WHERE Name = 'devenv.exe'"))
            {
                foreach (ManagementBaseObject process in searcher.Get())
                {
                    string commandLine = process["CommandLine"] as string ?? string.Empty;

                    // process commands includes the rootsuffix parameter we run with
                    bool foundSuffix = commandLine.IndexOf($"/rootsuffix {FolderRootSuffix}", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (foundSuffix)
                    {                        
                        return Convert.ToInt32(process["ProcessId"]);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Building the extension recompiles the code, and in the same way that Visual Studio does when it builds, adds it to the Experimental Instance ready to test
        /// </summary>
        private static void BuildExtensions(VisualStudioVersionInfo versionInfo)
        {
            foreach (string project in ExtensionProjectFiles)
            {
                string projectPath = Path.Combine(SolutionPaths.Root, project);

                // -v:m is minimal output, to show just warnings and errors.
                // -p:VisualStudioVersion builds for the version being tested
                ProcessResult build = ProcessRunner.RunProcessAndCaptureOutput(
                    versionInfo.MSBuildPath,
                    arguments: $"\"{projectPath}\" -restore -t:Build -p:Configuration=Debug -p:VisualStudioVersion={versionInfo.Version.Major}.0 -v:m",
                    TimeSpan.FromMinutes(10));

                if (build.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Building and deploying {project} with {versionInfo}'s MSBuild failed:{Environment.NewLine}{build.Output}");
                }
            }
        }

        /// <summary>Restores the default settings by running with /resetsettings and then closing.</summary>
        private static void ResetSettingsToDefault(VisualStudioVersionInfo versionInfo)
        {
            // General is one of /resetsettings' default collection specifiers on Learn, not a file name.
            ProcessResult result = ProcessRunner.RunProcessAndCaptureOutput(
                versionInfo.DevenvPath,
                arguments: $"/rootsuffix {FolderRootSuffix} /resetsettings General /command File.Exit",
                TimeSpan.FromMinutes(5));

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Resetting {versionInfo}'s Exp settings failed with exit code {result.ExitCode}:{Environment.NewLine}{result.Output}");
            }
        }
    }
}
