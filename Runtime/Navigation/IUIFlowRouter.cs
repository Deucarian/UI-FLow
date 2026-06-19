using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Public router API exposed by each UI Flow host.
    /// </summary>
    public interface IUIFlowRouter
    {
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowNavigationResult> PushAsync(
            UIFlowRoute route,
            object arguments = null,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowNavigationResult> PopAsync(
            UIFlowChannelId channel,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowNavigationResult> ReplaceAsync(
            UIFlowRoute route,
            object arguments = null,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowNavigationResult> ResetAsync(
            UIFlowChannelId channel,
            UIFlowRoute rootRoute = null,
            object arguments = null,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowNavigationResult> PopToAsync(
            UIFlowChannelId channel,
            UIFlowRouteId routeId,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowNavigationResult> BackAsync(
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        Task<UIFlowPresentationResult<TResult>> PresentAsync<TResult>(
            UIFlowRoute route,
            object arguments = null,
            UIFlowPresentationOptions options = default(UIFlowPresentationOptions),
            CancellationToken cancellationToken = default(CancellationToken));

        bool TryGetRoute(UIFlowRouteId routeId, out UIFlowRoute route);
        IReadOnlyList<UIFlowChannelSnapshot> GetChannelSnapshots();
        UIFlowOperationSnapshot CurrentOperation { get; }
        int QueuedOperationCount { get; }
        UIFlowInitializationState InitializationState { get; }
    }
}
