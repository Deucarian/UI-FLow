using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    internal sealed class UIFlowTransitionDriver
    {
        private readonly UIFlowChannelConfig _config;
        internal UIFlowTransitionDriver(UIFlowChannelConfig config) { _config = config; }
        private UIFlowChannelId ChannelId => _config.ChannelId;

        internal async Task RunCoverAndShowAsync(UIFlowStackEntry previous, UIFlowStackEntry next, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
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

        internal async Task RunHideAndRevealAsync(UIFlowStackEntry removed, UIFlowStackEntry revealed, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
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

        internal async Task RunReplaceAsync(UIFlowStackEntry current, UIFlowStackEntry next, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
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

        internal async Task RunResetAsync(UIFlowStackEntry current, UIFlowStackEntry root, UIFlowNavigationOptions options, CancellationToken cancellationToken)
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

        internal Task ShowEntryAsync(UIFlowStackEntry entry, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
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

        internal Task HideEntryAsync(UIFlowStackEntry entry, UIFlowOperationType operationType, UIFlowNavigationOptions options, CancellationToken cancellationToken)
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

        internal void ForceEntryShown(UIFlowStackEntry entry)
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

            try
            {
                transition.ForceShown(new UIFlowTransitionContext(entry.Screen, entry.Route, ChannelId, UIFlowOperationType.System, UIFlowTransitionDirection.Show, false));
            }
            catch (Exception exception)
            {
                entry.Screen.SetVisibleImmediate(true, true);
                UIFlowLog.Navigation.Exception(new UIFlowNavigationException(ChannelId.ToString(), "Restore",
                    entry.Route.RouteId.ToString(),
                    "The custom transition could not restore the screen. Immediate visibility was restored.", exception));
            }
        }

        internal void ForceEntryCovered(UIFlowStackEntry entry)
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

        internal async Task RollbackAfterFailedEntryAsync(UIFlowStackEntry previous, UIFlowStackEntry failed)
        {
            try { ForceEntryShown(previous); }
            finally
            {
                if (failed != null)
                {
                    try { if (failed.Screen != null) failed.Screen.SetVisibleImmediate(false, false); }
                    finally { await UIFlowEntryLifetime.ReleaseAsync(failed, UIFlowDismissalReason.NavigationRejected); }
                }
            }
        }
    }
}
