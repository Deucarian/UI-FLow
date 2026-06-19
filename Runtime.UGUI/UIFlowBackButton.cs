using System;
using Deucarian.UIFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Button adapter for host-level Back routing.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIFlowBackButton : MonoBehaviour
    {
        [SerializeField] private UIFlowHost _host;
        [SerializeField] private bool _useNearestParentHost = true;
        [SerializeField] private UIFlowNavigationOptions _options;

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

            UIFlowHost host = ResolveHost();
            if (host == null)
            {
                Debug.LogWarning("UIFlowBackButton requires a UIFlowHost reference or a parent host.", this);
                return;
            }

            try
            {
                _running = true;
                _button.interactable = false;
                await host.BackAsync(_options);
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

        private UIFlowHost ResolveHost()
        {
            if (_host != null)
            {
                return _host;
            }

            return _useNearestParentHost ? GetComponentInParent<UIFlowHost>() : null;
        }
    }
}
