using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    internal sealed class UIFlowEntryPreparation
    {
        private readonly IUIFlowScreenEnvironment _environment;
        private readonly UIFlowNavigator _navigator;
        private readonly UIFlowChannelConfig _config;
        internal UIFlowEntryPreparation(IUIFlowScreenEnvironment environment, UIFlowNavigator navigator, UIFlowChannelConfig config)
        {
            _environment = environment;
            _navigator = navigator;
            _config = config;
        }

        internal async Task<UIFlowStackEntry> AcquirePreparedEntryAsync(
            UIFlowRoute route,
            object arguments,
            UIFlowNavigationReason reason,
            IUIFlowPresentationCompletion presentationCompletion,
            CancellationToken cancellationToken)
        {
            IUIFlowScreenProvider provider = _environment.GetProvider(route);
            var request = _environment.CreateScreenRequest(_navigator, route, _config.Root, arguments);
            UIFlowScreenLease lease = await provider.AcquireAsync(request, cancellationToken);
            UIFlowStackEntry entry = null;
            try
            {
                if (lease == null || lease.Screen == null)
                {
                    throw new UIFlowNavigationException(_navigator.ChannelId.ToString(), "Acquire", route.RouteId.ToString(), "The provider returned a null lease or screen.");
                }

                entry = new UIFlowStackEntry(route, lease, presentationCompletion == null ? null : new UIFlowPresentationState(presentationCompletion));
                entry.ReleaseProvider = lease.Provider ?? provider;
                if (presentationCompletion != null) presentationCompletion.SetEntryId(entry.EntryId);

                var context = _environment.CreateScreenContext(_navigator, route, entry.EntryId, arguments, entry.EntryLifetimeToken, reason);
                entry.Context = context;
                await lease.Screen.PrepareAsync(context, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return entry;
            }
            catch
            {
                if (entry != null) await UIFlowEntryLifetime.ReleaseAsync(entry, UIFlowDismissalReason.NavigationRejected);
                else await UIFlowEntryLifetime.ReleaseLeaseAsync(lease, lease?.Provider ?? provider);
                throw;
            }
        }
    }
}
