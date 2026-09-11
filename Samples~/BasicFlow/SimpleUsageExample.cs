using UnityEngine;

namespace Deucarian.UIFlow.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        public System.Threading.Tasks.Task OpenSettings() => Screens.OpenAsync("settings");
        public System.Threading.Tasks.Task GoBack() => Screens.BackAsync();
    }
}
