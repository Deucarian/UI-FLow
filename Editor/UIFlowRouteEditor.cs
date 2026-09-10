using Deucarian.Editor;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowRoute), true)]
    public sealed class UIFlowRouteEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIFlowRoute route = (UIFlowRoute)target;
            DeucarianEditorTextGUI.LabelField("Stable Route ID", route.RouteId.ToString(), DeucarianEditorWorkbenchGUI.LabelStyle);

            EditorGUILayout.BeginHorizontal();
            if (DeucarianEditorActionGUI.Button("Copy Route ID"))
            {
                EditorGUIUtility.systemCopyBuffer = route.RouteId.ToString();
            }

            if (DeucarianEditorActionGUI.Button("Regenerate Route ID"))
            {
                if (EditorUtility.DisplayDialog(
                    "Regenerate UI Flow route ID?",
                    "Regenerating the route ID can break saved route references, catalogs, and code that stores this ID. Continue only when intentionally creating a new identity.",
                    "Regenerate",
                    "Cancel"))
                {
                    Undo.RecordObject(route, "Regenerate UI Flow Route ID");
                    route.RegenerateRouteId();
                    EditorUtility.SetDirty(route);
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawPropertiesExcluding(serializedObject, "m_Script", "_routeId");
            DrawValidation(route);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawValidation(UIFlowRoute route)
        {
            if (route.RouteId.IsEmpty)
            {
                DeucarianEditorTextGUI.HelpBox("Route ID is empty. UI Flow route assets should have a stable generated ID.", MessageType.Error);
            }

            if (route.TargetChannel.IsEmpty)
            {
                DeucarianEditorTextGUI.HelpBox("Target channel is empty.", MessageType.Error);
            }

            if (route.SourceMode == UIFlowScreenSourceMode.Prefab && route.Prefab == null)
            {
                DeucarianEditorTextGUI.HelpBox("Prefab routes require a UIFlowScreen prefab.", MessageType.Error);
            }

            if (route.SourceMode == UIFlowScreenSourceMode.ExternalSceneBinding && string.IsNullOrWhiteSpace(route.ExternalBindingId))
            {
                DeucarianEditorTextGUI.HelpBox("External scene binding routes should define an external binding ID.", MessageType.Warning);
            }

            if (route.SourceMode == UIFlowScreenSourceMode.CustomProvider && string.IsNullOrWhiteSpace(route.CustomProviderId))
            {
                DeucarianEditorTextGUI.HelpBox("Custom provider routes require a provider ID.", MessageType.Error);
            }

            if (route.Lifetime != UIFlowScreenLifetime.Transient && route.DuplicateRoutePolicy == UIFlowDuplicateRoutePolicy.Allow)
            {
                DeucarianEditorTextGUI.HelpBox("Cached and external routes cannot safely allow duplicate active entries. Use RejectIfPresent, IgnoreIfTop, or PopToExisting.", MessageType.Warning);
            }

            if (route.Prefab != null && DeucarianEditorActionGUI.Button("Ping Prefab"))
            {
                EditorGUIUtility.PingObject(route.Prefab);
            }
        }
    }
}
