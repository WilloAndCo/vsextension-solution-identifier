#nullable enable
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionIdentifier.MenuBar;
using SolutionIdentifier.Options;
using SolutionIdentifier.Storage;
using Task = System.Threading.Tasks.Task;

namespace SolutionIdentifier
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.MenuBarSplitButton", 1)] // provides a menu or toolbar item, anywhere in Visual Studio
    [ProvideOptionPage(typeof(SolutionIdentifierOptionsPage), categoryName: SolutionIdentifierOptionsPage.OptionsEntryName, pageName: SolutionIdentifierOptionsPage.OptionsSubPageName,
        categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true)] // supportsAutomation allows testing through DTE
    [Guid(PackageGuidString)]
    public sealed class SolutionIdentifierPackage : AsyncPackage
    {
        // Must match guidSolutionIdentifierPackage in SolutionIdentifierPackage.vsct.
        public const string PackageGuidString = "15a6b44b-7397-466d-91fa-4ebff0ee5940";

        private IdentifierController? _controller;
        private MenuBarSplitButton? _menuBarSplitButton;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (await GetServiceAsync(typeof(IMenuCommandService)) is not OleMenuCommandService commandService)
            {
                return;
            }

            if (await GetServiceAsync(typeof(SVsUIShell)) is not IVsUIShell uiShell)
            {
                return;
            }

            if (await GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solution)
            {
                return;
            }

            var options = (SolutionIdentifierOptionsPage)GetDialogPage(typeof(SolutionIdentifierOptionsPage));
            _controller = new IdentifierController(this, uiShell, solution, new IdentifierStore(), options);

            _menuBarSplitButton = new MenuBarSplitButton(_controller);
            _menuBarSplitButton.RegisterButtons(commandService);

            _controller.SubscribeToSolutionEvents();
            _controller.SubscribeToOptionsPageEvents();
            _controller.OnExtensionInitialised();
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Disposing is false if called from a finalizer. No base class has a finalizer so this will never be false.
            // ThrowIfNotOnUIThread() will throw if that ever changes
            if (disposing && _controller != null)
            {
                _controller.Dispose();
                _controller = null;
            }

            base.Dispose(disposing);
        }
    }
}
