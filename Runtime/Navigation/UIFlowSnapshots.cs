using System;
using System.Collections.Generic;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Immutable snapshot for a stack entry.
    /// </summary>
    public sealed class UIFlowStackEntrySnapshot
    {
        public UIFlowStackEntrySnapshot(Guid entryId, UIFlowRouteId routeId, string routeName, UIFlowScreenLifecycleState state, bool hasPresentation)
        {
            EntryId = entryId;
            RouteId = routeId;
            RouteName = routeName;
            State = state;
            HasPresentation = hasPresentation;
        }

        public Guid EntryId { get; private set; }
        public UIFlowRouteId RouteId { get; private set; }
        public string RouteName { get; private set; }
        public UIFlowScreenLifecycleState State { get; private set; }
        public bool HasPresentation { get; private set; }
    }

    /// <summary>
    /// Immutable snapshot for a channel navigator.
    /// </summary>
    public sealed class UIFlowChannelSnapshot
    {
        public UIFlowChannelSnapshot(UIFlowChannelId channelId, UIFlowChannelKind kind, IReadOnlyList<UIFlowStackEntrySnapshot> stack)
        {
            ChannelId = channelId;
            Kind = kind;
            Stack = stack;
        }

        public UIFlowChannelId ChannelId { get; private set; }
        public UIFlowChannelKind Kind { get; private set; }
        public IReadOnlyList<UIFlowStackEntrySnapshot> Stack { get; private set; }
    }

    /// <summary>
    /// Immutable snapshot for the currently executing operation.
    /// </summary>
    public sealed class UIFlowOperationSnapshot
    {
        public UIFlowOperationSnapshot(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowNavigationReason reason,
            UIFlowChannelId channelId,
            UIFlowRouteId targetRouteId,
            string diagnosticLabel,
            TimeSpan queueDuration)
        {
            OperationId = operationId;
            OperationType = operationType;
            Reason = reason;
            ChannelId = channelId;
            TargetRouteId = targetRouteId;
            DiagnosticLabel = diagnosticLabel;
            QueueDuration = queueDuration;
        }

        public long OperationId { get; private set; }
        public UIFlowOperationType OperationType { get; private set; }
        public UIFlowNavigationReason Reason { get; private set; }
        public UIFlowChannelId ChannelId { get; private set; }
        public UIFlowRouteId TargetRouteId { get; private set; }
        public string DiagnosticLabel { get; private set; }
        public TimeSpan QueueDuration { get; private set; }
    }

    /// <summary>
    /// Immutable diagnostic snapshot for a host.
    /// </summary>
    public sealed class UIFlowHostSnapshot
    {
        public UIFlowHostSnapshot(
            string hostName,
            UIFlowInitializationState initializationState,
            IReadOnlyList<UIFlowChannelSnapshot> channels,
            UIFlowOperationSnapshot currentOperation,
            int queuedOperationCount,
            UIFlowNavigationResult lastResult,
            Exception lastFailure)
        {
            HostName = hostName;
            InitializationState = initializationState;
            Channels = channels;
            CurrentOperation = currentOperation;
            QueuedOperationCount = queuedOperationCount;
            LastResult = lastResult;
            LastFailure = lastFailure;
        }

        public string HostName { get; private set; }
        public UIFlowInitializationState InitializationState { get; private set; }
        public IReadOnlyList<UIFlowChannelSnapshot> Channels { get; private set; }
        public UIFlowOperationSnapshot CurrentOperation { get; private set; }
        public int QueuedOperationCount { get; private set; }
        public UIFlowNavigationResult LastResult { get; private set; }
        public Exception LastFailure { get; private set; }
    }
}
