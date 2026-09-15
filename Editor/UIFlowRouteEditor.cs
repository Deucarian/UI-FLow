using Deucarian.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowRoute), true)]
    public sealed class UIFlowRouteEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var route = (UIFlowRoute)target;
            var root = DeucarianEditorInspector.CreateToolkit("UI Flow route");
            var identity = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted");
            root.Add(identity);
            var feedback = new VisualElement();
            void Refresh()
            {
                feedback.Clear();
                if (route == null) return;
                identity.text = "Route ID · " + route.RouteId;
                void Warn(string message, HelpBoxMessageType tone = HelpBoxMessageType.Error)
                    => feedback.Add(new HelpBox(message, tone));
                if (route.RouteId.IsEmpty) Warn("Route ID is empty. Generate a stable identity.");
                if (route.TargetChannel.IsEmpty) Warn("Target channel is empty.");
                if (route.SourceMode == UIFlowScreenSourceMode.Prefab && route.Prefab == null)
                    Warn("Prefab routes require a UIFlowScreen prefab.");
                if (route.SourceMode == UIFlowScreenSourceMode.ExternalSceneBinding && string.IsNullOrWhiteSpace(route.ExternalBindingId))
                    Warn("Define an external binding ID.", HelpBoxMessageType.Warning);
                if (route.SourceMode == UIFlowScreenSourceMode.CustomProvider && string.IsNullOrWhiteSpace(route.CustomProviderId))
                    Warn("Custom provider routes require a provider ID.");
                if (route.Lifetime != UIFlowScreenLifetime.Transient && route.DuplicateRoutePolicy == UIFlowDuplicateRoutePolicy.Allow)
                    Warn("Cached and external routes cannot allow duplicate active entries. Use RejectIfPresent, IgnoreIfTop or PopToExisting.", HelpBoxMessageType.Warning);
                if (route.Prefab != null)
                    feedback.Add(DeucarianEditorWorkspaceControls.Button("Locate prefab", () => EditorGUIUtility.PingObject(route.Prefab)));
            }
            root.Add(DeucarianEditorWorkspaceControls.Actions(
                DeucarianEditorWorkspaceControls.Button("Copy ID", () =>
                {
                    if (route != null) EditorGUIUtility.systemCopyBuffer = route.RouteId.ToString();
                }),
                DeucarianEditorWorkspaceControls.Button("Regenerate ID", () =>
                {
                    if (route == null || !EditorUtility.DisplayDialog(
                        "Regenerate UI Flow route ID?",
                        "This can break saved route references, catalogs and code that stores this ID. Continue only when intentionally creating a new identity.",
                        "Regenerate", "Cancel")) return;
                    Undo.RecordObject(route, "Regenerate UI Flow Route ID");
                    route.RegenerateRouteId(); EditorUtility.SetDirty(route); Refresh();
                }, DeucarianEditorButtonRole.Destructive)));
            DeucarianEditorInspector.Properties(root, serializedObject, "_routeId");
            root.Add(feedback);
            DeucarianEditorInspector.Observe(root, serializedObject, Refresh);
            return root;
        }
    }
}
