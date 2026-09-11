using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Deucarian.UIFlow
{
    [AddComponentMenu("Deucarian/UI Flow/Screen Trigger")]
    public sealed class ScreenTrigger : MonoBehaviour
    {
        [SerializeField] private ScreenKey screen;
        [SerializeField] private UnityEvent completed = new UnityEvent();
        [SerializeField] private UnityEvent<string> rejected = new UnityEvent<string>();
        public ScreenKey Screen { get => screen; set => screen = value; }
        public Task<UIFlowNavigationResult> OpenAsync(CancellationToken cancellationToken = default) => Screens.OpenAsync(screen, cancellationToken: cancellationToken);
        public async void Open()
        {
            try
            {
                var result = await OpenAsync();
                if (this == null) return;
                if (result.Succeeded) completed.Invoke(); else rejected.Invoke(result.Message);
            }
            catch (Exception error) { UIFlowLog.Navigation.Exception(error, "ScreenTrigger could not open its screen.", this); if (this != null) rejected.Invoke(error.Message); }
        }
        public async void Back()
        {
            try { await Screens.BackAsync(); }
            catch (Exception error) { UIFlowLog.Navigation.Exception(error, "ScreenTrigger could not navigate back.", this); if (this != null) rejected.Invoke(error.Message); }
        }
    }
}
