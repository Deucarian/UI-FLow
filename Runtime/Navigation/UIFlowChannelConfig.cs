using System;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Serialized channel configuration owned by a UI Flow host.
    /// </summary>
    [Serializable]
    public sealed class UIFlowChannelConfig
    {
        [SerializeField] private UIFlowChannelId _channelId = UIFlowChannelId.Main;
        [SerializeField] private UIFlowChannelKind _kind;
        [SerializeField] private Transform _root;
        [SerializeField] private UIFlowRoute _initialRoute;
        [SerializeField] private bool _participatesInBack = true;
        [SerializeField] private int _backPriority;
        [SerializeField] private bool _protectRootFromBack = true;
        [SerializeField] private UIFlowCoveredScreenBehavior _coveredScreenBehavior = UIFlowCoveredScreenBehavior.DeactivateCoveredScreen;
        [SerializeField] private UIFlowTransition _defaultShowTransition;
        [SerializeField] private UIFlowTransition _defaultHideTransition;
        [SerializeField] private UIFlowTransitionExecutionMode _transitionExecutionMode;
        [SerializeField] private UIFlowRequestConflictPolicy _defaultConflictPolicy;

        public UIFlowChannelId ChannelId
        {
            get { return _channelId; }
        }

        public UIFlowChannelKind Kind
        {
            get { return _kind; }
        }

        public Transform Root
        {
            get { return _root; }
        }

        public UIFlowRoute InitialRoute
        {
            get { return _initialRoute; }
        }

        public bool ParticipatesInBack
        {
            get { return _participatesInBack; }
        }

        public int BackPriority
        {
            get { return _backPriority; }
        }

        public bool ProtectRootFromBack
        {
            get { return _protectRootFromBack; }
        }

        public UIFlowCoveredScreenBehavior CoveredScreenBehavior
        {
            get { return _coveredScreenBehavior; }
        }

        public UIFlowTransition DefaultShowTransition
        {
            get { return _defaultShowTransition; }
        }

        public UIFlowTransition DefaultHideTransition
        {
            get { return _defaultHideTransition; }
        }

        public UIFlowTransitionExecutionMode TransitionExecutionMode
        {
            get { return _transitionExecutionMode; }
        }

        public UIFlowRequestConflictPolicy DefaultConflictPolicy
        {
            get { return _defaultConflictPolicy; }
        }
    }

    /// <summary>
    /// Serialized external scene binding used by external-scene routes.
    /// </summary>
    [Serializable]
    public sealed class UIFlowSceneScreenBinding
    {
        [SerializeField] private string _bindingId;
        [SerializeField] private UIFlowRoute _route;
        [SerializeField] private UIFlowScreen _screen;

        public string BindingId
        {
            get { return _bindingId; }
        }

        public UIFlowRoute Route
        {
            get { return _route; }
        }

        public UIFlowScreen Screen
        {
            get { return _screen; }
        }

        public bool Matches(UIFlowRoute route)
        {
            if (route == null)
            {
                return false;
            }

            if (_route == route)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(_bindingId) && string.Equals(_bindingId, route.ExternalBindingId, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
