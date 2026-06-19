using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    public sealed class BasicFlowArgumentScreen : UIFlowScreen
    {
        [SerializeField] private Text _message;

        protected override Task OnPrepareAsync(UIFlowContext context, CancellationToken cancellationToken)
        {
            BasicFlowScreenArguments arguments;
            if (_message != null && context.TryGetArguments(out arguments))
            {
                _message.text = arguments.Message;
            }

            return Task.CompletedTask;
        }
    }
}
