#nullable enable
using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionIdentifier.Appearance;
using SolutionIdentifier.Dialog;
using SolutionIdentifier.Options;
using SolutionIdentifier.Storage;

namespace SolutionIdentifier
{
    /// <summary>Keeps track of the open solution's identifier, and shows the dialog that changes it.</summary>
    internal sealed class IdentifierController : IDisposable
    {
        private readonly Package _package;
        private readonly IVsUIShell _uiShell;
        private readonly IVsSolution _solution;
        private readonly IdentifierStore _identifierStore;
        private readonly SolutionIdentifierOptionsPage _options;
        private SolutionAppearance _solutionAppearance = SolutionAppearance.NoSolutionAppearance;

        public IdentifierController(Package package, IVsUIShell uiShell, IVsSolution solution, IdentifierStore identifierStore, SolutionIdentifierOptionsPage options)
        {
            _package = package;
            _uiShell = uiShell;
            _solution = solution;
            _identifierStore = identifierStore;
            _options = options;
        }

        public bool IsIdentifierShown => _options.ShowSolutionIdentifier && IsSolutionOpen;
        public bool IsSolutionOpen => _solutionAppearance.IsSolutionOpen;
        public IdentifierAppearance CurrentIdentifier => _solutionAppearance.Appearance;

        public string BuildCurrentTitle()
        {
            return _solutionAppearance.BuildTitleToShow(_options.FoldersToShow);
        }

        public void SubscribeToSolutionEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SolutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;
            SolutionEvents.OnAfterCloseSolution += OnAfterCloseSolution;
            SolutionEvents.OnAfterRenameSolution += OnAfterRenameSolution;
        }

        public void SubscribeToOptionsPageEvents()
        {
            _options.ClearAllStoredIdentifiersRequested += OnClearAllIdentifiersRequested;
            _options.MenuBarOptionsChanged += OnMenuBarOptionsChanged;
        }

        public void OnExtensionInitialised()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // May happen after the solution has been loaded
            LoadIdentifierForOpenSolution();
        }

        public void ShowIdentifierDialogAndSaveResult()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string? solutionPath = _solutionAppearance.SolutionPath;
            if (solutionPath == null)
            {
                return;
            }

            IdentifierAppearance currentIdentifier = _solutionAppearance.Appearance;

            var identifierDialog = new IdentifierDialog(currentIdentifier);
            identifierDialog.ShowModal();

            switch (identifierDialog.ChosenAction)
            {
                case IdentifierDialogAction.Save:
                    _identifierStore.SaveIdentifier(solutionPath, identifierDialog.ChosenIdentifier);
                    _solutionAppearance = SolutionAppearance.CreateForSolution(solutionPath, identifierDialog.ChosenIdentifier);
                    break;
                case IdentifierDialogAction.Reset:
                    _identifierStore.DeleteIdentifier(solutionPath);
                    _solutionAppearance = SolutionAppearance.CreateForSolution(solutionPath, IdentifierAppearance.CreateDefaultForSolution(solutionPath));
                    break;
                case IdentifierDialogAction.OpenOptions:
                    _package.ShowOptionPage(typeof(SolutionIdentifierOptionsPage));
                    break;
            }

            UpdateMenuBar();
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UnsubscribeFromEvents();
        }

        private void LoadIdentifierForOpenSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string? solutionPath = ReadSavedSolutionFilePath();
            if (solutionPath == null)
            {
                _solutionAppearance = SolutionAppearance.NoSolutionAppearance;
            }
            else
            {
                _solutionAppearance = SolutionAppearance.CreateForSolution(solutionPath, _identifierStore.ReadIdentifierOrDefault(solutionPath));
            }

            UpdateMenuBar();
        }

        private void UnsubscribeFromEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SolutionEvents.OnAfterOpenSolution -= OnAfterOpenSolution;
            SolutionEvents.OnAfterCloseSolution -= OnAfterCloseSolution;
            SolutionEvents.OnAfterRenameSolution -= OnAfterRenameSolution;
            _options.ClearAllStoredIdentifiersRequested -= OnClearAllIdentifiersRequested;
            _options.MenuBarOptionsChanged -= OnMenuBarOptionsChanged;
        }

        private void OnClearAllIdentifiersRequested(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _identifierStore.DeleteAllIdentifiers();

            // call load on the now empty data, which will show the default
            LoadIdentifierForOpenSolution();
        }

        private void OnMenuBarOptionsChanged(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Covers the case where options are changed from outside the options menu
            UpdateMenuBar();
        }

        private void OnAfterOpenSolution(object sender, OpenSolutionEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadIdentifierForOpenSolution();
        }

        private void OnAfterRenameSolution(object sender, RenameSolutionEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadIdentifierForOpenSolution();
        }

        private void OnAfterCloseSolution(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _solutionAppearance = SolutionAppearance.NoSolutionAppearance;
            UpdateMenuBar();
        }

        private string? ReadSavedSolutionFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _solution.GetSolutionInfo(out _, out string solutionFilePath, out _);

            // Empty while no solution is open.
            if (string.IsNullOrEmpty(solutionFilePath))
            {
                return null;
            }

            // A folder opened without a solution file, or a new solution not yet saved, has no file on disk.
            if (File.Exists(solutionFilePath) == false)
            {
                return null;
            }

            return solutionFilePath;
        }

        private void UpdateMenuBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            const int dontForceImmediateUpdate = 0;
            _uiShell.UpdateCommandUI(dontForceImmediateUpdate);
        }
    }
}
