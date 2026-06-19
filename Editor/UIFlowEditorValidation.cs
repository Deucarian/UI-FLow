using System.Collections.Generic;
using Deucarian.UIFlow;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor
{
    public static class UIFlowEditorValidation
    {
        public static IReadOnlyList<string> ValidateHost(UIFlowHost host)
        {
            var messages = new List<string>();
            if (host == null)
            {
                messages.Add("Error: Host is null.");
                return messages;
            }

            SerializedObject serialized = new SerializedObject(host);
            SerializedProperty channels = serialized.FindProperty("_channels");
            SerializedProperty catalog = serialized.FindProperty("_routeCatalog");

            if (catalog.objectReferenceValue == null)
            {
                messages.Add("Warning: Host has no route catalog. Direct route asset navigation still works, but route-ID lookup does not.");
            }

            var ids = new HashSet<string>();
            for (int i = 0; i < channels.arraySize; i++)
            {
                SerializedProperty channel = channels.GetArrayElementAtIndex(i);
                if (channel == null)
                {
                    messages.Add("Error: Channel at index " + i + " is null.");
                    continue;
                }

                SerializedProperty channelId = channel.FindPropertyRelative("_channelId").FindPropertyRelative("_value");
                SerializedProperty root = channel.FindPropertyRelative("_root");
                SerializedProperty initialRoute = channel.FindPropertyRelative("_initialRoute");

                string id = channelId == null ? null : channelId.stringValue;
                if (string.IsNullOrWhiteSpace(id))
                {
                    messages.Add("Error: Channel at index " + i + " has an empty channel ID.");
                }
                else if (!ids.Add(id))
                {
                    messages.Add("Error: Duplicate channel ID '" + id + "'.");
                }

                if (root.objectReferenceValue == null)
                {
                    messages.Add("Error: Channel '" + id + "' has no root transform.");
                }

                UIFlowRoute route = initialRoute.objectReferenceValue as UIFlowRoute;
                if (route != null && route.TargetChannel.Value != id)
                {
                    messages.Add("Error: Initial route '" + route.name + "' targets '" + route.TargetChannel + "' but channel is '" + id + "'.");
                }
            }

            ValidateSceneBindings(serialized, messages);
            ValidateProviders(serialized, messages);
            return messages;
        }

        public static IReadOnlyList<string> ValidateProject()
        {
            var messages = new List<string>();
            ValidateRoutes(messages);
            ValidateCatalogs(messages);
            return messages;
        }

        private static void ValidateRoutes(List<string> messages)
        {
            var routeIds = new HashSet<UIFlowRouteId>();
            string[] routeGuids = AssetDatabase.FindAssets("t:UIFlowRoute");
            for (int i = 0; i < routeGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(routeGuids[i]);
                UIFlowRoute route = AssetDatabase.LoadAssetAtPath<UIFlowRoute>(path);
                if (route == null)
                {
                    continue;
                }

                if (route.RouteId.IsEmpty)
                {
                    messages.Add("Error: Route has empty ID: " + path);
                }
                else if (!routeIds.Add(route.RouteId))
                {
                    messages.Add("Error: Duplicate route ID '" + route.RouteId + "' at " + path);
                }

                if (route.SourceMode == UIFlowScreenSourceMode.Prefab && route.Prefab == null)
                {
                    messages.Add("Error: Prefab route missing prefab: " + path);
                }

                if (route.SourceMode == UIFlowScreenSourceMode.CustomProvider && string.IsNullOrWhiteSpace(route.CustomProviderId))
                {
                    messages.Add("Error: Custom provider route missing provider ID: " + path);
                }

                if (route.Lifetime != UIFlowScreenLifetime.Transient && route.DuplicateRoutePolicy == UIFlowDuplicateRoutePolicy.Allow)
                {
                    messages.Add("Warning: Cached/external route allows duplicates: " + path);
                }
            }
        }

        private static void ValidateCatalogs(List<string> messages)
        {
            string[] catalogGuids = AssetDatabase.FindAssets("t:UIFlowRouteCatalog");
            for (int i = 0; i < catalogGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(catalogGuids[i]);
                UIFlowRouteCatalog catalog = AssetDatabase.LoadAssetAtPath<UIFlowRouteCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                IReadOnlyList<string> errors = catalog.ValidateCatalog();
                for (int errorIndex = 0; errorIndex < errors.Count; errorIndex++)
                {
                    messages.Add("Error: " + path + ": " + errors[errorIndex]);
                }
            }
        }

        private static void ValidateSceneBindings(SerializedObject serialized, List<string> messages)
        {
            SerializedProperty bindings = serialized.FindProperty("_sceneBindings");
            var bindingIds = new HashSet<string>();
            for (int i = 0; i < bindings.arraySize; i++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                SerializedProperty bindingId = binding.FindPropertyRelative("_bindingId");
                SerializedProperty screen = binding.FindPropertyRelative("_screen");
                string id = bindingId.stringValue;

                if (string.IsNullOrWhiteSpace(id))
                {
                    messages.Add("Warning: Scene binding at index " + i + " has no binding ID.");
                }
                else if (!bindingIds.Add(id))
                {
                    messages.Add("Error: Duplicate scene binding ID '" + id + "'.");
                }

                if (screen.objectReferenceValue == null)
                {
                    messages.Add("Error: Scene binding '" + id + "' has no screen.");
                }
            }
        }

        private static void ValidateProviders(SerializedObject serialized, List<string> messages)
        {
            SerializedProperty providers = serialized.FindProperty("_customProviderComponents");
            var providerIds = new HashSet<string>();
            for (int i = 0; i < providers.arraySize; i++)
            {
                MonoBehaviour component = providers.GetArrayElementAtIndex(i).objectReferenceValue as MonoBehaviour;
                if (component == null)
                {
                    messages.Add("Warning: Custom provider entry " + i + " is empty.");
                    continue;
                }

                IUIFlowScreenProvider provider = component as IUIFlowScreenProvider;
                if (provider == null)
                {
                    messages.Add("Error: Custom provider '" + component.name + "' does not implement IUIFlowScreenProvider.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(provider.ProviderId))
                {
                    messages.Add("Error: Custom provider '" + component.name + "' has an empty ProviderId.");
                }
                else if (!providerIds.Add(provider.ProviderId))
                {
                    messages.Add("Error: Duplicate custom provider ID '" + provider.ProviderId + "'.");
                }
            }
        }
    }
}
