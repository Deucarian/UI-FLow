using Deucarian.Editor;
using Deucarian.UIFlow.UGUI;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowButtonAction))]
    public sealed class UIFlowButtonActionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Button action");
            DeucarianEditorInspector.Properties(root, serializedObject);
            var feedback = new VisualElement(); root.Add(feedback);
            DeucarianEditorInspector.Observe(root, serializedObject, () =>
            {
                feedback.Clear();
                if (!(target is UIFlowButtonAction binder)) return;
                var button = serializedObject.FindProperty("_button").objectReferenceValue as UnityEngine.UI.Button;
                if (button == null) button = binder.GetComponent<UnityEngine.UI.Button>();
                if (button == null) feedback.Add(new HelpBox("Assign a Button or add one to this GameObject.", HelpBoxMessageType.Error));
                if (serializedObject.FindProperty("_action").objectReferenceValue == null)
                    feedback.Add(new HelpBox("Assign an action from Assets → Create → Deucarian → UI Flow → Actions.", HelpBoxMessageType.Error));
                if (serializedObject.FindProperty("_host").objectReferenceValue == null)
                    feedback.Add(DeucarianEditorWorkspaceControls.Label("No host assigned: uses a parent host. Dismiss uses the containing screen.", "dw-muted"));
            });
            return root;
        }
    }
}
