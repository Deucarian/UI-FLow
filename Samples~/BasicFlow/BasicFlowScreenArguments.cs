using System;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    [Serializable]
    public sealed class BasicFlowScreenArguments
    {
        public BasicFlowScreenArguments(string message)
        {
            Message = message;
        }

        public string Message;
    }
}
