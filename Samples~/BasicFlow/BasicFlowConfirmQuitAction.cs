using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using Deucarian.UIFlow.UGUI;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Samples/Confirm Quit Action", fileName = "BasicFlowConfirmQuitAction")]
    public sealed class BasicFlowConfirmQuitAction : UIFlowAction
    {
        [SerializeField] private UIFlowRoute _confirmQuitRoute;
        [SerializeField] private UIFlowPresentationOptions _options;

        public override async Task ExecuteAsync(UIFlowActionContext context, CancellationToken cancellationToken)
        {
            UIFlowHost host;
            if (!TryResolveHost(context, this, out host))
            {
                return;
            }

            if (_confirmQuitRoute == null)
            {
                UIFlowLog.Samples.Warning("BasicFlowConfirmQuitAction requires a confirm quit route.", this);
                return;
            }

            UIFlowPresentationResult<bool> result = await host.PresentAsync<bool>(_confirmQuitRoute, null, _options, cancellationToken);
            if (result.HasValue && result.Value)
            {
                UIFlowLog.Samples.Info("Quit confirmed by Basic Flow sample.");
            }
        }
    }
}
