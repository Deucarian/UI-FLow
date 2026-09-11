using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using UnityEditor;
using UnityEngine;

namespace Deucarian.UIFlow.Editor.Definitions
{
    [Serializable]
    public sealed class ScreenDefinitionSpec : DeucarianDefinitionSpec
    {
        [DefinitionField("_targetChannel._value")] public string Channel = "main";
        [DefinitionField("_sourceMode")] public UIFlowScreenSourceMode Source;
        [DefinitionField("_prefab")] public UIFlowScreen Prefab;
        [DefinitionField("_externalBindingId")] public string ExternalBinding = string.Empty;
        [DefinitionField("_customProviderId")] public string CustomProvider = string.Empty;
        [DefinitionField("_lifetime")] public UIFlowScreenLifetime Lifetime;
        [DefinitionField("_duplicateRoutePolicy")] public UIFlowDuplicateRoutePolicy DuplicatePolicy = UIFlowDuplicateRoutePolicy.IgnoreIfTop;
        [DefinitionField("_showTransitionOverride")] public UIFlowTransition ShowTransition;
        [DefinitionField("_hideTransitionOverride")] public UIFlowTransition HideTransition;
        [DefinitionField("_guards")] public UIFlowGuard[] Guards = Array.Empty<UIFlowGuard>();
        [DefinitionField("_notes")] public string Notes = string.Empty;
    }

    public sealed class ScreenDefinitionSchema : DeucarianSerializedDefinitionSchema<UIFlowRoute, ScreenDefinitionSpec>
    {
        public override string Id => "screens";
        public override string DisplayName => "Screens";
        protected override string IdPath => "_routeId._value";
        protected override string NamePath => "_displayName";
        public override void Validate(DeucarianDefinitionSpec value)
        {
            base.Validate(value);
            var spec = (ScreenDefinitionSpec)value;
            if (string.IsNullOrWhiteSpace(spec.Channel)) throw new ArgumentException("Choose the screen's destination channel.");
            if (spec.Source == UIFlowScreenSourceMode.ExternalSceneBinding && spec.Lifetime != UIFlowScreenLifetime.External) throw new ArgumentException("Scene-bound screens need External lifetime.");
        }
        public override void ValidateReady(DeucarianDefinitionSpec value)
        {
            Validate(value);
            var spec = (ScreenDefinitionSpec)value;
            if (spec.Source == UIFlowScreenSourceMode.Prefab && spec.Prefab == null) throw new InvalidOperationException("Assign a UIFlowScreen prefab to '" + spec.Name + "' in Definitions.");
            if (spec.Source == UIFlowScreenSourceMode.ExternalSceneBinding && string.IsNullOrWhiteSpace(spec.ExternalBinding)) throw new InvalidOperationException("Choose the scene binding for '" + spec.Name + "'.");
            if (spec.Source == UIFlowScreenSourceMode.CustomProvider && string.IsNullOrWhiteSpace(spec.CustomProvider)) throw new InvalidOperationException("Choose the custom screen provider for '" + spec.Name + "'.");
        }
        public override void RefreshCatalog(bool validateOnly = false)
        {
            var routes = AssetDatabase.FindAssets("t:UIFlowRoute", new[] { "Assets" }).Select(x => AssetDatabase.LoadAssetAtPath<UIFlowRoute>(AssetDatabase.GUIDToAssetPath(x)))
                .Where(x => x != null).OrderBy(x => x.RouteId.ToString(), StringComparer.Ordinal).ToArray();
            if (routes.GroupBy(x => x.RouteId).Any(x => x.Count() > 1)) throw new InvalidOperationException("Screen IDs must be unique.");
            DeucarianDefinitionCatalog.Update<UIFlowRouteCatalog>("Assets/DeucarianDefinitions/Resources/Deucarian/UIFlow/ProjectScreens.asset", "_routes", routes, validateOnly);
        }
        [MenuItem("Assets/Create/Deucarian/UI Flow/Route")]
        private static void CreateDefinition() { Selection.activeObject = DeucarianDefinitionSync.Create(new ScreenDefinitionSchema(), "NewScreen"); }
    }
}
