using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Action asset that replaces the current route.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Actions/Replace Route", fileName = "UIFlowReplaceRouteAction")]
    public sealed class UIFlowReplaceRouteAction : UIFlowAction
    {
        [SerializeField] private UIFlowRoute _route;
        [SerializeField] private UIFlowNavigationOptions _options;

        public override Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowHost host;
            if (!TryResolveHost(context, this, out host))
            {
                return Task.CompletedTask;
            }

            if (_route == null)
            {
                Debug.LogWarning("UIFlowReplaceRouteAction requires a route.", this);
                return Task.CompletedTask;
            }

            return host.ReplaceAsync(_route, null, _options, cancellationToken);
        }
    }
}
