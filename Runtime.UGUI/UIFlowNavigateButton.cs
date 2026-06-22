using System;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Button adapter that requests Push, Replace, or Reset without referencing another screen.
    /// </summary>
    [Obsolete("Use UIFlowButtonAction with a UIFlowPushRouteAction, UIFlowReplaceRouteAction, or UIFlowResetRouteAction asset instead.")]
    [RequireComponent(typeof(Button))]
    public sealed class UIFlowNavigateButton : MonoBehaviour
    {
        [SerializeField] private UIFlowHost _host;
        [SerializeField] private bool _useNearestParentHost = true;
        [SerializeField] private UIFlowRoute _route;
        [SerializeField] private UIFlowNavigateButtonOperation _operation;
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
                UIFlowLog.UGUI.Warning("UIFlowNavigateButton requires a UIFlowHost reference or a parent host.", this);
                return;
            }

            if (_route == null)
            {
                UIFlowLog.UGUI.Warning("UIFlowNavigateButton requires a route.", this);
                return;
            }

            try
            {
                _running = true;
                SetButtonInteractable(false);

                if (_operation == UIFlowNavigateButtonOperation.Replace)
                {
                    await host.ReplaceAsync(_route, null, _options);
                }
                else if (_operation == UIFlowNavigateButtonOperation.Reset)
                {
                    await host.ResetAsync(_route.TargetChannel, _route, null, _options);
                }
                else
                {
                    await host.PushAsync(_route, null, _options);
                }
            }
            catch (Exception ex)
            {
                UIFlowLog.UGUI.Exception(ex, null, this);
            }
            finally
            {
                _running = false;
                SetButtonInteractable(true);
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

        private void SetButtonInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }
    }

    [Obsolete("Use UIFlowButtonAction with route action assets instead.")]
    public enum UIFlowNavigateButtonOperation
    {
        Push,
        Replace,
        Reset
    }
}
