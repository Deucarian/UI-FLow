using System;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Per-request navigation options.
    /// </summary>
    [Serializable]
    public struct UIFlowNavigationOptions
    {
        public bool? AnimateOverride;
        public UIFlowRequestConflictPolicy ConflictPolicy;
        public UIFlowNavigationReason Reason;
        public string DiagnosticLabel;

        public bool Animate
        {
            get { return !AnimateOverride.HasValue || AnimateOverride.Value; }
        }

        public static UIFlowNavigationOptions Default
        {
            get
            {
                return new UIFlowNavigationOptions
                {
                    ConflictPolicy = UIFlowRequestConflictPolicy.Queue,
                    Reason = UIFlowNavigationReason.Programmatic
                };
            }
        }

        public static UIFlowNavigationOptions Instant(UIFlowNavigationReason reason = UIFlowNavigationReason.Programmatic)
        {
            return new UIFlowNavigationOptions
            {
                AnimateOverride = false,
                ConflictPolicy = UIFlowRequestConflictPolicy.Queue,
                Reason = reason
            };
        }

        internal UIFlowNavigationOptions Normalize()
        {
            if (Reason == 0)
            {
                Reason = UIFlowNavigationReason.Programmatic;
            }

            return this;
        }
    }

    /// <summary>
    /// Per-request modal presentation options.
    /// </summary>
    [Serializable]
    public struct UIFlowPresentationOptions
    {
        public UIFlowNavigationOptions NavigationOptions;
        public bool? CloseOnCallerCancellationOverride;

        public bool CloseOnCallerCancellation
        {
            get { return !CloseOnCallerCancellationOverride.HasValue || CloseOnCallerCancellationOverride.Value; }
        }

        public static UIFlowPresentationOptions Default
        {
            get
            {
                return new UIFlowPresentationOptions
                {
                    NavigationOptions = UIFlowNavigationOptions.Default
                };
            }
        }

        internal UIFlowPresentationOptions Normalize()
        {
            NavigationOptions = NavigationOptions.Normalize();
            return this;
        }
    }
}
