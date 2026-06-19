using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Samples/Deny Guard", fileName = "BasicFlowDenyGuard")]
    public sealed class BasicFlowDenyGuard : UIFlowGuard
    {
        [SerializeField] private string _reason = "Sample guard denied this route.";

        public override Task<UIFlowGuardResult> EvaluateAsync(UIFlowGuardContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(UIFlowGuardResult.Deny(_reason));
        }
    }
}
