using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>Convenience calls through a configured router, preserving navigation results and cancellation.</summary>
    public static class Screens
    {
        private static Registration current;
        public static bool IsConfigured => current != null;
        private static IUIFlowRouter Router => current?.Router ??
            throw new InvalidOperationException("Configure a ScreensHost before using Screens.");

        public static IDisposable Bind(IUIFlowRouter router)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            if (current != null) throw new InvalidOperationException("A default screen router is already registered.");
            return current = new Registration(router);
        }

        public static Task<UIFlowNavigationResult> OpenAsync(ScreenKey screen, object arguments = null,
            CancellationToken cancellationToken = default)
        {
            var router = Router;
            if (screen == null) throw new ArgumentNullException(nameof(screen), "Select a screen in the Inspector or pass a named ScreenKey.");
            string routeId = screen.Id;
            if (!router.TryGetRoute(new UIFlowRouteId(routeId), out var route))
                throw new KeyNotFoundException("Screens.OpenAsync cannot find '" + routeId + "' in the configured router. Add this route to the UIFlowHost route catalog.");
            return router.PushAsync(route, arguments, cancellationToken: cancellationToken);
        }

        public static Task<UIFlowNavigationResult> BackAsync(CancellationToken cancellationToken = default) =>
            Router.BackAsync(cancellationToken: cancellationToken);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { current = null; }

        private sealed class Registration : IDisposable
        {
            public Registration(IUIFlowRouter router) { Router = router; }
            public IUIFlowRouter Router { get; }
            public void Dispose() { if (ReferenceEquals(current, this)) current = null; }
        }
    }
}
