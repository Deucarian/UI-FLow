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
        private readonly UIFlowHost _host;
        private readonly UIFlowChannelConfig _config;
        private readonly List<UIFlowStackEntry> _stack = new List<UIFlowStackEntry>();

        internal UIFlowNavigator(UIFlowHost host, UIFlowChannelConfig config)
        {
            _host = host;
            _config = config;
        }

        public UIFlowChannelId ChannelId
        {
            get { return _config.ChannelId; }
        }

        public UIFlowChannelKind Kind
        {
            get { return _config.Kind; }
        }

        public int Count
        {
            get { return _stack.Count; }
        }

        public UIFlowRoute CurrentRoute
        {
            get { return _stack.Count == 0 ? null : _stack[_stack.Count - 1].Route; }
        }

        public IReadOnlyList<UIFlowStackEntrySnapshot> Stack
        {
            get { return CreateStackSnapshot(); }
        }

        public bool CanHandleBack
        {
            get
            {
                if (!_config.ParticipatesInBack || _stack.Count == 0)
                {
                    return false;
                }

                if (_stack.Count == 1 && _config.ProtectRootFromBack)
                {
                    return false;
                }

                return true;
            }
        }

        internal UIFlowChannelConfig Config
        {
            get { return _config; }
        }

        internal bool ContainsEntry(Guid entryId)
        {
            return FindEntryIndex(entryId) >= 0;
        }

        internal UIFlowChannelSnapshot CreateSnapshot()
        {
            return new UIFlowChannelSnapshot(ChannelId, Kind, CreateStackSnapshot());
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

            GuardResolution resolution = await ResolveGuardsAsync(operationId, operationType, route, arguments, options, cancellationToken);
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

            UIFlowStackEntry previous = TopEntry();
            UIFlowStackEntry next = null;

            try
            {
                next = await AcquirePreparedEntryAsync(route, arguments, options.Reason, presentationCompletion, cancellationToken);

                if (previous != null)
                {
                    await previous.Screen.BeforeCoverAsync(cancellationToken);
                }

                await next.Screen.BeforeEnterAsync(cancellationToken);

                await RunCoverAndShowAsync(previous, next, operationType, options, cancellationToken);

                _stack.Add(next);

                if (previous != null)
                {
                    previous.Screen.AfterCover();
                }

                next.Screen.AfterEnter();
                _host.NotifyStackChanged(this);
                return UIFlowNavigationResult.SucceededResult(operationId, operationType, ChannelId, route.RouteId);
            }
            catch (OperationCanceledException)
            {
                await RollbackAfterFailedEntryAsync(previous, next);
                return UIFlowNavigationResult.Cancelled(operationId, operationType, ChannelId, route.RouteId, "Push was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                await RollbackAfterFailedEntryAsync(previous, next);
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

            UIFlowNavigationResult guardResult = await EvaluateExitGuardsAsync(operationId, operationType, removed.Route, null, null, options, cancellationToken);
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

                await RunHideAndRevealAsync(removed, revealed, operationType, options, cancellationToken);

                _stack.RemoveAt(_stack.Count - 1);
                removed.Screen.AfterExit();
                if (revealed != null)
                {
                    revealed.Screen.AfterReveal();
                }

                await ReleaseEntryAsync(removed, GetDismissalReasonForPop(operationType), cancellationToken);
                _host.NotifyStackChanged(this);
                return UIFlowNavigationResult.SucceededResult(operationId, operationType, ChannelId, removed.Route.RouteId);
            }
            catch (OperationCanceledException)
            {
                ForceEntryShown(removed);
                ForceEntryCovered(revealed);
                return UIFlowNavigationResult.Cancelled(operationId, operationType, ChannelId, removed.Route.RouteId, "Pop was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                ForceEntryShown(removed);
                ForceEntryCovered(revealed);
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

            GuardResolution resolution = await ResolveGuardsAsync(operationId, UIFlowOperationType.Replace, route, arguments, options, cancellationToken);
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
                next = await AcquirePreparedEntryAsync(route, arguments, options.Reason, null, cancellationToken);
                await current.Screen.BeforeExitAsync(cancellationToken);
                await next.Screen.BeforeEnterAsync(cancellationToken);

                await RunReplaceAsync(current, next, UIFlowOperationType.Replace, options, cancellationToken);

                _stack[_stack.Count - 1] = next;
                current.Screen.AfterExit();
                next.Screen.AfterEnter();
                await ReleaseEntryAsync(current, UIFlowDismissalReason.RemovedByReplacementOrReset, cancellationToken);
                _host.NotifyStackChanged(this);
                return UIFlowNavigationResult.SucceededResult(operationId, UIFlowOperationType.Replace, ChannelId, route.RouteId);
            }
            catch (OperationCanceledException)
            {
                await RollbackAfterFailedEntryAsync(current, next);
                return UIFlowNavigationResult.Cancelled(operationId, UIFlowOperationType.Replace, ChannelId, route.RouteId, "Replace was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                await RollbackAfterFailedEntryAsync(current, next);
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
            GuardResolution resolution = null;

            if (rootRoute != null)
            {
                resolution = await ResolveGuardsAsync(operationId, UIFlowOperationType.Reset, rootRoute, arguments, options, cancellationToken);
                if (resolution.Result != null)
                {
                    return resolution.Result;
                }

                rootRoute = resolution.Route;
                arguments = resolution.Arguments;
            }
            else
            {
                UIFlowNavigationResult guardResult = await EvaluateExitGuardsAsync(operationId, UIFlowOperationType.Reset, CurrentRoute, null, arguments, options, cancellationToken);
                if (guardResult != null)
                {
                    return guardResult;
                }
            }

            UIFlowStackEntry current = TopEntry();
            UIFlowStackEntry root = null;
            UIFlowRouteId resultRouteId = rootRoute == null ? default(UIFlowRouteId) : rootRoute.RouteId;

            try
            {
                if (rootRoute != null)
                {
                    root = await AcquirePreparedEntryAsync(rootRoute, arguments, options.Reason, null, cancellationToken);
                    await root.Screen.BeforeEnterAsync(cancellationToken);
                }

                if (current != null)
                {
                    await current.Screen.BeforeExitAsync(cancellationToken);
                }

                await RunResetAsync(current, root, options, cancellationToken);

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

                _host.NotifyStackChanged(this);
                return UIFlowNavigationResult.SucceededResult(operationId, UIFlowOperationType.Reset, ChannelId, resultRouteId);
            }
            catch (OperationCanceledException)
            {
                await RollbackAfterFailedEntryAsync(current, root);
                return UIFlowNavigationResult.Cancelled(operationId, UIFlowOperationType.Reset, ChannelId, resultRouteId, "Reset was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                await RollbackAfterFailedEntryAsync(current, root);
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
            int targetIndex = FindRouteIndex(routeId);
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
            UIFlowNavigationResult guardResult = await EvaluateExitGuardsAsync(operationId, UIFlowOperationType.PopTo, removedTop.Route, revealed.Route, null, options, cancellationToken);
            if (guardResult != null)
            {
                return guardResult;
            }

            try
            {
                await removedTop.Screen.BeforeExitAsync(cancellationToken);
                await revealed.Screen.BeforeRevealAsync(cancellationToken);
                await RunHideAndRevealAsync(removedTop, revealed, UIFlowOperationType.PopTo, options, cancellationToken);

                List<UIFlowStackEntry> removed = _stack.GetRange(targetIndex + 1, _stack.Count - targetIndex - 1);
                _stack.RemoveRange(targetIndex + 1, _stack.Count - targetIndex - 1);

                for (int i = 0; i < removed.Count; i++)
                {
                    removed[i].Screen.AfterExit();
                    await ReleaseEntryAsync(removed[i], UIFlowDismissalReason.EntryReleased, cancellationToken);
                }

                revealed.Screen.AfterReveal();
                _host.NotifyStackChanged(this);
                return UIFlowNavigationResult.SucceededResult(operationId, UIFlowOperationType.PopTo, ChannelId, routeId);
            }
            catch (OperationCanceledException)
            {
                ForceEntryShown(removedTop);
                ForceEntryCovered(revealed);
                return UIFlowNavigationResult.Cancelled(operationId, UIFlowOperationType.PopTo, ChannelId, routeId, "PopTo was cancelled and the previous state was restored.");
            }
            catch (Exception ex)
            {
                ForceEntryShown(removedTop);
                ForceEntryCovered(revealed);
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
            int index = FindEntryIndex(entryId);
            if (index < 0)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, ChannelId, default(UIFlowRouteId), "The entry has already been removed.");
            }

            if (index != _stack.Count - 1)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, ChannelId, _stack[index].Route.RouteId, "Only the top entry in its channel can close itself.");
            }

            UIFlowStackEntry entry = _stack[index];
            if (entry.Presentation != null)
            {
                string error;
                bool accepted = hasValue
                    ? entry.Presentation.TryRequestValue(value, valueType, out error)
                    : entry.Presentation.TryRequestDismissal(dismissalReason, out error);

                if (!accepted)
                {
                    return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, ChannelId, entry.Route.RouteId, error);
                }
            }
            else if (hasValue)
            {
                return UIFlowNavigationResult.Rejected(operationId, UIFlowOperationType.Dismiss, ChannelId, entry.Route.RouteId, "Only presented entries can close with a typed result.");
            }

            return await PopAsync(operationId, UIFlowOperationType.Dismiss, options, cancellationToken);
        }

        internal async Task ShutdownAsync()
        {
            List<UIFlowStackEntry> entries = new List<UIFlowStackEntry>(_stack);
            _stack.Clear();

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                await ReleaseEntryAsync(entries[i], UIFlowDismissalReason.HostDestroyed, CancellationToken.None);
            }

            _host.NotifyStackChanged(this);
        }

        private async Task<GuardResolution> ResolveGuardsAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowRoute targetRoute,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            UIFlowRoute route = targetRoute;
            object currentArguments = arguments;
            var visited = new HashSet<UIFlowRouteId>();

            for (int depth = 0; depth <= _host.RedirectDepthLimit; depth++)
            {
                if (route == null)
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.Rejected(operationId, operationType, ChannelId, default(UIFlowRouteId), "Navigation target route is null."));
                }

                if (route.TargetChannel != ChannelId)
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.Rejected(operationId, operationType, ChannelId, route.RouteId, "Route targets channel '" + route.TargetChannel + "' but this navigator is '" + ChannelId + "'."));
                }

                if (!visited.Add(route.RouteId))
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.GuardDenied(operationId, operationType, ChannelId, route.RouteId, "Guard redirect cycle detected at route '" + route.RouteId + "'."));
                }

                UIFlowGuardResult guard = await EvaluateGuardsForTargetAsync(operationType, route, currentArguments, options, cancellationToken);
                if (guard == null || guard.Decision == UIFlowGuardDecisionType.Allow)
                {
                    return GuardResolution.Allow(route, currentArguments);
                }

                if (guard.Decision == UIFlowGuardDecisionType.Deny)
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.GuardDenied(operationId, operationType, ChannelId, route.RouteId, guard.Reason));
                }

                _host.NotifyNavigationRedirected(operationId, operationType, options.Reason, ChannelId, route.RouteId, guard.RedirectRoute.RouteId, guard.Reason, options.DiagnosticLabel);
                route = guard.RedirectRoute;
                currentArguments = guard.RedirectArguments;
                options.Reason = UIFlowNavigationReason.Redirect;
            }

            return GuardResolution.FromResult(UIFlowNavigationResult.GuardDenied(operationId, operationType, ChannelId, targetRoute.RouteId, "Guard redirect depth limit exceeded."));
        }

        private async Task<UIFlowGuardResult> EvaluateGuardsForTargetAsync(
            UIFlowOperationType operationType,
            UIFlowRoute targetRoute,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            UIFlowNavigationResult exitResult = await EvaluateExitGuardsAsync(0, operationType, CurrentRoute, targetRoute, arguments, options, cancellationToken);
            if (exitResult != null)
            {
                if (exitResult.Status == UIFlowNavigationStatus.GuardDenied)
                {
                    return UIFlowGuardResult.Deny(exitResult.Message);
                }

                return UIFlowGuardResult.Deny(exitResult.Message);
            }

            var context = new UIFlowGuardContext(_host, this, operationType, options.Reason, CurrentRoute, targetRoute, arguments);

            IReadOnlyList<UIFlowGuard> globalGuards = _host.GlobalGuards;
            for (int i = 0; i < globalGuards.Count; i++)
            {
                UIFlowGuard guard = globalGuards[i];
                if (guard == null)
                {
                    continue;
                }

                UIFlowGuardResult result = await guard.EvaluateAsync(context, cancellationToken);
                if (result != null && result.Decision != UIFlowGuardDecisionType.Allow)
                {
                    return result;
                }
            }

            IReadOnlyList<UIFlowGuard> routeGuards = targetRoute.Guards;
            for (int i = 0; i < routeGuards.Count; i++)
            {
                UIFlowGuard guard = routeGuards[i];
                if (guard == null)
                {
                    continue;
                }

                UIFlowGuardResult result = await guard.EvaluateAsync(context, cancellationToken);
                if (result != null && result.Decision != UIFlowGuardDecisionType.Allow)
                {
                    return result;
                }
            }

            return UIFlowGuardResult.Allow();
        }

        private async Task<UIFlowNavigationResult> EvaluateExitGuardsAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowRoute currentRoute,
            UIFlowRoute targetRoute,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            UIFlowStackEntry current = TopEntry();
            if (current == null)
            {
                return null;
            }

            var context = new UIFlowGuardContext(_host, this, operationType, options.Reason, currentRoute, targetRoute, arguments);
            UIFlowGuardResult result = await current.Screen.EvaluateExitGuardAsync(context, cancellationToken);
            if (result != null && result.Decision == UIFlowGuardDecisionType.Deny)
            {
                return UIFlowNavigationResult.GuardDenied(operationId, operationType, ChannelId, current.Route.RouteId, result.Reason);
            }

            if (result != null && result.Decision == UIFlowGuardDecisionType.Redirect)
            {
                return UIFlowNavigationResult.GuardDenied(operationId, operationType, ChannelId, current.Route.RouteId, "Exit guards cannot redirect; request denied.");
            }

            return null;
        }

        private async Task<UIFlowNavigationResult> HandleDuplicateAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowRoute route,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            int existingIndex = FindRouteIndex(route.RouteId);
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

        private async Task<UIFlowStackEntry> AcquirePreparedEntryAsync(
            UIFlowRoute route,
            object arguments,
            UIFlowNavigationReason reason,
            IUIFlowPresentationCompletion presentationCompletion,
            CancellationToken cancellationToken)
        {
            IUIFlowScreenProvider provider = _host.GetProvider(route);
            var request = new UIFlowScreenRequest(_host, this, route, _config.Root, arguments);
            UIFlowScreenLease lease = await provider.AcquireAsync(request, cancellationToken);
            if (lease == null || lease.Screen == null)
            {
                throw new UIFlowNavigationException(ChannelId.ToString(), "Acquire", route.RouteId.ToString(), "The provider returned a null lease or screen.");
            }

            var entry = new UIFlowStackEntry(route, lease, presentationCompletion == null ? null : new UIFlowPresentationState(presentationCompletion));
            if (presentationCompletion != null)
            {
                presentationCompletion.SetEntryId(entry.EntryId);
            }

            var context = new UIFlowContext(_host, this, route, entry.EntryId, arguments, entry.EntryLifetimeToken, reason);
            entry.Context = context;
            await lease.Screen.PrepareAsync(context, cancellationToken);
            return entry;
        }

        private async Task RunCoverAndShowAsync(UIFlowStackEntry previous, UIFlowStackEntry next, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            if (_config.TransitionExecutionMode == UIFlowTransitionExecutionMode.Parallel)
            {
                Task show = ShowEntryAsync(next, operationType, options, cancellationToken);
                Task hide = Task.CompletedTask;
                if (previous != null)
                {
                    if (_config.CoveredScreenBehavior == UIFlowCoveredScreenBehavior.KeepActiveButNonInteractable)
                    {
                        previous.Screen.SetVisibleImmediate(true, false);
                    }
                    else
                    {
                        hide = HideEntryAsync(previous, operationType, options, cancellationToken);
                    }
                }

                await Task.WhenAll(hide, show);
                return;
            }

            if (previous != null)
            {
                if (_config.CoveredScreenBehavior == UIFlowCoveredScreenBehavior.KeepActiveButNonInteractable)
                {
                    previous.Screen.SetVisibleImmediate(true, false);
                }
                else
                {
                    await HideEntryAsync(previous, operationType, options, cancellationToken);
                }
            }

            await ShowEntryAsync(next, operationType, options, cancellationToken);
        }

        private async Task RunHideAndRevealAsync(UIFlowStackEntry removed, UIFlowStackEntry revealed, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            if (_config.TransitionExecutionMode == UIFlowTransitionExecutionMode.Parallel)
            {
                Task hide = HideEntryAsync(removed, operationType, options, cancellationToken);
                Task show = revealed == null ? Task.CompletedTask : ShowEntryAsync(revealed, operationType, options, cancellationToken);
                await Task.WhenAll(hide, show);
                return;
            }

            await HideEntryAsync(removed, operationType, options, cancellationToken);
            if (revealed != null)
            {
                await ShowEntryAsync(revealed, operationType, options, cancellationToken);
            }
        }

        private async Task RunReplaceAsync(UIFlowStackEntry current, UIFlowStackEntry next, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            if (_config.TransitionExecutionMode == UIFlowTransitionExecutionMode.Parallel)
            {
                Task hide = HideEntryAsync(current, operationType, options, cancellationToken);
                Task show = ShowEntryAsync(next, operationType, options, cancellationToken);
                await Task.WhenAll(hide, show);
                return;
            }

            await HideEntryAsync(current, operationType, options, cancellationToken);
            await ShowEntryAsync(next, operationType, options, cancellationToken);
        }

        private async Task RunResetAsync(UIFlowStackEntry current, UIFlowStackEntry root, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            if (_config.TransitionExecutionMode == UIFlowTransitionExecutionMode.Parallel)
            {
                Task hide = current == null ? Task.CompletedTask : HideEntryAsync(current, UIFlowOperationType.Reset, options, cancellationToken);
                Task show = root == null ? Task.CompletedTask : ShowEntryAsync(root, UIFlowOperationType.Reset, options, cancellationToken);
                await Task.WhenAll(hide, show);
                return;
            }

            if (current != null)
            {
                await HideEntryAsync(current, UIFlowOperationType.Reset, options, cancellationToken);
            }

            if (root != null)
            {
                await ShowEntryAsync(root, UIFlowOperationType.Reset, options, cancellationToken);
            }
        }

        private Task ShowEntryAsync(UIFlowStackEntry entry, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            UIFlowTransition transition = entry.Route.ShowTransitionOverride != null ? entry.Route.ShowTransitionOverride : _config.DefaultShowTransition;
            var context = new UIFlowTransitionContext(entry.Screen, entry.Route, ChannelId, operationType, UIFlowTransitionDirection.Show, options.Animate);
            if (transition == null)
            {
                entry.Screen.SetVisibleImmediate(true, true);
                return Task.CompletedTask;
            }

            return transition.ShowAsync(context, cancellationToken);
        }

        private Task HideEntryAsync(UIFlowStackEntry entry, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            UIFlowTransition transition = entry.Route.HideTransitionOverride != null ? entry.Route.HideTransitionOverride : _config.DefaultHideTransition;
            var context = new UIFlowTransitionContext(entry.Screen, entry.Route, ChannelId, operationType, UIFlowTransitionDirection.Hide, options.Animate);
            if (transition == null)
            {
                entry.Screen.SetVisibleImmediate(false, false);
                return Task.CompletedTask;
            }

            return transition.HideAsync(context, cancellationToken);
        }

        private void ForceEntryShown(UIFlowStackEntry entry)
        {
            if (entry == null || entry.Screen == null)
            {
                return;
            }

            UIFlowTransition transition = entry.Route.ShowTransitionOverride != null ? entry.Route.ShowTransitionOverride : _config.DefaultShowTransition;
            if (transition == null)
            {
                entry.Screen.SetVisibleImmediate(true, true);
                return;
            }

            transition.ForceShown(new UIFlowTransitionContext(entry.Screen, entry.Route, ChannelId, UIFlowOperationType.System, UIFlowTransitionDirection.Show, false));
        }

        private void ForceEntryCovered(UIFlowStackEntry entry)
        {
            if (entry == null || entry.Screen == null)
            {
                return;
            }

            if (_config.CoveredScreenBehavior == UIFlowCoveredScreenBehavior.KeepActiveButNonInteractable)
            {
                entry.Screen.SetVisibleImmediate(true, false);
            }
            else
            {
                entry.Screen.SetVisibleImmediate(false, false);
            }
        }

        private async Task RollbackAfterFailedEntryAsync(UIFlowStackEntry previous, UIFlowStackEntry failed)
        {
            ForceEntryShown(previous);
            if (failed != null)
            {
                failed.Screen.SetVisibleImmediate(false, false);
                await ReleaseEntryAsync(failed, UIFlowDismissalReason.NavigationRejected, CancellationToken.None);
            }
        }

        private async Task ReleaseEntryAsync(UIFlowStackEntry entry, UIFlowDismissalReason reason, CancellationToken cancellationToken)
        {
            if (entry == null || entry.Released)
            {
                return;
            }

            entry.Released = true;
            entry.EntryLifetimeCts.Cancel();

            if (entry.Presentation != null)
            {
                if (entry.Presentation.CompletionRequested)
                {
                    entry.Presentation.CompleteAfterExit();
                }
                else if (reason == UIFlowDismissalReason.HostDestroyed)
                {
                    entry.Presentation.Completion.CompleteHostDestroyed("UI Flow host was destroyed.");
                }
                else if (reason == UIFlowDismissalReason.RemovedByReplacementOrReset)
                {
                    entry.Presentation.CompleteRemoved(reason, "Presentation entry was removed by replacement or reset.");
                }
                else
                {
                    entry.Presentation.Completion.CompleteDismissed(reason, null);
                }
            }

            if (entry.Screen != null)
            {
                entry.Screen.ReleaseContext();
            }

            entry.EntryLifetimeCts.Dispose();

            if (entry.Lease != null && entry.Lease.Provider != null)
            {
                await entry.Lease.Provider.ReleaseAsync(entry.Lease, CancellationToken.None);
            }
        }

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

        private IReadOnlyList<UIFlowStackEntrySnapshot> CreateStackSnapshot()
        {
            var snapshots = new List<UIFlowStackEntrySnapshot>(_stack.Count);
            for (int i = 0; i < _stack.Count; i++)
            {
                UIFlowStackEntry entry = _stack[i];
                snapshots.Add(new UIFlowStackEntrySnapshot(entry.EntryId, entry.Route.RouteId, entry.Route.DisplayName, entry.Screen.State, entry.Presentation != null));
            }

            return snapshots;
        }

        private UIFlowStackEntry TopEntry()
        {
            return _stack.Count == 0 ? null : _stack[_stack.Count - 1];
        }

        private int FindRouteIndex(UIFlowRouteId routeId)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].Route.RouteId == routeId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindEntryIndex(Guid entryId)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].EntryId == entryId)
                {
                    return i;
                }
            }

            return -1;
        }

        private UIFlowNavigationException CreateException(UIFlowOperationType operationType, UIFlowRoute route, string remediation, Exception inner)
        {
            return new UIFlowNavigationException(ChannelId.ToString(), operationType.ToString(), route == null ? "<none>" : route.RouteId.ToString(), remediation, inner);
        }

        private sealed class GuardResolution
        {
            public UIFlowRoute Route;
            public object Arguments;
            public UIFlowNavigationResult Result;

            public static GuardResolution Allow(UIFlowRoute route, object arguments)
            {
                return new GuardResolution { Route = route, Arguments = arguments };
            }

            public static GuardResolution FromResult(UIFlowNavigationResult result)
            {
                return new GuardResolution { Result = result };
            }
        }
    }

    internal sealed class UIFlowStackEntry
    {
        public UIFlowStackEntry(UIFlowRoute route, UIFlowScreenLease lease, UIFlowPresentationState presentation)
        {
            EntryId = Guid.NewGuid();
            Route = route;
            Lease = lease;
            Screen = lease.Screen;
            Presentation = presentation;
            EntryLifetimeCts = new CancellationTokenSource();
        }

        public Guid EntryId;
        public UIFlowRoute Route;
        public UIFlowScreenLease Lease;
        public UIFlowScreen Screen;
        public UIFlowContext Context;
        public UIFlowPresentationState Presentation;
        public CancellationTokenSource EntryLifetimeCts;
        public bool Released;

        public CancellationToken EntryLifetimeToken
        {
            get { return EntryLifetimeCts.Token; }
        }
    }
}
