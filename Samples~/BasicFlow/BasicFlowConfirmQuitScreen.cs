using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    public sealed class BasicFlowConfirmQuitScreen : UIFlowScreen
    {
        public void Confirm()
        {
            CloseAsync(true);
        }

        public void Cancel()
        {
            DismissAsync();
        }
    }
}
