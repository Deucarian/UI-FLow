using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Base class for reusable UI Flow action assets executed by UI event binders.
    /// </summary>
    public abstract class UIFlowAction : ScriptableObject
    {
        public abstract Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken);

        protected static bool TryResolveHost(UIFlowActionContext context, Object logContext, out UIFlowHost host)
        {
            if (context != null && context.TryGetHost(out host))
            {
                return true;
            }

            UIFlowLog.UGUI.Warning("UI Flow action requires an explicit UIFlowHost or a parent UIFlowHost.", logContext);
            host = null;
            return false;
        }

        protected static bool TryResolveScreen(UIFlowActionContext context, Object logContext, out UIFlowScreen screen)
        {
            if (context != null && context.TryGetScreen(out screen))
            {
                return true;
            }

            UIFlowLog.UGUI.Warning("UI Flow dismiss action must run from inside an active UIFlowScreen context.", logContext);
            screen = null;
            return false;
        }
    }
}
