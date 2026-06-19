using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Context supplied to transition assets.
    /// </summary>
    public sealed class UIFlowTransitionContext
    {
        public UIFlowTransitionContext(
            UIFlowScreen screen,
            UIFlowRoute route,
            UIFlowChannelId channelId,
            UIFlowOperationType operationType,
            UIFlowTransitionDirection direction,
            bool animate)
        {
            Screen = screen;
            Route = route;
            ChannelId = channelId;
            OperationType = operationType;
            Direction = direction;
            Animate = animate;
        }

        public UIFlowScreen Screen { get; private set; }
        public UIFlowRoute Route { get; private set; }
        public UIFlowChannelId ChannelId { get; private set; }
        public UIFlowOperationType OperationType { get; private set; }
        public UIFlowTransitionDirection Direction { get; private set; }
        public bool Animate { get; private set; }
    }

    /// <summary>
    /// Stateless transition asset used by channels and routes.
    /// </summary>
    public abstract class UIFlowTransition : ScriptableObject
    {
        public abstract Task ShowAsync(UIFlowTransitionContext context, CancellationToken cancellationToken);
        public abstract Task HideAsync(UIFlowTransitionContext context, CancellationToken cancellationToken);
        public abstract void ForceShown(UIFlowTransitionContext context);
        public abstract void ForceHidden(UIFlowTransitionContext context);

        protected static void ApplyShown(UIFlowScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.SetVisibleImmediate(true, true);
        }

        protected static void ApplyHidden(UIFlowScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.SetVisibleImmediate(false, false);
        }
    }
}
