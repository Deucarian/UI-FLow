using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    internal interface IUIFlowScreenEnvironment
    {
        IUIFlowScreenProvider GetProvider(UIFlowRoute route);
        UIFlowScreenRequest CreateScreenRequest(UIFlowNavigator navigator, UIFlowRoute route, Transform parent, object arguments);
        UIFlowContext CreateScreenContext(UIFlowNavigator navigator, UIFlowRoute route, Guid entryId,
            object arguments, CancellationToken lifetime, UIFlowNavigationReason reason);
    }

    internal interface IUIFlowGuardEnvironment
    {
        int RedirectDepthLimit { get; }
        IReadOnlyList<UIFlowGuard> GlobalGuards { get; }
        UIFlowGuardContext CreateGuardContext(UIFlowNavigator navigator, UIFlowOperationType operation,
            UIFlowNavigationReason reason, UIFlowRoute current, UIFlowRoute target, object arguments);
        void NotifyNavigationRedirected(long operationId, UIFlowOperationType operation, UIFlowNavigationReason reason,
            UIFlowChannelId channel, UIFlowRouteId source, UIFlowRouteId target, string message, string diagnosticLabel);
    }

    internal sealed class UIFlowHostEnvironment : IUIFlowScreenEnvironment, IUIFlowGuardEnvironment
    {
        private readonly UIFlowHost _host;
        internal UIFlowHostEnvironment(UIFlowHost host) { _host = host ?? throw new ArgumentNullException(nameof(host)); }
        public IUIFlowScreenProvider GetProvider(UIFlowRoute route) => _host.GetProvider(route);
        public int RedirectDepthLimit => _host.RedirectDepthLimit;
        public IReadOnlyList<UIFlowGuard> GlobalGuards => _host.GlobalGuards;
        public UIFlowScreenRequest CreateScreenRequest(UIFlowNavigator navigator, UIFlowRoute route, Transform parent, object arguments)
            => new UIFlowScreenRequest(_host, navigator, route, parent, arguments);
        public UIFlowContext CreateScreenContext(UIFlowNavigator navigator, UIFlowRoute route, Guid entryId,
            object arguments, CancellationToken lifetime, UIFlowNavigationReason reason)
            => new UIFlowContext(_host, navigator, route, entryId, arguments, lifetime, reason);
        public UIFlowGuardContext CreateGuardContext(UIFlowNavigator navigator, UIFlowOperationType operation,
            UIFlowNavigationReason reason, UIFlowRoute current, UIFlowRoute target, object arguments)
            => new UIFlowGuardContext(_host, navigator, operation, reason, current, target, arguments);
        public void NotifyNavigationRedirected(long operationId, UIFlowOperationType operation, UIFlowNavigationReason reason,
            UIFlowChannelId channel, UIFlowRouteId source, UIFlowRouteId target, string message, string diagnosticLabel)
            => _host.NotifyNavigationRedirected(operationId, operation, reason, channel, source, target, message, diagnosticLabel);
        internal void NotifyStackChanged(UIFlowNavigator navigator) => _host.NotifyStackChanged(navigator);
    }
}
