using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Provider abstraction for resolving screens.
    /// </summary>
    public interface IUIFlowScreenProvider
    {
        string ProviderId { get; }

        Task<UIFlowScreenLease> AcquireAsync(UIFlowScreenRequest request, CancellationToken cancellationToken);

        Task ReleaseAsync(UIFlowScreenLease lease, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Request passed to a screen provider.
    /// </summary>
    public sealed class UIFlowScreenRequest
    {
        public UIFlowScreenRequest(UIFlowHost host, UIFlowNavigator navigator, UIFlowRoute route, Transform parent, object arguments)
        {
            Host = host;
            Navigator = navigator;
            Route = route;
            Parent = parent;
            Arguments = arguments;
        }

        public UIFlowHost Host { get; private set; }
        public UIFlowNavigator Navigator { get; private set; }
        public UIFlowRoute Route { get; private set; }
        public Transform Parent { get; private set; }
        public object Arguments { get; private set; }
    }

    /// <summary>
    /// Lease returned by a screen provider.
    /// </summary>
    public sealed class UIFlowScreenLease
    {
        public UIFlowScreenLease(UIFlowScreen screen, UIFlowRoute route, bool ownsGameObject, bool isExternal, bool isCacheable, IUIFlowScreenProvider provider)
        {
            if (screen == null)
            {
                throw new ArgumentNullException(nameof(screen));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            Screen = screen;
            Route = route;
            OwnsGameObject = ownsGameObject;
            IsExternal = isExternal;
            IsCacheable = isCacheable;
            Provider = provider;
        }

        public UIFlowScreen Screen { get; private set; }
        public UIFlowRoute Route { get; private set; }
        public bool OwnsGameObject { get; private set; }
        public bool IsExternal { get; private set; }
        public bool IsCacheable { get; private set; }
        public IUIFlowScreenProvider Provider { get; private set; }
    }
}
