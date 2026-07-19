using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    public sealed class UIFlowDebuggerWindow : EditorWindow
    {
        public const string MenuPath = UIFlowProjectValidator.MenuRoot + "Debugger";

        private Vector2 _scroll;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<UIFlowDebuggerWindow>("UI Flow");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            IReadOnlyList<UIFlowHost> hosts = UIFlowDiagnosticRegistry.ActiveHosts;
            if (hosts.Count == 0)
            {
                EditorGUILayout.HelpBox("No active UI Flow hosts are registered.", MessageType.Info);
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

            EditorGUILayout.EndScrollView();
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private static void DrawHost(UIFlowHostSnapshot snapshot)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(snapshot.HostName, EditorStyles.boldLabel);
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
        }
    }
}
