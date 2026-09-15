using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.UIFlow.Editor
{
    internal sealed class UIFlowDebuggerPage
    {
        private readonly DeucarianEditorCollectionWorkspace view;
        private readonly List<UIFlowHost> hosts = new List<UIFlowHost>();
        private readonly List<string> hostNames = new List<string>();
        private UIFlowHost host;
        private string channelId, selectedId, fingerprint, message;
        private double nextRefresh;
        private bool disposed;
        private bool transitionExpanded;
        private CancellationTokenSource request;
        private Task backTask;
        public IDeucarianEditorPage Page { get; }

        internal UIFlowDebuggerPage()
        {
            var root = new VisualElement();
            view = new DeucarianEditorCollectionWorkspace(root, Application.productName, "UI Flow",
                "See how screens move through your app.", "deucarian.ui-flow.debugger", "Find a route…");
            view.UsePanels();
            view.Collection.AddToClassList("dw-balanced-panels");
            view.Collection.AddToClassList("dw-detached-details");
            view.Collection.Q<ScrollView>("workspace-collection").Insert(0, Ui.Label("Screen stack", "dw-section-title"));
            view.Workspace.SetScopeBeforeTabs();
            view.Workspace.SetScopeStacked();
            view.Workspace.SearchField.RegisterValueChangedCallback(_ => Refresh(true));
            Page = new DeucarianEditorPage(root, activate: _ => Refresh(false), deactivate: CancelRequest,
                update: _ => Update(), dispose: Dispose);
            Refresh(true);
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh = EditorApplication.timeSinceStartup + .5;
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (disposed) return;
            RefreshHosts();
            UIFlowHostSnapshot snapshot = host != null ? host.CreateDiagnosticSnapshot() : null;
            string next = Fingerprint(snapshot);
            if (!force && next == fingerprint) return;
            RenderScope(snapshot);
            RenderCollection(snapshot);
            fingerprint = Fingerprint(snapshot);
        }

        private void RefreshHosts()
        {
            hosts.Clear(); hostNames.Clear();
            foreach (var candidate in UIFlowDiagnosticRegistry.ActiveHosts)
            {
                if (candidate == null) continue;
                hosts.Add(candidate);
                hostNames.Add(candidate.name + " · " + candidate.GetInstanceID());
            }
            if (host != null && !hosts.Contains(host)) host = null;
            if (host == null && hosts.Count > 0) host = hosts[0];
        }

        private void RenderScope(UIFlowHostSnapshot snapshot)
        {
            var scope = view.Workspace.Scope;
            scope.Clear(); view.Workspace.Tabs.Clear();
            var choices = hosts.Count > 0 ? new List<string>(hostNames) : new List<string> { "No active host" };
            var targets = new List<UIFlowHost>(hosts);
            var picker = new PopupField<string>(choices, Math.Max(0, hosts.IndexOf(host))) { name = "ui-flow-host" };
            picker.SetEnabled(hosts.Count > 0);
            picker.RegisterValueChangedCallback(_ =>
            {
                CancelRequest(); host = targets[picker.index]; channelId = selectedId = null; message = null; Refresh(true);
            });
            scope.Add(Ui.Field("Host", picker));
            if (snapshot == null) return;
            bool found = false;
            foreach (var channel in snapshot.Channels) found |= channel.ChannelId.ToString() == channelId;
            if (!found) { channelId = snapshot.Channels.Count > 0 ? snapshot.Channels[0].ChannelId.ToString() : null; selectedId = null; }
            foreach (var channel in snapshot.Channels)
            {
                if (snapshot.Channels.Count < 2) break;
                string id = channel.ChannelId.ToString();
                var tab = Ui.Button(id, () => { channelId = id; selectedId = null; Refresh(true); });
                tab.AddToClassList("dw-tab");
                tab.EnableInClassList("dw-selected", channelId == id);
                view.Workspace.Tabs.Add(tab);
            }
        }

        private void RenderCollection(UIFlowHostSnapshot snapshot)
        {
            UIFlowChannelSnapshot channel = null;
            if (snapshot != null)
                foreach (var candidate in snapshot.Channels)
                    if (candidate.ChannelId.ToString() == channelId) { channel = candidate; break; }
            var items = new List<DeucarianEditorCollectionItem>();
            UIFlowStackEntrySnapshot selected = null;
            string search = view.Workspace.SearchField.value ?? string.Empty;
            if (channel != null)
            {
                bool selectedIsVisible = false;
                foreach (var entry in channel.Stack)
                    selectedIsVisible |= entry.EntryId.ToString() == selectedId &&
                        (entry.RouteName + " " + entry.RouteId).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!selectedIsVisible) selectedId = null;
                for (int index = channel.Stack.Count - 1; index >= 0; index--)
                {
                    var entry = channel.Stack[index];
                    string id = entry.EntryId.ToString();
                    if ((entry.RouteName + " " + entry.RouteId).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (selectedId == null) selectedId = id;
                    if (selectedId == id) selected = entry;
                    items.Add(new DeucarianEditorCollectionItem(id, entry.RouteName, string.Empty, entry.State.ToString(),
                        () => { selectedId = id; Refresh(true); }));
                }
            }
            if (selected == null) selectedId = null;
            var previousTrail = view.Workspace.Scope.Q("ui-flow-trail");
            previousTrail?.RemoveFromHierarchy();
            if (channel != null && channel.Stack.Count > 0)
            {
                var trail = Ui.Region("ui-flow-trail", "dw-route-trail");
                foreach (var entry in channel.Stack)
                {
                    string id = entry.EntryId.ToString();
                    var step = Ui.Button(entry.RouteName, () => { selectedId = id; Refresh(true); });
                    step.AddToClassList("dw-route-step");
                    step.EnableInClassList("dw-selected", selectedId == id);
                    step.tooltip = "Inspect this screen. The running app is unchanged.";
                    trail.Add(step);
                    if (!ReferenceEquals(entry, channel.Stack[channel.Stack.Count - 1]))
                        trail.Add(Ui.Icon(DeucarianEditorIconIds.ChevronRight));
                }
                view.Workspace.Scope.Add(trail);
            }
            view.SetItems(items, selectedId, snapshot == null
                ? "Start Play Mode with a UI Flow host to see its screen stack." : "No screens match this channel and search.");
            var details = view.Details;
            details.Clear();
            var screen = Ui.Panel("ui-flow-selected-screen", "Selected screen");
            details.Add(screen);
            var form = new DeucarianEditorWorkspaceForm(screen);
            if (selected != null)
            {
                form.ReadOnly("ui-flow-route", "Route", () => selected.RouteId.ToString());
                form.ReadOnly("ui-flow-state", "State", () => selected.State.ToString());
            }
            if (snapshot != null)
            {
                form.ReadOnly("ui-flow-operation", "Transition", () => snapshot.CurrentOperation == null ? "Idle"
                    : snapshot.CurrentOperation.OperationType + " · " + snapshot.CurrentOperation.TargetRouteId);
            }
            var back = Ui.IconButton(backTask != null && !backTask.IsCompleted ? "Navigating…" : "Back", DeucarianEditorIconIds.Back, StartBack);
            back.name = "ui-flow-back";
            back.tooltip = "Navigate back in the running app. This uses the host's normal guards and transitions.";
            back.SetEnabled(EditorApplication.isPlaying && snapshot?.InitializationState == UIFlowInitializationState.Initialized
                && (backTask == null || backTask.IsCompleted));
            var actions = Ui.Actions(Ui.IconButton("Refresh", DeucarianEditorIconIds.Refresh, () => Refresh(true), DeucarianEditorButtonRole.Primary), back);
            actions.AddToClassList("dw-equal-actions"); screen.Add(actions);
            var transition = new Foldout { name = "ui-flow-last-transition", text = "Last transition", value = transitionExpanded };
            transition.AddToClassList("dw-foldout"); transition.AddToClassList("dw-foldout-panel");
            transition.AddToClassList("dw-foldout-followup");
            transition.RegisterValueChangedCallback(evt => transitionExpanded = evt.newValue);
            details.Add(transition);
            transition.Add(Ui.Label(message ?? snapshot?.LastFailure?.Message ?? snapshot?.LastResult?.ToString()
                ?? "No transition recorded.", "dw-muted"));
            var diagnostics = new DeucarianEditorWorkspaceForm(transition);
            if (selected != null)
                diagnostics.ReadOnly("ui-flow-presentation", "Presentation", () => selected.HasPresentation ? "Attached" : "None");
            if (snapshot != null)
            {
                diagnostics.ReadOnly("ui-flow-initialization", "Host", () => snapshot.InitializationState.ToString());
                diagnostics.ReadOnly("ui-flow-queued", "Queued", () => snapshot.QueuedOperationCount.ToString());
            }
            if (snapshot?.CurrentOperation != null)
            {
                var advanced = new Foldout { text = "Operation details", value = false };
                advanced.AddToClassList("dw-foldout"); transition.Add(advanced);
                var operation = snapshot.CurrentOperation;
                var debug = new DeucarianEditorWorkspaceForm(advanced);
                debug.ReadOnly(null, "Operation", () => operation.OperationId.ToString());
                debug.ReadOnly(null, "Reason", () => operation.Reason.ToString());
                debug.ReadOnly(null, "Label", () => operation.DiagnosticLabel ?? "—");
                debug.ReadOnly(null, "Queue duration", () => operation.QueueDuration.TotalMilliseconds.ToString("0") + " ms");
            }
        }

        private string Fingerprint(UIFlowHostSnapshot snapshot)
        {
            var text = new StringBuilder().Append(host != null ? host.GetInstanceID() : 0).Append(channelId).Append(selectedId).Append(message);
            foreach (var name in hostNames) text.Append(name);
            if (snapshot == null) return text.ToString();
            text.Append(snapshot.InitializationState).Append(snapshot.QueuedOperationCount).Append(snapshot.LastResult).Append(snapshot.LastFailure?.Message)
                .Append(snapshot.CurrentOperation?.OperationId).Append(backTask != null && !backTask.IsCompleted);
            foreach (var channel in snapshot.Channels)
            {
                text.Append(channel.ChannelId);
                foreach (var entry in channel.Stack) text.Append(entry.EntryId).Append(entry.State).Append(entry.HasPresentation);
            }
            return text.ToString();
        }

        private void StartBack()
        {
            if (!EditorApplication.isPlaying || host == null || (backTask != null && !backTask.IsCompleted)) return;
            request = new CancellationTokenSource();
            backTask = NavigateBack(host, request);
            Refresh(true);
        }

        private async Task NavigateBack(UIFlowHost target, CancellationTokenSource cancellation)
        {
            try
            {
                var result = await target.BackAsync(new UIFlowNavigationOptions { DiagnosticLabel = "UI Flow debugger" }, cancellation.Token);
                if (!disposed && host == target) message = result.ToString();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (!disposed && host == target) message = error.Message; }
            finally { if (ReferenceEquals(request, cancellation)) request = null; cancellation.Dispose(); }
        }

        private void CancelRequest() => request?.Cancel();
        private void Dispose() { disposed = true; CancelRequest(); view.Dispose(); }
    }
}
