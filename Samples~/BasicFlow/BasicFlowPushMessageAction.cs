using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using Deucarian.UIFlow.UGUI;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Samples/Push Message Route Action", fileName = "BasicFlowPushMessageAction")]
    public sealed class BasicFlowPushMessageAction : UIFlowAction
    {
        [SerializeField] private UIFlowRoute _route;
        [SerializeField] private string _message;
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
                Debug.LogWarning("BasicFlowPushMessageAction requires a route.", this);
                return Task.CompletedTask;
            }

            return host.PushAsync(_route, new BasicFlowScreenArguments(_message), _options, cancellationToken);
        }
    }
}
