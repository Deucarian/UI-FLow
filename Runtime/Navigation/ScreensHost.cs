using System;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>Add to the application's configured UIFlowHost to expose the Screens entry point.</summary>
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent, RequireComponent(typeof(UIFlowHost))]
    public sealed class ScreensHost : MonoBehaviour
    {
        private IDisposable registration;
        private void OnEnable() { registration = Screens.Bind(GetComponent<UIFlowHost>()); }
        private void OnDisable() { registration?.Dispose(); registration = null; }
    }
}
