using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// CanvasGroup alpha transition without tweening dependencies.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Transitions/Canvas Group Fade", fileName = "UIFlowCanvasGroupFadeTransition")]
    public sealed class UIFlowCanvasGroupFadeTransition : UIFlowTransition
    {
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool _useUnscaledTime = true;

        public float Duration
        {
            get { return _duration; }
        }

        public bool UseUnscaledTime
        {
            get { return _useUnscaledTime; }
        }

        public override Task ShowAsync(UIFlowTransitionContext context, CancellationToken cancellationToken)
        {
            return FadeAsync(context, 1f, true, cancellationToken);
        }

        public override Task HideAsync(UIFlowTransitionContext context, CancellationToken cancellationToken)
        {
            return FadeAsync(context, 0f, false, cancellationToken);
        }

        public override void ForceShown(UIFlowTransitionContext context)
        {
            ApplyShown(context.Screen);
        }

        public override void ForceHidden(UIFlowTransitionContext context)
        {
            ApplyHidden(context.Screen);
        }

        private async Task FadeAsync(UIFlowTransitionContext context, float targetAlpha, bool targetVisible, CancellationToken cancellationToken)
        {
            UIFlowScreen screen = context.Screen;
            if (screen == null)
            {
                return;
            }

            CanvasGroup group = screen.EnsureCanvasGroup();
            screen.gameObject.SetActive(true);
            group.interactable = false;
            group.blocksRaycasts = false;

            if (!context.Animate || _duration <= 0f)
            {
                group.alpha = targetAlpha;
                screen.SetVisibleImmediate(targetVisible, targetVisible);
                return;
            }

            float startAlpha = group.alpha;
            float elapsed = 0f;

            try
            {
                while (elapsed < _duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float normalized = Mathf.Clamp01(elapsed / _duration);
                    float curveValue = _curve == null ? normalized : _curve.Evaluate(normalized);
                    group.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
                    await Task.Yield();
                    elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                }

                group.alpha = targetAlpha;
                screen.SetVisibleImmediate(targetVisible, targetVisible);
            }
            catch
            {
                if (targetVisible)
                {
                    screen.SetVisibleImmediate(true, true);
                }
                else
                {
                    screen.SetVisibleImmediate(false, false);
                }

                throw;
            }
        }
    }
}
