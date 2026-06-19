using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    public static class UIFlowProjectValidator
    {
        [MenuItem("Tools/Deucarian/UI Flow/Validate Project")]
        public static void ValidateProject()
        {
            IReadOnlyList<string> messages = UIFlowEditorValidation.ValidateProject();
            if (messages.Count == 0)
            {
                Debug.Log("UI Flow project validation passed.");
                EditorUtility.DisplayDialog("UI Flow Validation", "No UI Flow issues were found.", "OK");
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].StartsWith("Error:"))
                {
                    Debug.LogError(messages[i]);
                }
                else
                {
                    Debug.LogWarning(messages[i]);
                }
            }

            EditorUtility.DisplayDialog("UI Flow Validation", "UI Flow validation found " + messages.Count + " issue(s). See Console for details.", "OK");
        }
    }
}
