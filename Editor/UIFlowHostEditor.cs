using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowHost))]
    public sealed class UIFlowHostEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UIFlowHost host = (UIFlowHost)target;
            IReadOnlyList<string> messages = UIFlowEditorValidation.ValidateHost(host);
            for (int i = 0; i < messages.Count; i++)
            {
                EditorGUILayout.HelpBox(messages[i], messages[i].StartsWith("Error:") ? MessageType.Error : MessageType.Warning);
            }

            if (Application.isPlaying)
            {
                UIFlowHostSnapshot snapshot = host.CreateDiagnosticSnapshot();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Initialization", snapshot.InitializationState.ToString());
                EditorGUILayout.LabelField("Queued Operations", snapshot.QueuedOperationCount.ToString());
                if (snapshot.CurrentOperation != null)
                {
                    EditorGUILayout.LabelField("Current Operation", snapshot.CurrentOperation.OperationType + " " + snapshot.CurrentOperation.TargetRouteId);
                }
            }
        }
    }
}
