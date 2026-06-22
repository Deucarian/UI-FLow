using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Action asset that presents a route and waits for its dismissal or result.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Actions/Present Route", fileName = "UIFlowPresentRouteAction")]
    public sealed class UIFlowPresentRouteAction : UIFlowAction
    {
        [SerializeField] private UIFlowRoute _route;
        [SerializeField] private UIFlowPresentationOptions _options;

        public override async Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowHost host;
            if (!TryResolveHost(context, this, out host))
            {
                return;
            }

            if (_route == null)
            {
                UIFlowLog.UGUI.Warning("UIFlowPresentRouteAction requires a route.", this);
                return;
            }

            await host.PresentAsync<object>(_route, null, _options, cancellationToken);
        }
    }
}
