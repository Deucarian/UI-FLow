using System;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Result returned by a completed navigation request.
    /// </summary>
    public sealed class UIFlowNavigationResult
    {
        private UIFlowNavigationResult(
            UIFlowNavigationStatus status,
            long operationId,
            UIFlowOperationType operationType,
            UIFlowChannelId channelId,
            UIFlowRouteId routeId,
            string message)
        {
            Status = status;
            OperationId = operationId;
            OperationType = operationType;
            ChannelId = channelId;
            RouteId = routeId;
            Message = message;
        }

        public UIFlowNavigationStatus Status { get; private set; }
        public long OperationId { get; private set; }
        public UIFlowOperationType OperationType { get; private set; }
        public UIFlowChannelId ChannelId { get; private set; }
        public UIFlowRouteId RouteId { get; private set; }
        public string Message { get; private set; }

        public bool Succeeded
        {
            get { return Status == UIFlowNavigationStatus.Succeeded || Status == UIFlowNavigationStatus.NoOp; }
        }

        public static UIFlowNavigationResult SucceededResult(long operationId, UIFlowOperationType operationType, UIFlowChannelId channelId, UIFlowRouteId routeId, string message = null)
        {
            return new UIFlowNavigationResult(UIFlowNavigationStatus.Succeeded, operationId, operationType, channelId, routeId, message);
        }

        public static UIFlowNavigationResult NoOp(long operationId, UIFlowOperationType operationType, UIFlowChannelId channelId, UIFlowRouteId routeId, string message)
        {
            return new UIFlowNavigationResult(UIFlowNavigationStatus.NoOp, operationId, operationType, channelId, routeId, message);
        }

        public static UIFlowNavigationResult Rejected(long operationId, UIFlowOperationType operationType, UIFlowChannelId channelId, UIFlowRouteId routeId, string message)
        {
            return new UIFlowNavigationResult(UIFlowNavigationStatus.Rejected, operationId, operationType, channelId, routeId, message);
        }

        public static UIFlowNavigationResult GuardDenied(long operationId, UIFlowOperationType operationType, UIFlowChannelId channelId, UIFlowRouteId routeId, string message)
        {
            return new UIFlowNavigationResult(UIFlowNavigationStatus.GuardDenied, operationId, operationType, channelId, routeId, message);
        }

        public static UIFlowNavigationResult Cancelled(long operationId, UIFlowOperationType operationType, UIFlowChannelId channelId, UIFlowRouteId routeId, string message)
        {
            return new UIFlowNavigationResult(UIFlowNavigationStatus.Cancelled, operationId, operationType, channelId, routeId, message);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} channel={2} route={3} message={4}", Status, OperationType, ChannelId, RouteId, Message);
        }
    }

    /// <summary>
    /// Result returned by a typed modal presentation.
    /// </summary>
    /// <typeparam name="TResult">Typed value expected from the presented entry.</typeparam>
    public sealed class UIFlowPresentationResult<TResult>
    {
        private UIFlowPresentationResult(
            UIFlowPresentationStatus status,
            UIFlowDismissalReason dismissalReason,
            bool hasValue,
            TResult value,
            string message)
        {
            Status = status;
            DismissalReason = dismissalReason;
            HasValue = hasValue;
            _value = value;
            Message = message;
        }

        private readonly TResult _value;

        public UIFlowPresentationStatus Status { get; private set; }
        public UIFlowDismissalReason DismissalReason { get; private set; }
        public bool HasValue { get; private set; }
        public string Message { get; private set; }

        public TResult Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new UIFlowUsageException("Presentation did not complete with a typed value. Check HasValue before reading Value.");
                }

                return _value;
            }
        }

        public static UIFlowPresentationResult<TResult> Completed(TResult value)
        {
            return new UIFlowPresentationResult<TResult>(UIFlowPresentationStatus.Completed, UIFlowDismissalReason.None, true, value, null);
        }

        public static UIFlowPresentationResult<TResult> Dismissed(UIFlowDismissalReason reason, string message = null)
        {
            return new UIFlowPresentationResult<TResult>(UIFlowPresentationStatus.Dismissed, reason, false, default(TResult), message);
        }

        public static UIFlowPresentationResult<TResult> Cancelled(string message = null)
        {
            return new UIFlowPresentationResult<TResult>(UIFlowPresentationStatus.Cancelled, UIFlowDismissalReason.CallerCancelled, false, default(TResult), message);
        }

        public static UIFlowPresentationResult<TResult> HostDestroyed(string message = null)
        {
            return new UIFlowPresentationResult<TResult>(UIFlowPresentationStatus.HostDestroyed, UIFlowDismissalReason.HostDestroyed, false, default(TResult), message);
        }

        public static UIFlowPresentationResult<TResult> Removed(UIFlowDismissalReason reason, string message = null)
        {
            return new UIFlowPresentationResult<TResult>(UIFlowPresentationStatus.Removed, reason, false, default(TResult), message);
        }
    }
}
