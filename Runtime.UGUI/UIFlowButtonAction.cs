using System;
using System.Threading;
using Deucarian.UIFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Reusable scene-facing binder that connects a Unity UI Button click to a UI Flow action asset.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIFlowButtonAction : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private UIFlowAction _action;
        [SerializeField] private UIFlowHost _host;
        [SerializeField] private bool _disableWhileRunning = true;

        private bool _running;
        private CancellationTokenSource _runCts;

        public Button Button
        {
            get { return _button; }
        }

        public UIFlowAction Action
        {
            get { return _action; }
        }

        private void Awake()
        {
            ResolveButton();
        }

        private void OnEnable()
        {
            ResolveButton();
            if (_button == null)
            {
                UIFlowLog.UGUI.Warning("UIFlowButtonAction requires a Button reference.", this);
                return;
            }

            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }

            if (_runCts != null)
            {
                _runCts.Cancel();
            }
        }

        private void Reset()
        {
            ResolveButton();
        }

        private void OnValidate()
        {
            ResolveButton();
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

            if (_action == null)
            {
                UIFlowLog.UGUI.Warning("UIFlowButtonAction requires a UIFlowAction asset.", this);
                return;
            }

            try
            {
                _running = true;
                SetButtonInteractable(false);
                _runCts = new CancellationTokenSource();
                var context = new UIFlowActionContext(this, _button, _host);
                await _action.ExecuteAsync(context, _runCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                UIFlowLog.UGUI.Exception(ex, null, this);
            }
            finally
            {
                if (_runCts != null)
                {
                    _runCts.Dispose();
                    _runCts = null;
                }

                _running = false;
                SetButtonInteractable(true);
            }
        }

        private void ResolveButton()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (_disableWhileRunning && _button != null)
            {
                _button.interactable = interactable;
            }
        }
    }
}
