using Deucarian.Editor;
using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowHost))]
    public sealed class UIFlowHostEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("UI Flow host");
            DeucarianEditorInspector.Properties(root, serializedObject);
            var feedback = new VisualElement(); root.Add(feedback);
            void Refresh()
            {
                feedback.Clear();
                if (!(target is UIFlowHost host)) return;
                foreach (string message in UIFlowEditorValidation.ValidateHost(host))
                    feedback.Add(new HelpBox(message, message.StartsWith("Error:") ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning));
                if (!Application.isPlaying) return;
                UIFlowHostSnapshot snapshot = host.CreateDiagnosticSnapshot();
                feedback.Add(DeucarianEditorWorkspaceControls.Label("Runtime", "dw-section-title"));
                feedback.Add(DeucarianEditorWorkspaceControls.Label("Initialization · " + snapshot.InitializationState, "dw-muted"));
                feedback.Add(DeucarianEditorWorkspaceControls.Label("Queued operations · " + snapshot.QueuedOperationCount, "dw-muted"));
                if (snapshot.CurrentOperation != null)
                    feedback.Add(DeucarianEditorWorkspaceControls.Label(snapshot.CurrentOperation.OperationType + " · " + snapshot.CurrentOperation.TargetRouteId, "dw-muted"));
            }
            DeucarianEditorInspector.Observe(root, serializedObject, Refresh);
            root.schedule.Execute(Refresh).Every(500);
            return root;
        }
    }
}
