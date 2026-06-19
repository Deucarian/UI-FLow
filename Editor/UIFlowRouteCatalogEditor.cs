using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    [CustomEditor(typeof(UIFlowRouteCatalog))]
    public sealed class UIFlowRouteCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UIFlowRouteCatalog catalog = (UIFlowRouteCatalog)target;
            IReadOnlyList<string> errors = catalog.ValidateCatalog();
            for (int i = 0; i < errors.Count; i++)
            {
                EditorGUILayout.HelpBox(errors[i], MessageType.Error);
            }

            if (GUILayout.Button("Collect Routes In Project"))
            {
                CollectRoutes(catalog);
            }
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

            SerializedObject serialized = new SerializedObject(catalog);
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
