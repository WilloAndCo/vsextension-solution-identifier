#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SolutionIdentifier.Options
{
    /// <summary>The "Storage for all solutions" drop-down's choices.</summary>
    // The grid shows the names as they are written here.
    // ComVisible(true) because automation reads the page's properties through COM.
    [ComVisible(true)]
    public enum StoredIdentifiersAction
    {
        Keep,
        Clear,
    }

    /// <summary>Tools > Options > Solution Identifier > General.</summary>
    [ComVisible(true)]
    public sealed class SolutionIdentifierOptionsPage : DialogPage
    {
        public const string OptionsEntryName = "Solution Identifier";
        public const string OptionsSubPageName = "General";
        public const bool DefaultShowSolutionIdentifier = true;
        public const int DefaultFoldersToShow = 2;
        public const StoredIdentifiersAction DefaultStoredIdentifiersAction = StoredIdentifiersAction.Keep;

        private bool _showSolutionIdentifier = DefaultShowSolutionIdentifier;
        private int _foldersToShow = DefaultFoldersToShow;

        /// <summary>Raised when the page's changes are applied with Clear chosen.</summary>
        public event EventHandler? ClearAllStoredIdentifiersRequested;

        /// <summary>
        /// Raised when "Show solution identifier" or "Folders to show" changes, whether from the Options dialog,
        /// loading the saved settings, or automation such as DTE.Properties.
        /// </summary>
        public event EventHandler? MenuBarOptionsChanged;

        [Category("Menu bar")]
        [DisplayName("Show solution identifier")]
        [Description("Shows the open solution's identifier on the menu bar.")]
        [DefaultValue(DefaultShowSolutionIdentifier)]
        public bool ShowSolutionIdentifier
        {
            get => _showSolutionIdentifier;
            set => SetShowSolutionIdentifierAndNotifyIfDifferent(value);
        }

        // Passed to SolutionPathDisplayText.ShortenPathToLastFolders.
        [Category("Menu bar")]
        [DisplayName("Folders to show")]
        [Description("For a solution with no title, how many folders above the solution file the menu bar shows. 0 shows only the file name.")]
        [DefaultValue(DefaultFoldersToShow)]
        public int FoldersToShow
        {
            get => _foldersToShow;
            set => SetFoldersToShowAndNotifyIfDifferent(value);
        }

        /// <summary>A one-off action rather than a setting, so it goes back to Keep when saved and when Options closes.</summary>
        [Category("Stored identifiers")]
        [DisplayName("Storage for all solutions")]
        [Description("Setting to 'Clear' deletes every solution's stored information when you click OK in the Options dialog.")]
        [DefaultValue(DefaultStoredIdentifiersAction)]
        public StoredIdentifiersAction ChosenActionForStoredIdentifiers { get; set; } = DefaultStoredIdentifiersAction;

        public override void SaveSettingsToStorage()
        {
            // Always reset back to keep on save
            ChosenActionForStoredIdentifiers = StoredIdentifiersAction.Keep;
            base.SaveSettingsToStorage();
        }
        protected override void OnClosed(EventArgs e)
        {
            // Always reset back to keep on cancel
            ChosenActionForStoredIdentifiers = StoredIdentifiersAction.Keep;
            base.OnClosed(e);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply && ChosenActionForStoredIdentifiers == StoredIdentifiersAction.Clear)
            {
                ClearAllStoredIdentifiersRequested?.Invoke(this, EventArgs.Empty);
            }

            base.OnApply(e);
        }

        private void SetShowSolutionIdentifierAndNotifyIfDifferent(bool showSolutionIdentifier)
        {
            if (_showSolutionIdentifier == showSolutionIdentifier)
            {
                return;
            }

            _showSolutionIdentifier = showSolutionIdentifier;
            MenuBarOptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetFoldersToShowAndNotifyIfDifferent(int foldersToShow)
        {
            if (_foldersToShow == foldersToShow)
            {
                return;
            }

            _foldersToShow = foldersToShow;
            MenuBarOptionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
