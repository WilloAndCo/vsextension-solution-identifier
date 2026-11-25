using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using SolutionIdentifier.IntegrationTests.Com;
using SolutionIdentifier.IntegrationTests.VisualStudio;

namespace SolutionIdentifier.IntegrationTests.Screen
{
    /// <summary>
    /// Finds and interacts with Visual Studio's windows, buttons and menu items using Windows UI Automation
    /// </summary>
    /// <remarks>
    /// Every UI Automation call here runs on its own MtaThread from ms guidelines - https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading.
    /// These methods are called from the test StaThread
    /// </remarks>
    internal sealed class ScreenElementsController
    {
        private readonly int _processId;

        public ScreenElementsController(int processId)
        {
            _processId = processId;
        }

        public AutomationElement GetWindow(string name, TimeSpan retryTimeout)
        {
            AutomationElement? found = null;
            Wait.PollUntilTrueOrThrow(() => (found = FindWindow(name)) != null, retryTimeout, () => $"a window named '{name}'. Visual Studio's windows: {WindowNamesToString()}");
            return found!;
        }

        /// <summary>A window of Visual Studio's with this name: a top-level window, or one inside any of them.</summary>
        public AutomationElement? FindWindow(string name) => ReadOnMtaThread(() =>
        {
            var namedCondition = new PropertyCondition(AutomationElement.NameProperty, name);
            var isWindowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            foreach (AutomationElement window in FindVisualStudioTopLevelWindows())
            {
                if (window.Current.Name == name)
                {
                    return window;
                }

                AutomationElement inner = window.FindFirst(TreeScope.Descendants, new AndCondition(namedCondition, isWindowCondition));
                if (inner != null)
                {
                    return inner;
                }
            }

            return null;
        });

        public bool CannotFindWindow(string name) => FindWindow(name) == null;

        /// <summary>
        /// Every on-screen element with this name, in all of Visual Studio's windows. Null when the search failed.
        /// </summary>
        public IReadOnlyList<AutomationElement>? FindVisibleElementsNamed(string name) => ReadOnMtaThread(() =>
        {
            var isNamed = new PropertyCondition(AutomationElement.NameProperty, name);
            return FindVisualStudioTopLevelWindows()
                .SelectMany(window => window.FindAll(TreeScope.Descendants, isNamed).Cast<AutomationElement>())
                .Where(IsOnScreen)
                .ToList();
        });

        public static AutomationElement GetDescendantByAutomationId(AutomationElement scope, string automationId, TimeSpan retryTimeout)
        {
            var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
            AutomationElement? found = null;
            Wait.PollUntilTrueOrThrow(() => (found = ReadOnMtaThread(() => scope.FindFirst(TreeScope.Descendants, condition))) != null, retryTimeout, () => $"an element with automation ID '{automationId}'");
            return found!;
        }

        public static AutomationElement GetDescendantByNameAndType(AutomationElement scope, string name, ControlType controlType, TimeSpan retryTimeout)
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, name),
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
            AutomationElement? found = null;
            Wait.PollUntilTrueOrThrow(() => (found = ReadOnMtaThread(() => scope.FindFirst(TreeScope.Descendants, condition))) != null, retryTimeout, () => $"a {controlType.ProgrammaticName} named '{name}'");
            return found!;
        }

        /// <summary>Presses a button through its Invoke pattern</summary>
        public static void PressButton(AutomationElement button)
        {
            MtaThread.RunOnMtaThread(() => ((InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern)).Invoke());
        }

        public static void ReplaceTextBoxText(AutomationElement textBox, string text)
        {
            MtaThread.RunOnMtaThread(() => ((ValuePattern)textBox.GetCurrentPattern(ValuePattern.Pattern)).SetValue(text));
        }

        public static void SelectListItem(AutomationElement listItem)
        {
            MtaThread.RunOnMtaThread(() => ((SelectionItemPattern)listItem.GetCurrentPattern(SelectionItemPattern.Pattern)).Select());
        }

        public static bool IsMenuItem(AutomationElement element) =>
            MtaThread.RunOnMtaThread(() => element.Current.ControlType == ControlType.MenuItem);

        public static Rect GetBoundsInPixels(AutomationElement element) => MtaThread.RunOnMtaThread(() => element.Current.BoundingRectangle);

        public static void BringWindowToFront(IntPtr window)
        {
            // Often refused by Windows (it only lets some processes change the foreground window). Not checked:
            // ThrowIfPointIsNotOverProcessWindow checks the click point itself, which is what matters.
            SetForegroundWindow(window);

            TimeSpan waitForRedraw = TimeSpan.FromMilliseconds(50);
            Thread.Sleep(waitForRedraw);
        }

        public static string ElementsToString(IEnumerable<AutomationElement> elements) =>
            MtaThread.RunOnMtaThread(() => string.Join("; ", elements.Select(ElementToString)));

        /// <summary>The names of Visual Studio's windows and the windows inside them, for failure messages.</summary>
        public string WindowNamesToString()
        {
            try
            {
                return ReadOnMtaThread(() =>
                {
                    var isWindowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
                    var names = new List<string>();
                    foreach (AutomationElement window in FindVisualStudioTopLevelWindows())
                    {
                        names.Add($"'{window.Current.Name}'");
                        foreach (AutomationElement inner in window.FindAll(TreeScope.Descendants, isWindowCondition))
                        {
                            names.Add($"  inside it: '{inner.Current.Name}'");
                        }
                    }

                    return names.Count == 0 ? "(no windows)" : string.Join("; ", names);
                }) ?? "(couldn't read windows: they closed while being read)";
            }
            catch (InvalidOperationException ex)
            {
                return $"(couldn't read windows: {ex.Message})";
            }
        }
                
        private List<AutomationElement> FindVisualStudioTopLevelWindows()
        {
            // A Top Level window is a window with no parent. Multiple windows may belong to Visual Studio and have the same process id (main window, options, etc)
            return AutomationElement.RootElement
                .FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _processId))
                .Cast<AutomationElement>()
                .ToList();
        }

        private static bool IsOnScreen(AutomationElement element)
        {
            try
            {
                Rect bounds = element.Current.BoundingRectangle;
                return element.Current.IsOffscreen == false && bounds.IsEmpty == false && bounds.Width > 0 && bounds.Height > 0;
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        private static string ElementToString(AutomationElement element)
        {
            try
            {
                AutomationElement.AutomationElementInformation current = element.Current;
                return $"{current.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty)} '{current.Name}' at {current.BoundingRectangle}";
            }
            catch (ElementNotAvailableException)
            {
                return "(gone)";
            }
        }

        /// <summary>Null when an element disappears while it is being read (a closing window, for example).</summary>
        /// <remarks>Use for searches where an element disappearing partway through should count as "not found" rather than an exception</remarks>
        private static T? ReadOnMtaThread<T>(Func<T?> read) where T : class
        {
            return MtaThread.RunOnMtaThread(() =>
            {
                try
                {
                    return read();
                }
                catch (ElementNotAvailableException)
                {
                    return null;
                }
            });
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr window);
    }
}
