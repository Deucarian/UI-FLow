using System;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;

namespace Deucarian.UIFlow.Samples.BasicFlow
{
    public sealed class BasicFlowSampleController : MonoBehaviour
    {
        [SerializeField] private UIFlowHost _host;
        [SerializeField] private UIFlowRoute _mainMenuRoute;
        [SerializeField] private UIFlowRoute _settingsRoute;
        [SerializeField] private UIFlowRoute _audioRoute;
        [SerializeField] private UIFlowRoute _controlsRoute;
        [SerializeField] private UIFlowRoute _confirmQuitRoute;
        [SerializeField] private UIFlowRoute _infoDialogRoute;
        [SerializeField] private UIFlowRoute _loadingOverlayRoute;

        public void OpenSettings()
        {
            Run(() => _host.PushAsync(_settingsRoute, new BasicFlowScreenArguments("Opened from Main Menu")));
        }

        public void OpenAudio()
        {
            Run(() => _host.PushAsync(_audioRoute, new BasicFlowScreenArguments("Audio settings")));
        }

        public void OpenControls()
        {
            Run(() => _host.ReplaceAsync(_controlsRoute, new BasicFlowScreenArguments("Controls replaced the current settings screen")));
        }

        public void ResetToMainMenu()
        {
            Run(() => _host.ResetAsync(UIFlowChannelId.Main, _mainMenuRoute));
        }

        public void ShowInfoDialog()
        {
            Run(() => _host.PushAsync(_infoDialogRoute));
        }

        public void ShowLoadingOverlay()
        {
            Run(() => _host.PushAsync(_loadingOverlayRoute));
        }

        public void HideOverlay()
        {
            Run(() => _host.PopAsync(UIFlowChannelId.Overlay));
        }

        public void Back()
        {
            Run(() => _host.BackAsync());
        }

        public void ConfirmQuit()
        {
            Run(async () =>
            {
                UIFlowPresentationResult<bool> result = await _host.PresentAsync<bool>(_confirmQuitRoute);
                if (result.HasValue && result.Value)
                {
                    Debug.Log("Quit confirmed by Basic Flow sample.");
                }
            });
        }

        private async void Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }
    }

    [Serializable]
    public sealed class BasicFlowScreenArguments
    {
        public BasicFlowScreenArguments(string message)
        {
            Message = message;
        }

        public string Message;
    }
}
