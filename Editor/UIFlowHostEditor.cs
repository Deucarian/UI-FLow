using Deucarian.Editor;
using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowHost))]
    public sealed class UIFlowHostEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UIFlowHost host = (UIFlowHost)target;
            IReadOnlyList<string> messages = UIFlowEditorValidation.ValidateHost(host);
            for (int i = 0; i < messages.Count; i++)
            {
                DeucarianEditorTextGUI.HelpBox(messages[i], messages[i].StartsWith("Error:") ? MessageType.Error : MessageType.Warning);
            }

            if (Application.isPlaying)
            {
                UIFlowHostSnapshot snapshot = host.CreateDiagnosticSnapshot();
                EditorGUILayout.Space();
                DeucarianEditorTextGUI.LabelField("Runtime", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                DeucarianEditorTextGUI.LabelField("Initialization", snapshot.InitializationState.ToString());
                DeucarianEditorTextGUI.LabelField("Queued Operations", snapshot.QueuedOperationCount.ToString());
                if (snapshot.CurrentOperation != null)
                {
                    DeucarianEditorTextGUI.LabelField("Current Operation", snapshot.CurrentOperation.OperationType + " " + snapshot.CurrentOperation.TargetRouteId);
                }
            }
        }
    }
}
