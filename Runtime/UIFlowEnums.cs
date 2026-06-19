namespace Deucarian.UIFlow
{
    /// <summary>
    /// Describes the role of a host channel.
    /// </summary>
    public enum UIFlowChannelKind
    {
        Main,
        Modal,
        Overlay,
        Custom
    }

    /// <summary>
    /// Describes how a route resolves its screen.
    /// </summary>
    public enum UIFlowScreenSourceMode
    {
        Prefab,
        ExternalSceneBinding,
        CustomProvider
    }

    /// <summary>
    /// Describes how long a route's screen instance may live.
    /// </summary>
    public enum UIFlowScreenLifetime
    {
        Transient,
        Cached,
        External
    }

    /// <summary>
    /// Controls what happens when a route is requested while an existing stack entry already uses it.
    /// </summary>
    public enum UIFlowDuplicateRoutePolicy
    {
        Allow,
        RejectIfPresent,
        IgnoreIfTop,
        PopToExisting
    }

    /// <summary>
    /// Controls visual handling for a screen that is covered by another stack entry.
    /// </summary>
    public enum UIFlowCoveredScreenBehavior
    {
        DeactivateCoveredScreen,
        KeepActiveButNonInteractable
    }

    /// <summary>
    /// Controls how show and hide transitions are composed.
    /// </summary>
    public enum UIFlowTransitionExecutionMode
    {
        HideThenShow,
        Parallel
    }

    /// <summary>
    /// Identifies a navigation operation.
    /// </summary>
    public enum UIFlowOperationType
    {
        Push,
        Pop,
        Replace,
        Reset,
        PopTo,
        Back,
        Present,
        Dismiss,
        System
    }

    /// <summary>
    /// Captures why an operation was requested.
    /// </summary>
    public enum UIFlowNavigationReason
    {
        Programmatic,
        User,
        Back,
        Initialization,
        Redirect,
        System
    }

    /// <summary>
    /// Controls how a request behaves when another request is active or queued.
    /// </summary>
    public enum UIFlowRequestConflictPolicy
    {
        Queue,
        RejectIfBusy,
        CoalesceEquivalent
    }

    /// <summary>
    /// Structured status for navigation results.
    /// </summary>
    public enum UIFlowNavigationStatus
    {
        Succeeded,
        NoOp,
        Rejected,
        GuardDenied,
        Cancelled
    }

    /// <summary>
    /// Structured status for modal presentation results.
    /// </summary>
    public enum UIFlowPresentationStatus
    {
        Completed,
        Dismissed,
        Cancelled,
        HostDestroyed,
        Removed
    }

    /// <summary>
    /// Explains why a presentation ended without a typed value.
    /// </summary>
    public enum UIFlowDismissalReason
    {
        None,
        Dismissed,
        Back,
        CallerCancelled,
        HostDestroyed,
        RemovedByReplacementOrReset,
        EntryReleased,
        NavigationRejected
    }

    /// <summary>
    /// Runtime lifecycle state for a screen instance.
    /// </summary>
    public enum UIFlowScreenLifecycleState
    {
        Unbound,
        Preparing,
        Entering,
        Active,
        Covering,
        Covered,
        Revealing,
        Exiting,
        Released
    }

    /// <summary>
    /// Identifies a guard decision.
    /// </summary>
    public enum UIFlowGuardDecisionType
    {
        Allow,
        Deny,
        Redirect
    }

    /// <summary>
    /// Identifies a transition direction.
    /// </summary>
    public enum UIFlowTransitionDirection
    {
        Show,
        Hide
    }

    /// <summary>
    /// Runtime initialization state for a host.
    /// </summary>
    public enum UIFlowInitializationState
    {
        NotInitialized,
        Initializing,
        Initialized,
        Failed,
        Shutdown
    }
}
