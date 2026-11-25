using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SolutionIdentifier.IntegrationTests.Com
{
    /// <summary>
    /// Runs code on a new STA thread with RetryCallsMessageFilter registered. DTE and Inspector calls are COM calls that
    /// need the filter, and COM only allows a message filter on an STA thread.
    /// </summary>
    /// <remarks>
    /// Registers RetryCallsMessageFilter on the new thread while the code runs.
    /// </remarks>
    internal static class StaThread
    {
        // Setting so Windows never scales coordinates. Every position is in real pixels on every monitor.
        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        public static void RunOnStaThreadWithMessageFilter(Action work)
        {
            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                MakeCurrentThreadPerMonitorDpiAware();
                RetryCallsMessageFilter.RegisterOnCurrentThread();
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    RetryCallsMessageFilter.RevokeOnCurrentThread();
                }
            })
            {
                Name = "DTE test thread",
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            failure?.Throw();
        }

        /// <summary>
        /// Makes the calling thread per-monitor DPI aware, so it doesn't get coordinates scaled by Windows display scaling.
        /// </summary>
        /// <remarks>Set because Visual Studio is DPI aware.</remarks>
        public static void MakeCurrentThreadPerMonitorDpiAware() => SetThreadDpiAwarenessContext(PerMonitorAwareV2);

        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);
    }
}
