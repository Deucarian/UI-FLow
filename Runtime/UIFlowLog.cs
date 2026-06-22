using Deucarian.Logging;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Package-level log categories for UI Flow.
    /// </summary>
    public static class UIFlowLog
    {
        public static readonly DLog General = DLog.For("UIFlow");
        public static readonly DLog Navigation = DLog.For("UIFlow.Navigation");
        public static readonly DLog Screens = DLog.For("UIFlow.Screens");
        public static readonly DLog UGUI = DLog.For("UIFlow.UGUI");
        public static readonly DLog Validation = DLog.For("UIFlow.Validation");
        public static readonly DLog Samples = DLog.For("UIFlow.Samples");
    }
}
