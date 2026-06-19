using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Action asset that pushes a route.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Actions/Push Route", fileName = "UIFlowPushRouteAction")]
    public sealed class UIFlowPushRouteAction : UIFlowAction
    {
        [SerializeField] private UIFlowRoute _route;
        [SerializeField] private UIFlowNavigationOptions _options;

        public UIFlowRoute Route
        {
            get { return _route; }
        }

        public override Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowHost host;
            if (!TryResolveHost(context, this, out host))
            {
                return Task.CompletedTask;
            }

            if (_route == null)
            {
                Debug.LogWarning("UIFlowPushRouteAction requires a route.", this);
                return Task.CompletedTask;
            }

            return host.PushAsync(_route, null, _options, cancellationToken);
        }
    }
}
