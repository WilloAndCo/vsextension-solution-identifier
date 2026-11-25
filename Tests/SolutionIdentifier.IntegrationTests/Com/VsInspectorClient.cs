using System;
using System.Reflection;

namespace SolutionIdentifier.IntegrationTests.Com
{
    /// <summary>Calls the VisualStudioInspector extension through DTE.GetObject.</summary>
    /// <remarks>Names and functionality must match VisualStudioInspector exactly.</remarks>
    internal sealed class VsInspectorClient
    {
        // Must match VsInspector.AutomationName.
        private const string InspectorAutomationName = "SolutionIdentifier.VisualStudioInspector";

        private readonly EnvDTE80.DTE2 _dte;
        private object? _vsInspectorProxy;

        public VsInspectorClient(EnvDTE80.DTE2 dte)
        {
            // DTE of the devenv.exe that VisualStudioTestRun started
            _dte = dte;
        }

        /// <summary>Loads the package with this GUID if Visual Studio hasn't already; true once it is loaded.</summary>
        /// <remarks>Must be called before interacting with Options.</remarks>
        public bool LoadPackageIfNotLoaded(string packageGuidString) =>
            InvokeInspectorMethodByName<bool>(nameof(LoadPackageIfNotLoaded), packageGuidString);

        public int ReadButtonFlags(string commandSetGuidString, int buttonId) =>
            InvokeInspectorMethodByName<int>(nameof(ReadButtonFlags), commandSetGuidString, buttonId);

        public string ReadButtonText(string commandSetGuidString, int buttonId) =>
            InvokeInspectorMethodByName<string>(nameof(ReadButtonText), commandSetGuidString, buttonId);

        /// <summary>Calls the inspector's method of the same name.</summary>
        /// <remarks>This runs on the Visual Studio instance through the COM interface.</remarks>
        private T InvokeInspectorMethodByName<T>(string methodName, params object[] args)
        {
            if (_vsInspectorProxy == null)
            {
                _vsInspectorProxy = _dte.GetObject(InspectorAutomationName)
                    ?? throw new InvalidOperationException("The inspector extension isn't available (DTE.GetObject returned null).");
            }

            // Runs inside Visual Studio, through COM's IDispatch
            return (T)_vsInspectorProxy.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, _vsInspectorProxy, args);
        }
    }
}
