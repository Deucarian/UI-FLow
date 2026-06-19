using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Samples/Login Redirect Guard", fileName = "BasicFlowLoginRedirectGuard")]
    public sealed class BasicFlowLoginRedirectGuard : UIFlowGuard
    {
        [SerializeField] private bool _loggedIn;
        [SerializeField] private UIFlowRoute _loginRoute;

        public override Task<UIFlowGuardResult> EvaluateAsync(UIFlowGuardContext context, CancellationToken cancellationToken)
        {
            if (_loggedIn || _loginRoute == null || context.TargetRoute == _loginRoute)
            {
                return Task.FromResult(UIFlowGuardResult.Allow());
            }

            return Task.FromResult(UIFlowGuardResult.Redirect(_loginRoute, null, "Sample redirects protected routes to Login."));
        }
    }
}
