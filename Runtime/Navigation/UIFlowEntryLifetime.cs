using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.UIFlow
{
    internal static class UIFlowEntryLifetime
    {
        public static async Task ReleaseAsync(UIFlowStackEntry entry, UIFlowDismissalReason reason)
        {
            if (entry == null || entry.Released) return;
            entry.Released = true;
            Attempt(entry, "CancelLifetime", () => entry.EntryLifetimeCts.Cancel());
            Attempt(entry, "CompletePresentation", () => CompletePresentation(entry, reason));
            Attempt(entry, "ReleaseContext", () =>
            {
                if (entry.Screen != null) entry.Screen.ReleaseContext();
            });
            entry.EntryLifetimeCts.Dispose();
            await ReleaseLeaseAsync(entry.Lease, entry.ReleaseProvider);
        }

        public static async Task ReleaseLeaseAsync(UIFlowScreenLease lease, IUIFlowScreenProvider provider)
        {
            if (lease == null || provider == null) return;
            try { await provider.ReleaseAsync(lease, CancellationToken.None); }
            catch (Exception exception)
            {
                Report(lease.Route, "ReleaseLease", exception);
            }
        }

        private static void CompletePresentation(UIFlowStackEntry entry, UIFlowDismissalReason reason)
        {
            if (entry.Presentation == null) return;
            if (entry.Presentation.CompletionRequested) entry.Presentation.CompleteAfterExit();
            else if (reason == UIFlowDismissalReason.HostDestroyed)
                entry.Presentation.Completion.CompleteHostDestroyed("UI Flow host was destroyed.");
            else if (reason == UIFlowDismissalReason.RemovedByReplacementOrReset)
                entry.Presentation.CompleteRemoved(reason, "Presentation entry was removed by replacement or reset.");
            else entry.Presentation.Completion.CompleteDismissed(reason, null);
        }

        private static void Attempt(UIFlowStackEntry entry, string operation, Action action)
        {
            try { action(); }
            catch (Exception exception) { Report(entry.Route, operation, exception); }
        }

        private static void Report(UIFlowRoute route, string operation, Exception exception)
        {
            UIFlowLog.Navigation.Exception(new UIFlowNavigationException(
                route == null ? "<released>" : route.TargetChannel.ToString(), operation,
                route == null ? "<released>" : route.RouteId.ToString(),
                "Screen cleanup failed. Remaining cleanup was attempted; committed navigation was preserved.", exception));
        }
    }
}
