using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SolutionIdentifier.VisualStudioInspector
{
    /// <summary>
    /// Lets the integration tests reach VsInspector through DTE.GetObject("SolutionIdentifier.VisualStudioInspector").
    /// ProvideAutomationObject registers the name under this package's GUID, so DTE.GetObject can find the package, and calling GetObject will load it.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutomationObject(VsInspector.AutomationName)]
    [Guid(PackageGuidString)]
    public sealed class VsInspectorPackage : AsyncPackage
    {
        public const string PackageGuidString = "13e3b293-04a6-4db9-b761-39822609005f";

        /// <summary>Created on the first DTE.GetObject call; lasts until Visual Studio closes.</summary>
        private VsInspector? _vsInspector;

        protected override object GetAutomationObject(string name)
        {
            if (string.Equals(name, VsInspector.AutomationName, StringComparison.Ordinal) == false)
            {
                return base.GetAutomationObject(name);
            }

            return _vsInspector ??= new VsInspector();
        }
    }
}
