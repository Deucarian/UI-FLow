using System;
using Deucarian.UIFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Button adapter that dismisses the containing active UI Flow screen entry.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIFlowDismissButton : MonoBehaviour
    {
        private Button _button;
        private bool _running;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            RunAsync();
        }

        private async void RunAsync()
        {
            if (_running)
            {
                return;
            }

            UIFlowScreen screen = GetComponentInParent<UIFlowScreen>();
            if (screen == null || !screen.HasContext)
            {
                Debug.LogWarning("UIFlowDismissButton must be inside an active UIFlowScreen context.", this);
                return;
            }

            try
            {
                _running = true;
                _button.interactable = false;
                await screen.DismissAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
            finally
            {
                _running = false;
                if (_button != null)
                {
                    _button.interactable = true;
                }
            }
        }
    }
}
