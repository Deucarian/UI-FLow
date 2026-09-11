using Deucarian.Editor;
using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowRouteCatalog))]
    public sealed class UIFlowRouteCatalogEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Route catalog");
            DeucarianEditorInspector.Properties(root, serializedObject);
            UIFlowRouteCatalog catalog = (UIFlowRouteCatalog)target;
            var feedback = new VisualElement(); root.Add(feedback);
            void Refresh()
            {
                feedback.Clear();
                if (catalog == null) return;
                foreach (string error in catalog.ValidateCatalog()) feedback.Add(new HelpBox(error, HelpBoxMessageType.Error));
            }
            root.Add(DeucarianEditorWorkspaceControls.Button("Collect project routes", () =>
            {
                if (catalog == null) return;
                CollectRoutes(catalog); Refresh();
            }));
            DeucarianEditorInspector.Observe(root, serializedObject, Refresh);
            return root;
        }

        private static void CollectRoutes(UIFlowRouteCatalog catalog)
        {
            string[] guids = AssetDatabase.FindAssets("t:UIFlowRoute");
            var routes = new List<UIFlowRoute>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UIFlowRoute route = AssetDatabase.LoadAssetAtPath<UIFlowRoute>(path);
                if (route != null)
                {
                    routes.Add(route);
                }
            }

            using var serialized = new SerializedObject(catalog);
            SerializedProperty property = serialized.FindProperty("_routes");
            property.arraySize = routes.Count;
            for (int i = 0; i < routes.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = routes[i];
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }
    }
}
