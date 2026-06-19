using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Immediate transition with no animation.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Transitions/Instant", fileName = "UIFlowInstantTransition")]
    public sealed class UIFlowInstantTransition : UIFlowTransition
    {
        public override Task ShowAsync(UIFlowTransitionContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyShown(context.Screen);
            return Task.CompletedTask;
        }

        public override Task HideAsync(UIFlowTransitionContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyHidden(context.Screen);
            return Task.CompletedTask;
        }

        public override void ForceShown(UIFlowTransitionContext context)
        {
            ApplyShown(context.Screen);
        }

        public override void ForceHidden(UIFlowTransitionContext context)
        {
            ApplyHidden(context.Screen);
        }
    }
}
