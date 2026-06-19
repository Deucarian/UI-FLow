using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Context supplied to global, exit, and route-entry guards.
    /// </summary>
    public sealed class UIFlowGuardContext
    {
        public UIFlowGuardContext(
            UIFlowHost host,
            UIFlowNavigator navigator,
            UIFlowOperationType operationType,
            UIFlowNavigationReason reason,
            UIFlowRoute currentRoute,
            UIFlowRoute targetRoute,
            object arguments)
        {
            Host = host;
            Navigator = navigator;
            OperationType = operationType;
            Reason = reason;
            CurrentRoute = currentRoute;
            TargetRoute = targetRoute;
            Arguments = arguments;
        }

        public UIFlowHost Host { get; private set; }
        public UIFlowNavigator Navigator { get; private set; }
        public UIFlowOperationType OperationType { get; private set; }
        public UIFlowNavigationReason Reason { get; private set; }
        public UIFlowRoute CurrentRoute { get; private set; }
        public UIFlowRoute TargetRoute { get; private set; }
        public object Arguments { get; private set; }
        public UIFlowChannelId ChannelId
        {
            get { return Navigator == null ? default(UIFlowChannelId) : Navigator.ChannelId; }
        }
    }

    /// <summary>
    /// Result returned by a guard.
    /// </summary>
    public sealed class UIFlowGuardResult
    {
        private UIFlowGuardResult(UIFlowGuardDecisionType decision, string reason, UIFlowRoute redirectRoute, object redirectArguments)
        {
            Decision = decision;
            Reason = reason;
            RedirectRoute = redirectRoute;
            RedirectArguments = redirectArguments;
        }

        public UIFlowGuardDecisionType Decision { get; private set; }
        public string Reason { get; private set; }
        public UIFlowRoute RedirectRoute { get; private set; }
        public object RedirectArguments { get; private set; }

        public static UIFlowGuardResult Allow()
        {
            return new UIFlowGuardResult(UIFlowGuardDecisionType.Allow, null, null, null);
        }

        public static UIFlowGuardResult Deny(string reason)
        {
            return new UIFlowGuardResult(UIFlowGuardDecisionType.Deny, string.IsNullOrEmpty(reason) ? "Guard denied navigation." : reason, null, null);
        }

        public static UIFlowGuardResult Redirect(UIFlowRoute route, object arguments = null, string reason = null)
        {
            if (route == null)
            {
                return Deny("Guard requested a redirect without a route.");
            }

            return new UIFlowGuardResult(UIFlowGuardDecisionType.Redirect, reason, route, arguments);
        }
    }

    /// <summary>
    /// Base class for asynchronous UI Flow guard assets.
    /// </summary>
    public abstract class UIFlowGuard : ScriptableObject
    {
        public abstract Task<UIFlowGuardResult> EvaluateAsync(UIFlowGuardContext context, CancellationToken cancellationToken);
    }
}
