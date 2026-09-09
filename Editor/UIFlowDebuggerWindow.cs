using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    public sealed class UIFlowDebuggerWindow : EditorWindow
    {
        private Vector2 _scroll;

        public static void Open()
        {
            UIFlowDebuggerWindow window = DeucarianEditorWindowPages.GetStandalone<UIFlowDebuggerWindow>("UI Flow");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        public static IDeucarianEditorPage CreatePage() =>
            DeucarianEditorImGuiPage.Create<UIFlowDebuggerWindow>("deucarian.ui-flow.debugger", window => window.OnGUI());

        private void OnGUI()
        {
            using (DeucarianEditorWorkbenchPanelScope page =
                   DeucarianEditorWorkbenchGUI.BeginSettingsPage(GUILayout.ExpandHeight(true)))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                DeucarianEditorChrome.DrawPackageHeader(
                    "workflow",
                    "UI Flow Debugger",
                    "Inspect active hosts, queued operations, channels, and failures.");
                IReadOnlyList<UIFlowHost> hosts = UIFlowDiagnosticRegistry.ActiveHosts;
                if (hosts.Count == 0)
                {
                    DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                        "circle-info",
                        "No active UI Flow hosts are registered.",
                        DeucarianEditorStatus.Info);
                }

                for (int i = 0; i < hosts.Count; i++)
                {
                    UIFlowHost host = hosts[i];
                    if (host == null)
                    {
                        continue;
                    }

                    DrawHost(host.CreateDiagnosticSnapshot());
                }

                DeucarianEditorChrome.DrawFooterVersion("com.deucarian.ui-flow");
                EditorGUILayout.EndScrollView();
            }

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private static void DrawHost(UIFlowHostSnapshot snapshot)
        {
            DeucarianEditorChrome.DrawSectionHeader(snapshot.HostName);
            DeucarianEditorChrome.BeginSection();
            EditorGUILayout.LabelField("Initialization", snapshot.InitializationState.ToString());
            EditorGUILayout.LabelField("Queued", snapshot.QueuedOperationCount.ToString());

            if (snapshot.CurrentOperation != null)
            {
                EditorGUILayout.LabelField("Current", snapshot.CurrentOperation.OperationType + " " + snapshot.CurrentOperation.TargetRouteId);
            }

            for (int i = 0; i < snapshot.Channels.Count; i++)
            {
                UIFlowChannelSnapshot channel = snapshot.Channels[i];
                EditorGUILayout.LabelField("Channel " + channel.ChannelId + " (" + channel.Kind + ")", EditorStyles.miniBoldLabel);
                for (int entryIndex = 0; entryIndex < channel.Stack.Count; entryIndex++)
                {
                    UIFlowStackEntrySnapshot entry = channel.Stack[entryIndex];
                    EditorGUILayout.LabelField("  " + entryIndex + ": " + entry.RouteName + " [" + entry.RouteId + "] " + entry.State + (entry.HasPresentation ? " presentation" : string.Empty));
                }
            }

            if (snapshot.LastResult != null)
            {
                EditorGUILayout.LabelField("Last Result", snapshot.LastResult.ToString(), EditorStyles.wordWrappedLabel);
            }

            if (snapshot.LastFailure != null)
            {
                EditorGUILayout.HelpBox(snapshot.LastFailure.Message, MessageType.Error);
            }

            DeucarianEditorChrome.EndSection();
        }
    }
}
