// <deucarian-definition schema="screens" />
// Editable declaration. Use the package definition editor or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_screens
{
    public static class Definition_FlowSampleDetails
    {
        public static global::Deucarian.UIFlow.Editor.Definitions.ScreenDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.UIFlow.Editor.Definitions.ScreenDefinitionSpec
        {
            Channel = "main",
            CustomProvider = "",
            DuplicatePolicy = global::Deucarian.UIFlow.UIFlowDuplicateRoutePolicy.IgnoreIfTop,
            ExternalBinding = "",
            Guards = new global::Deucarian.UIFlow.UIFlowGuard[]
            {
            },
            HideTransition = null,
            Id = "ddbd0cae827c4bbbadcea0e63843a136",
            Lifetime = global::Deucarian.UIFlow.UIFlowScreenLifetime.Transient,
            Name = "FlowSampleDetails",
            Notes = "",
            Prefab = global::Deucarian.Editor.Definitions.DeucarianDefinitionAssets.Load<global::Deucarian.UIFlow.UIFlowScreen>("da76b945e39b64c45813f6a561b22ab3", 6458151488274767948L),
            ShowTransition = null,
            Source = global::Deucarian.UIFlow.UIFlowScreenSourceMode.Prefab,
        };
        // end-definition-value
    }
}
