using System;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.UIFlow.Editor
{
    [CustomPropertyDrawer(typeof(ScreenKey), true)]
    public sealed class ScreenKeyDrawer : DeucarianKeyDrawer
    {
        public override Type KeyType => typeof(ScreenKey);
        public override Type DefinitionSetAttribute => typeof(ScreenKeySetAttribute);
        public override string SetupHint => "Select an existing ScreenKey; declare reusable keys once in a [ScreenKeySet] class.";
    }
}
