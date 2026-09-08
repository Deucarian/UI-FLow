using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    internal sealed class UIFlowGuardPipeline
    {
        private readonly IUIFlowGuardEnvironment _environment;
        private readonly UIFlowNavigator _navigator;
        private readonly Func<UIFlowStackEntry> _top;
        internal UIFlowGuardPipeline(IUIFlowGuardEnvironment environment, UIFlowNavigator navigator, Func<UIFlowStackEntry> top)
        {
            _environment = environment;
            _navigator = navigator;
            _top = top;
        }

        internal async Task<GuardResolution> ResolveGuardsAsync(
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

            for (int depth = 0; depth <= _environment.RedirectDepthLimit; depth++)
            {
                if (route == null)
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.Rejected(operationId, operationType, _navigator.ChannelId, default(UIFlowRouteId), "Navigation target route is null."));
                }

                if (route.TargetChannel != _navigator.ChannelId)
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.Rejected(operationId, operationType, _navigator.ChannelId, route.RouteId, "Route targets channel '" + route.TargetChannel + "' but this navigator is '" + _navigator.ChannelId + "'."));
                }

                if (!visited.Add(route.RouteId))
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.GuardDenied(operationId, operationType, _navigator.ChannelId, route.RouteId, "Guard redirect cycle detected at route '" + route.RouteId + "'."));
                }

                UIFlowGuardResult guard = await EvaluateGuardsForTargetAsync(operationType, route, currentArguments, options, cancellationToken);
                if (guard == null || guard.Decision == UIFlowGuardDecisionType.Allow)
                {
                    return GuardResolution.Allow(route, currentArguments);
                }

                if (guard.Decision == UIFlowGuardDecisionType.Deny)
                {
                    return GuardResolution.FromResult(UIFlowNavigationResult.GuardDenied(operationId, operationType, _navigator.ChannelId, route.RouteId, guard.Reason));
                }

                _environment.NotifyNavigationRedirected(operationId, operationType, options.Reason, _navigator.ChannelId, route.RouteId, guard.RedirectRoute.RouteId, guard.Reason, options.DiagnosticLabel);
                route = guard.RedirectRoute;
                currentArguments = guard.RedirectArguments;
                options.Reason = UIFlowNavigationReason.Redirect;
            }

            return GuardResolution.FromResult(UIFlowNavigationResult.GuardDenied(operationId, operationType, _navigator.ChannelId, targetRoute.RouteId, "Guard redirect depth limit exceeded."));
        }

        internal async Task<UIFlowGuardResult> EvaluateGuardsForTargetAsync(
            UIFlowOperationType operationType,
            UIFlowRoute targetRoute,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            UIFlowNavigationResult exitResult = await EvaluateExitGuardsAsync(0, operationType, _navigator.CurrentRoute, targetRoute, arguments, options, cancellationToken);
            if (exitResult != null)
            {
                if (exitResult.Status == UIFlowNavigationStatus.GuardDenied)
                {
                    return UIFlowGuardResult.Deny(exitResult.Message);
                }

                return UIFlowGuardResult.Deny(exitResult.Message);
            }

            var context = _environment.CreateGuardContext(_navigator, operationType, options.Reason, _navigator.CurrentRoute, targetRoute, arguments);

            IReadOnlyList<UIFlowGuard> globalGuards = _environment.GlobalGuards;
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

        internal async Task<UIFlowNavigationResult> EvaluateExitGuardsAsync(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowRoute currentRoute,
            UIFlowRoute targetRoute,
            object arguments,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken)
        {
            UIFlowStackEntry current = _top();
            if (current == null)
            {
                return null;
            }

            var context = _environment.CreateGuardContext(_navigator, operationType, options.Reason, currentRoute, targetRoute, arguments);
            UIFlowGuardResult result = await current.Screen.EvaluateExitGuardAsync(context, cancellationToken);
            if (result != null && result.Decision == UIFlowGuardDecisionType.Deny)
            {
                return UIFlowNavigationResult.GuardDenied(operationId, operationType, _navigator.ChannelId, current.Route.RouteId, result.Reason);
            }

            if (result != null && result.Decision == UIFlowGuardDecisionType.Redirect)
            {
                return UIFlowNavigationResult.GuardDenied(operationId, operationType, _navigator.ChannelId, current.Route.RouteId, "Exit guards cannot redirect; request denied.");
            }

            return null;
        }

        internal sealed class GuardResolution
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
}
