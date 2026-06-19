using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Navigation context assigned to a screen while a stack entry is leased.
    /// </summary>
    public sealed class UIFlowContext
    {
        internal UIFlowContext(
            UIFlowHost host,
            UIFlowNavigator navigator,
            UIFlowRoute route,
            Guid entryId,
            object arguments,
            CancellationToken entryLifetimeToken,
            UIFlowNavigationReason reason)
        {
            Host = host;
            Navigator = navigator;
            Route = route;
            EntryId = entryId;
            Arguments = arguments;
            EntryLifetimeToken = entryLifetimeToken;
            Reason = reason;
        }

        public UIFlowHost Host { get; private set; }
        public IUIFlowRouter Router
        {
            get { return Host; }
        }

        public UIFlowNavigator Navigator { get; private set; }
        public UIFlowRoute Route { get; private set; }
        public Guid EntryId { get; private set; }
        public object Arguments { get; private set; }
        public CancellationToken EntryLifetimeToken { get; private set; }
        public UIFlowNavigationReason Reason { get; private set; }

        public bool TryGetArguments<T>(out T value)
        {
            if (Arguments is T)
            {
                value = (T)Arguments;
                return true;
            }

            value = default(T);
            return false;
        }

        public T GetRequiredArguments<T>()
        {
            T value;
            if (TryGetArguments<T>(out value))
            {
                return value;
            }

            string actual = Arguments == null ? "<null>" : Arguments.GetType().FullName;
            throw new UIFlowArgumentException("Route '" + Route.RouteId + "' expected arguments of type '" + typeof(T).FullName + "' but received '" + actual + "'.");
        }

        public Task<UIFlowNavigationResult> CloseAsync<TResult>(TResult result, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Host.CloseEntryAsync(EntryId, true, result, typeof(TResult), UIFlowDismissalReason.None, cancellationToken);
        }

        public Task<UIFlowNavigationResult> DismissAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Host.CloseEntryAsync(EntryId, false, null, null, UIFlowDismissalReason.Dismissed, cancellationToken);
        }
    }
}
