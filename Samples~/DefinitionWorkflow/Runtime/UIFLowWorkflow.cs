using System;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.DefinitionWorkflow
{
    /// <summary>Small caller example. The configured scene hosts own services and resource lifetimes.</summary>
    public sealed class UIFLowWorkflow : MonoBehaviour
    {
        [SerializeField] private ScreenKey screen;
        [SerializeField] private ScreenTrigger trigger;
        private string status = "Ready. Choose an action below.";
        public string Status => status;
        public async void Open() { var result = await Screens.OpenAsync(screen); status = "Open: " + result; }
        public void OpenComponent() { trigger.Open(); status = "Screen requested through ScreenTrigger."; }
        public async void Back() { var result = await Screens.BackAsync(); status = "Back: " + result; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Math.Min(540, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("UI-FLow — definition workflow");
            GUILayout.Label("The screen definition stores its prefab and navigation defaults. Callers use the generated screen key or the same typed Inspector selection.");
            GUILayout.Space(12);
            if (GUILayout.Button("Open with C#", GUILayout.Height(32))) { try { Open(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Open from component", GUILayout.Height(32))) { try { OpenComponent(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Back", GUILayout.Height(32))) { try { Back(); } catch (Exception error) { status = error.Message; } }
            GUILayout.Space(12);
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
