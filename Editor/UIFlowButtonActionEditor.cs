using Deucarian.UIFlow.UGUI;
using UnityEditor;
using UnityEngine.UI;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowButtonAction))]
    public sealed class UIFlowButtonActionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty button = serializedObject.FindProperty("_button");
            SerializedProperty action = serializedObject.FindProperty("_action");
            SerializedProperty host = serializedObject.FindProperty("_host");
            SerializedProperty disableWhileRunning = serializedObject.FindProperty("_disableWhileRunning");

            EditorGUILayout.PropertyField(button);
            EditorGUILayout.PropertyField(action);
            EditorGUILayout.PropertyField(host);
            EditorGUILayout.PropertyField(disableWhileRunning);

            UIFlowButtonAction binder = (UIFlowButtonAction)target;
            Button resolvedButton = button.objectReferenceValue as Button;
            if (resolvedButton == null)
            {
                resolvedButton = binder.GetComponent<Button>();
            }

            if (resolvedButton == null)
            {
                EditorGUILayout.HelpBox("Assign a Button or add UIFlowButtonAction to a GameObject with a Button.", MessageType.Error);
            }

            if (action.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a UIFlowAction asset. Create one from Assets > Create > Deucarian > UI Flow > Actions.", MessageType.Error);
            }

            if (host.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Host is optional. When empty, the action searches for a parent UIFlowHost. Dismiss actions instead use the containing UIFlowScreen.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
