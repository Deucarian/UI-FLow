using System;
using Deucarian.Editor;

namespace Deucarian.UIFlow.Editor
{
    public sealed class ScreenKeySource : DeucarianAssetKeySource<UIFlowRoute>
    {
        public override Type KeyType => typeof(ScreenKey);
        public override Type DefinitionSetAttribute => typeof(ScreenKeySetAttribute);
        public override string GeneratedClassName => "ProjectScreens";
        protected override DeucarianKeyChoice ReadDefinition(UIFlowRoute asset) =>
            new DeucarianKeyChoice(asset.RouteId.ToString(), asset.DisplayName);
    }
}
