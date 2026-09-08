using System;

namespace Deucarian.UIFlow
{
    internal static class UIFlowDismissalPolicy
    {
        internal static UIFlowNavigationResult Request(UIFlowStackState stack, UIFlowChannelId channelId,
            long operationId, Guid entryId, bool hasValue, object value, Type valueType, UIFlowDismissalReason dismissalReason)
        {
            int index = stack.FindEntryIndex(entryId);
            if (index < 0)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, channelId, default(UIFlowRouteId), "The entry has already been removed.");
            }

            if (index != stack.Count - 1)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, channelId, stack[index].Route.RouteId, "Only the top entry in its channel can close itself.");
            }

            UIFlowStackEntry entry = stack[index];
            if (entry.Presentation != null)
            {
                string error;
                bool accepted = hasValue
                    ? entry.Presentation.TryRequestValue(value, valueType, out error)
                    : entry.Presentation.TryRequestDismissal(dismissalReason, out error);

                if (!accepted)
                {
                    return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, channelId, entry.Route.RouteId, error);
                }
            }
            else if (hasValue)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, channelId, entry.Route.RouteId, "Only presented entries can close with a typed result.");
            }

            return null;
        }
    }
}
