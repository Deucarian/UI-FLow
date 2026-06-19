using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Built-in provider for serialized external scene screen bindings.
    /// </summary>
    public sealed class UIFlowExternalSceneScreenProvider : IUIFlowScreenProvider
    {
        public const string Id = "ui-flow-external";

        private readonly IReadOnlyList<UIFlowSceneScreenBinding> _bindings;

        public UIFlowExternalSceneScreenProvider(IReadOnlyList<UIFlowSceneScreenBinding> bindings)
        {
            _bindings = bindings;
        }

        public string ProviderId
        {
            get { return Id; }
        }

        public Task<UIFlowScreenLease> AcquireAsync(UIFlowScreenRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UIFlowRoute route = request.Route;
            for (int i = 0; i < _bindings.Count; i++)
            {
                UIFlowSceneScreenBinding binding = _bindings[i];
                if (binding != null && binding.Matches(route) && binding.Screen != null)
                {
                    binding.Screen.transform.SetParent(request.Parent, false);
                    binding.Screen.SetVisibleImmediate(false, false);
                    return Task.FromResult(new UIFlowScreenLease(binding.Screen, route, false, true, true, this));
                }
            }

            throw new UIFlowNavigationException(
                route.TargetChannel.ToString(),
                "AcquireExternalSceneBinding",
                route.RouteId.ToString(),
                "Add a matching external scene binding to the UIFlowHost.");
        }

        public Task ReleaseAsync(UIFlowScreenLease lease, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (lease != null && lease.Screen != null)
            {
                lease.Screen.SetVisibleImmediate(false, false);
                lease.Screen.ForceUnbound();
            }

            return Task.CompletedTask;
        }
    }
}
