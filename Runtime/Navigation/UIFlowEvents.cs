using System;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Event payload emitted by UI Flow hosts.
    /// </summary>
    public sealed class UIFlowNavigationEventArgs : EventArgs
    {
        public UIFlowNavigationEventArgs(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowNavigationReason reason,
            UIFlowChannelId channelId,
            UIFlowRouteId sourceRouteId,
            UIFlowRouteId targetRouteId,
            UIFlowNavigationResult result,
            string message,
            Exception exception,
            TimeSpan queueDuration,
            TimeSpan executionDuration,
            string diagnosticLabel)
        {
            OperationId = operationId;
            OperationType = operationType;
            Reason = reason;
            ChannelId = channelId;
            SourceRouteId = sourceRouteId;
            TargetRouteId = targetRouteId;
            Result = result;
            Message = message;
            Exception = exception;
            QueueDuration = queueDuration;
            ExecutionDuration = executionDuration;
            DiagnosticLabel = diagnosticLabel;
        }

        public long OperationId { get; private set; }
        public UIFlowOperationType OperationType { get; private set; }
        public UIFlowNavigationReason Reason { get; private set; }
        public UIFlowChannelId ChannelId { get; private set; }
        public UIFlowRouteId SourceRouteId { get; private set; }
        public UIFlowRouteId TargetRouteId { get; private set; }
        public UIFlowNavigationResult Result { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; private set; }
        public TimeSpan QueueDuration { get; private set; }
        public TimeSpan ExecutionDuration { get; private set; }
        public string DiagnosticLabel { get; private set; }
    }

    /// <summary>
    /// Event payload emitted when a channel stack changes.
    /// </summary>
    public sealed class UIFlowStackChangedEventArgs : EventArgs
    {
        public UIFlowStackChangedEventArgs(UIFlowChannelSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public UIFlowChannelSnapshot Snapshot { get; private set; }
    }
}
