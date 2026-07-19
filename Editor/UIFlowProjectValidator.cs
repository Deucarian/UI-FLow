using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    public static class UIFlowProjectValidator
    {
        public const string MenuRoot = "Tools/Deucarian/Experience and Interaction/UI and Presentation/UI Flow/";
        public const string MenuPath = MenuRoot + "Validate Project";

        [MenuItem(MenuPath)]
        public static void ValidateProject()
        {
            IReadOnlyList<string> messages = UIFlowEditorValidation.ValidateProject();
            if (messages.Count == 0)
            {
                UIFlowLog.Validation.Info("UI Flow project validation passed.");
                EditorUtility.DisplayDialog("UI Flow Validation", "No UI Flow issues were found.", "OK");
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].StartsWith("Error:"))
                {
                    UIFlowLog.Validation.Error(messages[i]);
                }
                else
                {
                    UIFlowLog.Validation.Warning(messages[i]);
                }
            }

            EditorUtility.DisplayDialog("UI Flow Validation", "UI Flow validation found " + messages.Count + " issue(s). See Console for details.", "OK");
        }
    }
}
