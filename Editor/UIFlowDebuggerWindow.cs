using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.UIFlow.Editor
{
    public sealed class UIFlowDebuggerWindow : EditorWindow
    {
        private DeucarianEditorPageSession session;

        public static void Open() => DeucarianEditorToolWindow.Open("deucarian.ui-flow.debugger");
        public static IDeucarianEditorPage CreatePage() => new UIFlowDebuggerPage().Page;

        private void CreateGUI()
        {
            session?.Dispose();
            session = new DeucarianEditorPageSession(this, "deucarian.ui-flow.debugger", CreatePage());
        }

        private void OnDisable() { session?.Dispose(); session = null; }
    }
}
