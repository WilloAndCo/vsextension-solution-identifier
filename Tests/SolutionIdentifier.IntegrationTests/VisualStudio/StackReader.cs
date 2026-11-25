using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    /// <summary>
    /// Reads Visual Studio's main thread stack using the command-line debugger (cdb.exe)
    /// Useful for diagnosing if and why the main thread is stuck 
    /// </summary>
    /// <remarks>
    /// Starts a new debugging cdb process, pauses the visual studio process, cdb reads the stack, then detaches and leaves it running.
    /// </remarks>
    internal static class StackReader
    {
        private static readonly TimeSpan DebuggerTimeout = TimeSpan.FromSeconds(60);

        public static string GetMainThreadStackWithDebugger(int visualStudioProcessId)
        {
            // lookup which command line debugger executable to use
            string? cdbExeFilepath = FindCdbExecutable();
            if (cdbExeFilepath == null)
            {
                return "(not read: cdb.exe from the Windows SDK's Debugging Tools for Windows isn't installed, or the SDK's folder isn't in the registry)";
            }

            // create temporary folders for cdb's commands, cdb takes paths to run
            string tempWorkingFolder = Path.Combine(Path.GetTempPath(), "SolutionIdentifier.IntegrationTests", "StackReader");

            // .net only needs symbols for files and line numbers, methods exist without it, so empty symbol directory is fine
            string emptySymbolFolder = Path.Combine(tempWorkingFolder, "no-symbols");
            Directory.CreateDirectory(emptySymbolFolder);

            // commands file needed to run commands, issues them one at a time top to bottom
            string commandsFilePath = Path.Combine(tempWorkingFolder, $"commands-{visualStudioProcessId}.txt");

            // sos turns dot net stacks into readable text, it's an extension to cdb, included by default with .net framework
            const string LoadExtensionToReadDotNetStackCommand = ".loadby sos clr";

            // ~0 = thread 0, e = execute, !clrstack = print the .net stack of the current thread
            const string ReadMainThreadStackCommand = "~0e !clrstack";

            // qd = quit and detach, leaves the process running
            const string DetachAndExitCommand = "qd";

            // add commands in order
            File.WriteAllLines(commandsFilePath, new[] { LoadExtensionToReadDotNetStackCommand, ReadMainThreadStackCommand, DetachAndExitCommand });

            try
            {
                // -p process ID, -y symbol path, -cf commands file
                string arguments = $" -p {visualStudioProcessId} -y \"{emptySymbolFolder}\" -cf \"{commandsFilePath}\"";

                ProcessResult result = ProcessRunner.RunProcessAndCaptureOutput(cdbExeFilepath, arguments, DebuggerTimeout);
                return FindStackInDebuggerOutput(result.Output);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is Win32Exception || ex is InvalidOperationException || ex is IOException)
            {
                return $"(not read: {ex.Message})";
            }
            finally
            {
                DeleteFolderIgnoringErrors(tempWorkingFolder);
            }
        }

        /// <summary> Extract the stack from the full debugger output, using known start and end strings. </summary>
        private static string FindStackInDebuggerOutput(string debuggerOutput)
        {
            const string StackStartsWithStr = "OS Thread Id:";
            const string StackEndsBeforeStr = "> qd";

            int startIndex = debuggerOutput.IndexOf(StackStartsWithStr, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                return debuggerOutput;
            }

            int endIndex = debuggerOutput.IndexOf(StackEndsBeforeStr, startIndex, StringComparison.Ordinal);
            if (endIndex < 0)
            {
                return debuggerOutput.Substring(startIndex);
            }

            int lineBeforeIndex = debuggerOutput.LastIndexOf('\n', endIndex);
            return debuggerOutput.Substring(startIndex, lineBeforeIndex - startIndex).Trim();
        }

        private static string? FindCdbExecutable()
        {
            // Visual Studio's own scripts find the Windows SDK using this registry method
            string? windowsKitsFolder = ReadWindowsKitsFolderFromRegistry();
            if (windowsKitsFolder == null)
            {
                return null;
            }

            // select the correct version of cdb.exe for the OS
            string architecture = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            string cdbPath = Path.Combine(windowsKitsFolder, "Debuggers", architecture, "cdb.exe");
            return File.Exists(cdbPath) ? cdbPath : null;
        }

        /// <summary>The Windows 10/11 SDK's install folder recorded in the registry</summary>
        private static string? ReadWindowsKitsFolderFromRegistry()
        {
            // Inside windows dev kits root should be KitsRoot10 (called that since Windows 10) with data to the Windows SDK folder
            using (RegistryKey? key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots"))
            {
                return key?.GetValue("KitsRoot10") as string;
            }
        }

        private static void DeleteFolderIgnoringErrors(string folder)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }
    }
}
