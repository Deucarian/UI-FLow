using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Read-only channel navigator runtime owned by a UI Flow host.
    /// </summary>
    public sealed class UIFlowNavigator
    {
        private readonly Action<UIFlowNavigator> _notifyStackChanged;
        private readonly UIFlowGuardPipeline _guards;
        private readonly UIFlowEntryPreparation _preparation;
        private readonly UIFlowTransitionDriver _transitions;
        private readonly UIFlowChannelConfig _config;
        private readonly UIFlowStackState _stack = new UIFlowStackState();

        internal UIFlowNavigator(UIFlowHost host, UIFlowChannelConfig config)
            : this(new UIFlowHostEnvironment(host), config) { }

        private UIFlowNavigator(UIFlowHostEnvironment environment, UIFlowChannelConfig config)
            : this(environment, environment, environment.NotifyStackChanged, config) { }

        internal UIFlowNavigator(IUIFlowScreenEnvironment screens, IUIFlowGuardEnvironment guards,
            Action<UIFlowNavigator> notifyStackChanged, UIFlowChannelConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _notifyStackChanged = notifyStackChanged;
            _guards = new UIFlowGuardPipeline(guards ?? throw new ArgumentNullException(nameof(guards)), this, _stack.TopEntry);
            _preparation = new UIFlowEntryPreparation(screens ?? throw new ArgumentNullException(nameof(screens)), this, config);
            _transitions = new UIFlowTransitionDriver(config);
        }

        public UIFlowChannelId ChannelId => _config.ChannelId;

        public UIFlowChannelKind Kind => _config.Kind;

        public int Count => _stack.Count;

        public UIFlowRoute CurrentRoute => _stack.Count == 0 ? null : _stack[_stack.Count - 1].Route;

        public IReadOnlyList<UIFlowStackEntrySnapshot> Stack => _stack.CreateStackSnapshot();

        public bool CanHandleBack => _config.ParticipatesInBack && _stack.Count > 0 &&
                                     (_stack.Count > 1 || !_config.ProtectRootFromBack);

        internal UIFlowChannelConfig Config => _config;

        internal bool ContainsEntry(Guid entryId)
        {
            return _stack.FindEntryIndex(entryId) >= 0;
        }

        internal UIFlowChannelSnapshot CreateSnapshot()
        {
            return new UIFlowChannelSnapshot(ChannelId, Kind, _stack.CreateStackSnapshot());
        }

        internal async Task<UIFlowNavigationResult> PushAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowRoute route,
            object arguments,
            UIFlowNavigationOptions options,
            IUIFlowPresentationCompletion presentationCompletion,
            CancellationToken cancellationToken)
        {
            options = options.Normalize();
            if (route == null)
            {
                return UIFlowNavigationResult.Rejected(operationId, operationType, ChannelId, default(UIFlowRouteId), "Push requires a route.");
            }

            UIFlowGuardPipeline.GuardResolution resolution = await _guards.ResolveGuardsAsync(operationId, operationType, route, arguments, options, cancellationToken);
            if (resolution.Result != null)
            {
                return resolution.Result;
            }

            route = resolution.Route;
            arguments = resolution.Arguments;

            UIFlowNavigationResult duplicateResult = await HandleDuplicateAsync(operationId, operationType, route, options, cancellationToken);
            if (duplicateResult != null)
            {
                return duplicateResult;
            }

            UIFlowStackEntry previous = _stack.TopEntry();
            UIFlowStackEntry next = null;

            try
            {
                next = await _preparation.AcquirePreparedEntryAsync(route, arguments, options.Reason, presentationCompletion, cancellationToken);

                if (previous != null)
                {
                    await previous.Screen.BeforeCoverAsync(cancellationToken);
                }

                await next.Screen.BeforeEnterAsync(cancellationToken);

                await _transitions.RunCoverAndShowAsync(previous, next, operationType, options, cancellationToken);

                _stack.Add(next);

                if (previous != null)
                {
                    previous.Screen.AfterCover();
                }

                next.Screen.AfterEnter();
                NotifyStackChanged();
                return UIFlowNavigationResult.SucceededResult(operationId, operationType, ChannelId, route.RouteId);
            }
            catch (OperationCanceledException)
            {
                await _transitions.RollbackAfterFailedEntryAsync(previous, next);
                return UIFlowNavigationResult.Cancelled(operationId, operationType, ChannelId, route.RouteId, "Push was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                await _transitions.RollbackAfterFailedEntryAsync(previous, next);
                throw CreateException(operationType, route, "Push failed; the previous stack state was restored.", ex);
            }
        }

        internal async Task<UIFlowNavigationResult> PopAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            options = options.Normalize();
            if (_stack.Count == 0)
            {
                return UIFlowNavigationResult.NoOp(operationId, operationType, ChannelId, default(UIFlowRouteId), "Channel stack is empty.");
            }

            if (_stack.Count == 1 && _config.ProtectRootFromBack)
            {
                return UIFlowNavigationResult.NoOp(operationId, operationType, ChannelId, _stack[0].Route.RouteId, "The root entry is protected from Back/Pop.");
            }

            UIFlowStackEntry removed = _stack[_stack.Count - 1];
            UIFlowStackEntry revealed = _stack.Count > 1 ? _stack[_stack.Count - 2] : null;

            UIFlowNavigationResult guardResult = await _guards.EvaluateExitGuardsAsync(operationId, operationType, removed.Route, null, null, options, cancellationToken);
            if (guardResult != null)
            {
                return guardResult;
            }

            try
            {
                await removed.Screen.BeforeExitAsync(cancellationToken);
                if (revealed != null)
                {
                    await revealed.Screen.BeforeRevealAsync(cancellationToken);
                }

                await _transitions.RunHideAndRevealAsync(removed, revealed, operationType, options, cancellationToken);

                _stack.RemoveAt(_stack.Count - 1);
                removed.Screen.AfterExit();
                if (revealed != null)
                {
                    revealed.Screen.AfterReveal();
                }

                await ReleaseEntryAsync(removed, GetDismissalReasonForPop(operationType), cancellationToken);
                NotifyStackChanged();
                return UIFlowNavigationResult.SucceededResult(operationId, operationType, ChannelId, removed.Route.RouteId);
            }
            catch (OperationCanceledException)
            {
                _transitions.ForceEntryShown(removed);
                _transitions.ForceEntryCovered(revealed);
                return UIFlowNavigationResult.Cancelled(operationId, operationType, ChannelId, removed.Route.RouteId, "Pop was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                _transitions.ForceEntryShown(removed);
                _transitions.ForceEntryCovered(revealed);
                throw CreateException(operationType, removed.Route, "Pop failed; the previous stack state was restored.", ex);
            }
        }

        internal async Task<UIFlowNavigationResult> ReplaceAsync(
            long operationId,
            UIFlowRoute route,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            options = options.Normalize();
            if (_stack.Count == 0)
            {
                return await PushAsync(operationId, UIFlowOperationType.Replace, route, arguments, options, null, cancellationToken);
            }

            UIFlowGuardPipeline.GuardResolution resolution = await _guards.ResolveGuardsAsync(operationId, UIFlowOperationType.Replace, route, arguments, options, cancellationToken);
            if (resolution.Result != null)
            {
                return resolution.Result;
            }

            route = resolution.Route;
            arguments = resolution.Arguments;

            UIFlowNavigationResult duplicateResult = await HandleDuplicateAsync(operationId, UIFlowOperationType.Replace, route, options, cancellationToken);
            if (duplicateResult != null)
            {
                return duplicateResult;
            }

            UIFlowStackEntry current = _stack[_stack.Count - 1];
            UIFlowStackEntry next = null;

            try
            {
                next = await _preparation.AcquirePreparedEntryAsync(route, arguments, options.Reason, null, cancellationToken);
                await current.Screen.BeforeExitAsync(cancellationToken);
                await next.Screen.BeforeEnterAsync(cancellationToken);

                await _transitions.RunReplaceAsync(current, next, UIFlowOperationType.Replace, options, cancellationToken);

                _stack[_stack.Count - 1] = next;
                current.Screen.AfterExit();
                next.Screen.AfterEnter();
                await ReleaseEntryAsync(current, UIFlowDismissalReason.RemovedByReplacementOrReset, cancellationToken);
                NotifyStackChanged();
                return UIFlowNavigationResult.SucceededResult(operationId, UIFlowOperationType.Replace, ChannelId, route.RouteId);
            }
            catch (OperationCanceledException)
            {
                await _transitions.RollbackAfterFailedEntryAsync(current, next);
                return UIFlowNavigationResult.Cancelled(operationId, UIFlowOperationType.Replace, ChannelId, route.RouteId, "Replace was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                await _transitions.RollbackAfterFailedEntryAsync(current, next);
                throw CreateException(UIFlowOperationType.Replace, route, "Replace failed; the previous stack state was restored.", ex);
            }
        }

        internal async Task<UIFlowNavigationResult> ResetAsync(
            long operationId,
            UIFlowRoute rootRoute,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            options = options.Normalize();
            UIFlowGuardPipeline.GuardResolution resolution = null;

            if (rootRoute != null)
            {
                resolution = await _guards.ResolveGuardsAsync(operationId, UIFlowOperationType.Reset, rootRoute, arguments, options, cancellationToken);
                if (resolution.Result != null)
                {
                    return resolution.Result;
                }

                rootRoute = resolution.Route;
                arguments = resolution.Arguments;
            }
            else
            {
                UIFlowNavigationResult guardResult = await _guards.EvaluateExitGuardsAsync(operationId, UIFlowOperationType.Reset, CurrentRoute, null, arguments, options, cancellationToken);
                if (guardResult != null)
                {
                    return guardResult;
                }
            }

            UIFlowStackEntry current = _stack.TopEntry();
            UIFlowStackEntry root = null;
            UIFlowRouteId resultRouteId = rootRoute == null ? default(UIFlowRouteId) : rootRoute.RouteId;

            try
            {
                if (rootRoute != null)
                {
                    root = await _preparation.AcquirePreparedEntryAsync(rootRoute, arguments, options.Reason, null, cancellationToken);
                    await root.Screen.BeforeEnterAsync(cancellationToken);
                }

                if (current != null)
                {
                    await current.Screen.BeforeExitAsync(cancellationToken);
                }

                await _transitions.RunResetAsync(current, root, options, cancellationToken);

                List<UIFlowStackEntry> removed = new List<UIFlowStackEntry>(_stack);
                _stack.Clear();
                if (root != null)
                {
                    _stack.Add(root);
                    root.Screen.AfterEnter();
                }

                for (int i = 0; i < removed.Count; i++)
                {
                    removed[i].Screen.AfterExit();
                    await ReleaseEntryAsync(removed[i], UIFlowDismissalReason.RemovedByReplacementOrReset, cancellationToken);
                }

                NotifyStackChanged();
                return UIFlowNavigationResult.SucceededResult(operationId, UIFlowOperationType.Reset, ChannelId, resultRouteId);
            }
            catch (OperationCanceledException)
            {
                await _transitions.RollbackAfterFailedEntryAsync(current, root);
                return UIFlowNavigationResult.Cancelled(operationId, UIFlowOperationType.Reset, ChannelId, resultRouteId, "Reset was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                await _transitions.RollbackAfterFailedEntryAsync(current, root);
                throw CreateException(UIFlowOperationType.Reset, rootRoute, "Reset failed; the previous stack state was restored.", ex);
            }
        }

        internal async Task<UIFlowNavigationResult> PopToAsync(
            long operationId,
            UIFlowRouteId routeId,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            options = options.Normalize();
            int targetIndex = _stack.FindRouteIndex(routeId);
            if (targetIndex < 0)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.PopTo, ChannelId, routeId, "Route is not present in the channel stack.");
            }

            if (targetIndex == _stack.Count - 1)
            {
                return UIFlowNavigationResult.NoOp(operationId, UIFlowOperationType.PopTo, ChannelId, routeId, "Requested route is already top.");
            }

            UIFlowStackEntry removedTop = _stack[_stack.Count - 1];
            UIFlowStackEntry revealed = _stack[targetIndex];
            UIFlowNavigationResult guardResult = await _guards.EvaluateExitGuardsAsync(operationId, UIFlowOperationType.PopTo, removedTop.Route, revealed.Route, null, options, cancellationToken);
            if (guardResult != null)
            {
                return guardResult;
            }

            try
            {
                await removedTop.Screen.BeforeExitAsync(cancellationToken);
                await revealed.Screen.BeforeRevealAsync(cancellationToken);
                await _transitions.RunHideAndRevealAsync(removedTop, revealed, UIFlowOperationType.PopTo, options, cancellationToken);

                List<UIFlowStackEntry> removed = _stack.GetRange(targetIndex + 1, _stack.Count - targetIndex - 1);
                _stack.RemoveRange(targetIndex + 1, _stack.Count - targetIndex - 1);

                for (int i = 0; i < removed.Count; i++)
                {
                    removed[i].Screen.AfterExit();
                    await ReleaseEntryAsync(removed[i], UIFlowDismissalReason.EntryReleased, cancellationToken);
                }

                revealed.Screen.AfterReveal();
                NotifyStackChanged();
                return UIFlowNavigationResult.SucceededResult(operationId, UIFlowOperationType.PopTo, ChannelId, routeId);
            }
            catch (OperationCanceledException)
            {
                _transitions.ForceEntryShown(removedTop);
                _transitions.ForceEntryCovered(revealed);
                return UIFlowNavigationResult.Cancelled(operationId, UIFlowOperationType.PopTo, ChannelId, routeId, "PopTo was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                _transitions.ForceEntryShown(removedTop);
                _transitions.ForceEntryCovered(revealed);
                throw CreateException(UIFlowOperationType.PopTo, revealed.Route, "PopTo failed; the previous stack state was restored.", ex);
            }
        }

        internal async Task<UIFlowNavigationResult> CloseEntryAsync(
            long operationId,
            Guid entryId,
            bool hasValue,
            object value,
            Type valueType,
            UIFlowDismissalReason dismissalReason,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            UIFlowNavigationResult rejected = UIFlowDismissalPolicy.Request(
                _stack, ChannelId, operationId, entryId, hasValue, value, valueType, dismissalReason);
            return rejected ?? await PopAsync(operationId, UIFlowOperationType.Dismiss, options, cancellationToken);
        }

        internal async Task ShutdownAsync()
        {
            List<UIFlowStackEntry> entries = new List<UIFlowStackEntry>(_stack);
            _stack.Clear();

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                await ReleaseEntryAsync(entries[i], UIFlowDismissalReason.HostDestroyed, CancellationToken.None);
            }

            NotifyStackChanged();
        }

        private async Task<UIFlowNavigationResult> HandleDuplicateAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowRoute route,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            int existingIndex = _stack.FindRouteIndex(route.RouteId);
            bool exists = existingIndex >= 0;
            bool isTop = exists && existingIndex == _stack.Count - 1;

            if (isTop && route.DuplicateRoutePolicy == UIFlowDuplicateRoutePolicy.IgnoreIfTop)
            {
                return UIFlowNavigationResult.NoOp(operationId, operationType, ChannelId, route.RouteId, "Requested route is already top.");
            }

            if (exists && route.DuplicateRoutePolicy == UIFlowDuplicateRoutePolicy.PopToExisting)
            {
                return await PopToAsync(operationId, route.RouteId, options, cancellationToken);
            }

            if (exists && route.DuplicateRoutePolicy == UIFlowDuplicateRoutePolicy.RejectIfPresent)
            {
                return UIFlowNavigationResult.Rejected(operationId, operationType, ChannelId, route.RouteId, "Route is already present in the stack.");
            }

            if (exists && route.Lifetime != UIFlowScreenLifetime.Transient)
            {
                return UIFlowNavigationResult.Rejected(operationId, operationType, ChannelId, route.RouteId, "Cached and external routes cannot have duplicate active entries.");
            }

            return null;
        }

        private Task ReleaseEntryAsync(UIFlowStackEntry entry, UIFlowDismissalReason reason, CancellationToken cancellationToken) =>
            UIFlowEntryLifetime.ReleaseAsync(entry, reason);

        private UIFlowDismissalReason GetDismissalReasonForPop(UIFlowOperationType operationType)
        {
            if (operationType == UIFlowOperationType.Back)
            {
                return UIFlowDismissalReason.Back;
            }

            if (operationType == UIFlowOperationType.Dismiss)
            {
                return UIFlowDismissalReason.Dismissed;
            }

            return UIFlowDismissalReason.EntryReleased;
        }

        private UIFlowNavigationException CreateException(UIFlowOperationType operationType, UIFlowRoute route, string remediation, Exception inner)
        {
            return new UIFlowNavigationException(ChannelId.ToString(), operationType.ToString(), route == null ? "<none>" : route.RouteId.ToString(), remediation, inner);
        }


        private void NotifyStackChanged()
        {
            try { _notifyStackChanged?.Invoke(this); }
            catch (Exception exception)
            {
                UIFlowLog.Navigation.Exception(CreateException(UIFlowOperationType.System, CurrentRoute,
                    "Navigation committed, but an observer failed. Refresh the affected view.", exception));
            }
        }

    }

}
