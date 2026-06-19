using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Built-in provider for prefab-backed routes.
    /// </summary>
    public sealed class UIFlowPrefabScreenProvider : IUIFlowScreenProvider
    {
        public const string Id = "ui-flow-prefab";

        private readonly Dictionary<UIFlowRouteId, UIFlowScreenLease> _cache = new Dictionary<UIFlowRouteId, UIFlowScreenLease>();

        public string ProviderId
        {
            get { return Id; }
        }

        public Task<UIFlowScreenLease> AcquireAsync(UIFlowScreenRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UIFlowRoute route = request.Route;
            if (route.Prefab == null)
            {
                throw new UIFlowNavigationException(
                    route.TargetChannel.ToString(),
                    "AcquirePrefab",
                    route.RouteId.ToString(),
                    "Assign a UIFlowScreen prefab on the route asset.");
            }

            if (route.Lifetime == UIFlowScreenLifetime.Cached)
            {
                UIFlowScreenLease cached;
                if (_cache.TryGetValue(route.RouteId, out cached) && cached.Screen != null)
                {
                    cached.Screen.transform.SetParent(request.Parent, false);
                    cached.Screen.SetVisibleImmediate(false, false);
                    return Task.FromResult(cached);
                }
            }

            UIFlowScreen screen = Object.Instantiate(route.Prefab, request.Parent, false);
            screen.name = route.Prefab.name + " (" + route.DisplayName + ")";
            screen.SetVisibleImmediate(false, false);

            var lease = new UIFlowScreenLease(screen, route, true, false, route.Lifetime == UIFlowScreenLifetime.Cached, this);
            if (lease.IsCacheable && !_cache.ContainsKey(route.RouteId))
            {
                _cache.Add(route.RouteId, lease);
            }

            return Task.FromResult(lease);
        }

        public Task ReleaseAsync(UIFlowScreenLease lease, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (lease == null || lease.Screen == null)
            {
                return Task.CompletedTask;
            }

            if (lease.IsCacheable)
            {
                lease.Screen.SetVisibleImmediate(false, false);
                lease.Screen.ForceUnbound();
                return Task.CompletedTask;
            }

            if (lease.OwnsGameObject)
            {
                Object.Destroy(lease.Screen.gameObject);
            }

            return Task.CompletedTask;
        }
    }
}
