using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SolutionIdentifier.VisualStudioInspector
{
    /// <summary>
    /// What the integration tests call through DTE.GetObject("SolutionIdentifier.VisualStudioInspector")
    /// Provides information DTE can't read by default
    /// </summary>
    /// <remarks>
    /// AutoDispatch - lets the integration test process call this instance's public methods by name, through IDispatch.
    /// Instance is accessed through DTE by name _dte.GetObject(InspectorAutomationName), and IDispatch calls methods using GetType().InvokeMember(methodName)
    /// </remarks>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public sealed class VsInspector
    {
        public const string AutomationName = "SolutionIdentifier.VisualStudioInspector";

        /// <summary>True once the package is loaded.</summary>
        public bool LoadPackageIfNotLoaded(string packageGuidString) => RunOnVisualStudioUIThread(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Visual Studio shell service, IVsShell is the list of callable methods
            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is not IVsShell shell)
            {
                return false;
            }

            var packageGuid = new Guid(packageGuidString);

            // Check Visual Studio if package with this guid is loaded
            if (ErrorHandler.Succeeded(shell.IsPackageLoaded(ref packageGuid, out IVsPackage loadedPackage)) && loadedPackage != null)
            {
                return true;
            }

            // Otherwise load it now
            return ErrorHandler.Succeeded(shell.LoadPackage(ref packageGuid, out IVsPackage newlyLoadedPackage)) && newlyLoadedPackage != null;
        });

        /// <summary>The button's flags reported from Visual Studio, in Windows enum OLECMDF form (supported, enabled, latched, invisible...).</summary>
        public int ReadButtonFlags(string commandSetGuidString, int buttonId) => RunOnVisualStudioUIThread(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QueryButtonStatus(commandSetGuidString, buttonId).Flags;
        });

        public string ReadButtonText(string commandSetGuidString, int buttonId) => RunOnVisualStudioUIThread(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return QueryButtonStatus(commandSetGuidString, buttonId).Text;
        });

        /// <summary>Calling thread waits for UI thread to be free and runs <paramref name="read"/> on it.</summary>
        private static T RunOnVisualStudioUIThread<T>(Func<T> read)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return read();
            });
        }

        /// <summary>Ask Visual Studio for the button flags and text using COM interfaces.</summary>
        private static (int Flags, string Text) QueryButtonStatus(string commandSetGuidString, int buttonId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // SUIHostCommandDispatcher = service to ask about commands, in this extension the buttons
            // IOleCommandTarget = the callable methods on that service, QueryStatus to query or Exec to run it.
            if (ServiceProvider.GlobalProvider.GetService(typeof(SUIHostCommandDispatcher)) is not IOleCommandTarget commandDispatcher)
            {
                throw new InvalidOperationException("The SUIHostCommandDispatcher service isn't available.");
            }

            var commandSetGuid = new Guid(commandSetGuidString);

            // OLECMD = describes one command with an id. QueryStatus() fills the flags.
            var commandsArray = new[] { new OLECMD { cmdID = (uint)buttonId } };

            // Max length of button text.
            const int TextBufferChars = 256;

            // QueryStatus writes the text in a buffer
            // The memory must start with a OLECMDTEXT head - which text and buffer size.
            // The text goes in the header's last field, rgwz, which is one character and runs past into the buffer
            int textMemorySizeInBytes = Marshal.SizeOf<OLECMDTEXT>() + TextBufferChars * sizeof(char);

            // Memory block .NET doesn't manage, freed in FreeCoTaskMem
            IntPtr textMemory = Marshal.AllocCoTaskMem(textMemorySizeInBytes);
            try
            {
                // Copies a new C# array of 0s into the text Memory, to ensure all the bytes are 0
                Marshal.Copy(new byte[textMemorySizeInBytes], 0, textMemory, textMemorySizeInBytes);

                // Write the header into the start of the memory. cmdtextf = get name, cwBuf = buffer size
                var textHeader = new OLECMDTEXT
                {
                    cmdtextf = (uint)OLECMDTEXTF.OLECMDTEXTF_NAME,
                    cwBuf = TextBufferChars,
                };

                // Copy textHeader struct to address at textMemory, puts it at the start
                Marshal.StructureToPtr(textHeader, textMemory, fDeleteOld: false);

                // QueryStatus writes the flags into commands[0].cmdf and the text into textMemory.
                const uint numCommands = 1;
                ErrorHandler.ThrowOnFailure(commandDispatcher.QueryStatus(ref commandSetGuid, numCommands, commandsArray, textMemory));

                // The text starts at rgwz, find the starting address
                IntPtr offsetOfTextCharInHeader = Marshal.OffsetOf<OLECMDTEXT>(nameof(OLECMDTEXT.rgwz));
                int offsetInBytes = offsetOfTextCharInHeader.ToInt32();
                IntPtr textStart = textMemory + offsetInBytes;

                // Read characters from there up to the first 0.
                string text = Marshal.PtrToStringUni(textStart) ?? string.Empty;

                // Return the flags from the header, and the text found from the memory block
                return ((int)commandsArray[0].cmdf, text);
            }
            finally
            {
                Marshal.FreeCoTaskMem(textMemory);
            }
        }
    }
}
