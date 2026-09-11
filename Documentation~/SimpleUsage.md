# Simple usage

Add ScreensHost to your already configured UIFlowHost. Put the settings route in its route catalog with ID settings. Screens keeps the existing router, guards, queue, transitions, and back behavior. The actual return type is Task<UIFlowNavigationResult>, so callers can inspect rejected/cancelled navigation when needed. Unknown IDs throw before navigation. CancellationToken is optional. Additional independent UI flows keep their own host references.

Import the **Basic Flow** sample from Unity Package Manager. Its caller script is:

```csharp
using UnityEngine;

namespace Deucarian.UIFlow.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        public System.Threading.Tasks.Task OpenSettings() => Screens.OpenAsync("settings");
        public System.Threading.Tasks.Task GoBack() => Screens.BackAsync();
    }
}
```
