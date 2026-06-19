using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Action asset that dismisses the containing active UI Flow screen entry.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Actions/Dismiss", fileName = "UIFlowDismissAction")]
    public sealed class UIFlowDismissAction : UIFlowAction
    {
        public override Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowScreen screen;
            if (!TryResolveScreen(context, this, out screen))
            {
                return Task.CompletedTask;
            }

            return screen.DismissAsync(cancellationToken);
        }
    }
}
