using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Action asset that resets a channel, optionally to a root route.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Actions/Reset Route", fileName = "UIFlowResetRouteAction")]
    public sealed class UIFlowResetRouteAction : UIFlowAction
    {
        [SerializeField] private UIFlowRoute _rootRoute;
        [SerializeField] private UIFlowChannelId _channel = UIFlowChannelId.Main;
        [SerializeField] private UIFlowNavigationOptions _options;

        public override Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowHost host;
            if (!TryResolveHost(context, this, out host))
            {
                return Task.CompletedTask;
            }

            UIFlowChannelId channel = _rootRoute == null ? _channel : _rootRoute.TargetChannel;
            if (channel.IsEmpty)
            {
                UIFlowLog.UGUI.Warning("UIFlowResetRouteAction requires a channel when no root route is assigned.", this);
                return Task.CompletedTask;
            }

            return host.ResetAsync(channel, _rootRoute, null, _options, cancellationToken);
        }
    }
}
