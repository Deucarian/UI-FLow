using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Base component for screens controlled by UI Flow.
    /// </summary>
    public class UIFlowScreen : MonoBehaviour
    {
        private UIFlowContext _context;
        private UIFlowScreenLifecycleState _state;
        private CanvasGroup _canvasGroup;
        private bool _interactable = true;

        public UIFlowScreenLifecycleState State
        {
            get { return _state; }
        }

        public UIFlowContext Context
        {
            get { return _context; }
        }

        public bool HasContext
        {
            get { return _context != null; }
        }

        public bool IsInteractable
        {
            get { return _interactable; }
        }

        public bool TryGetArguments<T>(out T value)
        {
            if (_context == null)
            {
                value = default(T);
                return false;
            }

            return _context.TryGetArguments<T>(out value);
        }

        public T GetRequiredArguments<T>()
        {
            if (_context == null)
            {
                throw new UIFlowUsageException("Screen '" + name + "' does not currently have a UI Flow context.");
            }

            return _context.GetRequiredArguments<T>();
        }

        public Task<UIFlowNavigationResult> CloseAsync<TResult>(TResult result, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_context == null)
            {
                return Task.FromResult(UIFlowNavigationResult.Rejected(0, UIFlowOperationType.Dismiss, default(UIFlowChannelId), default(UIFlowRouteId), "Screen has already been released."));
            }

            return _context.CloseAsync(result, cancellationToken);
        }

        public Task<UIFlowNavigationResult> DismissAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_context == null)
            {
                return Task.FromResult(UIFlowNavigationResult.Rejected(0, UIFlowOperationType.Dismiss, default(UIFlowChannelId), default(UIFlowRouteId), "Screen has already been released."));
            }

            return _context.DismissAsync(cancellationToken);
        }

        protected virtual Task OnPrepareAsync(UIFlowContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnBeforeEnterAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnAfterEnter()
        {
        }

        protected virtual Task OnBeforeCoverAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnAfterCover()
        {
        }

        protected virtual Task OnBeforeRevealAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnAfterReveal()
        {
        }

        protected virtual Task OnBeforeExitAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnAfterExit()
        {
        }

        protected virtual void OnReleased()
        {
        }

        protected virtual Task<UIFlowGuardResult> OnExitGuardAsync(UIFlowGuardContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(UIFlowGuardResult.Allow());
        }

        internal Task PrepareAsync(UIFlowContext context, CancellationToken cancellationToken)
        {
            _context = context;
            SetState(UIFlowScreenLifecycleState.Preparing);
            return OnPrepareAsync(context, cancellationToken);
        }

        internal Task BeforeEnterAsync(CancellationToken cancellationToken)
        {
            SetState(UIFlowScreenLifecycleState.Entering);
            return OnBeforeEnterAsync(cancellationToken);
        }

        internal void AfterEnter()
        {
            SetState(UIFlowScreenLifecycleState.Active);
            SafeAfterHook(OnAfterEnter, "OnAfterEnter");
        }

        internal Task BeforeCoverAsync(CancellationToken cancellationToken)
        {
            SetState(UIFlowScreenLifecycleState.Covering);
            return OnBeforeCoverAsync(cancellationToken);
        }

        internal void AfterCover()
        {
            SetState(UIFlowScreenLifecycleState.Covered);
            SafeAfterHook(OnAfterCover, "OnAfterCover");
        }

        internal Task BeforeRevealAsync(CancellationToken cancellationToken)
        {
            SetState(UIFlowScreenLifecycleState.Revealing);
            return OnBeforeRevealAsync(cancellationToken);
        }

        internal void AfterReveal()
        {
            SetState(UIFlowScreenLifecycleState.Active);
            SafeAfterHook(OnAfterReveal, "OnAfterReveal");
        }

        internal Task BeforeExitAsync(CancellationToken cancellationToken)
        {
            SetState(UIFlowScreenLifecycleState.Exiting);
            return OnBeforeExitAsync(cancellationToken);
        }

        internal void AfterExit()
        {
            SafeAfterHook(OnAfterExit, "OnAfterExit");
        }

        internal void ReleaseContext()
        {
            _context = null;
            SetState(UIFlowScreenLifecycleState.Released);
            SafeAfterHook(OnReleased, "OnReleased");
        }

        internal Task<UIFlowGuardResult> EvaluateExitGuardAsync(UIFlowGuardContext context, CancellationToken cancellationToken)
        {
            return OnExitGuardAsync(context, cancellationToken);
        }

        internal void ForceUnbound()
        {
            _context = null;
            SetState(UIFlowScreenLifecycleState.Unbound);
        }

        internal CanvasGroup EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            return _canvasGroup;
        }

        internal void SetVisibleImmediate(bool visible, bool interactable)
        {
            if (this == null)
            {
                return;
            }

            gameObject.SetActive(visible);
            CanvasGroup group = EnsureCanvasGroup();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && interactable;
            group.blocksRaycasts = visible && interactable;
            _interactable = visible && interactable;
        }

        internal void SetInteractable(bool interactable)
        {
            if (this == null)
            {
                return;
            }

            CanvasGroup group = EnsureCanvasGroup();
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
            _interactable = interactable;
        }

        private void SetState(UIFlowScreenLifecycleState state)
        {
            _state = state;
        }

        private void SafeAfterHook(Action callback, string callbackName)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                UIFlowLog.Screens.Exception(new UIFlowNavigationException(_context == null ? "<released>" : _context.Route.TargetChannel.ToString(), callbackName, _context == null ? "<released>" : _context.Route.RouteId.ToString(), "A screen notification hook threw after state was committed.", ex), null, this);
            }
        }
    }
}
