using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Action asset that requests host-level Back routing.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Actions/Back", fileName = "UIFlowBackAction")]
    public sealed class UIFlowBackAction : UIFlowAction
    {
        [SerializeField] private UIFlowNavigationOptions _options;

        public override Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowHost host;
            if (!TryResolveHost(context, this, out host))
            {
                return Task.CompletedTask;
            }

            return host.BackAsync(_options, cancellationToken);
        }
    }
}
