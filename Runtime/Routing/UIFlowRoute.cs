using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Extensible route asset describing a navigable UI destination.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Route", fileName = "UIFlowRoute")]
    public class UIFlowRoute : ScriptableObject
    {
        [SerializeField] private UIFlowRouteId _routeId;
        [SerializeField] private string _displayName;
        [SerializeField] private UIFlowChannelId _targetChannel = UIFlowChannelId.Main;
        [SerializeField] private UIFlowScreenSourceMode _sourceMode;
        [SerializeField] private UIFlowScreen _prefab;
        [SerializeField] private string _externalBindingId;
        [SerializeField] private string _customProviderId;
        [SerializeField] private UIFlowScreenLifetime _lifetime;
        [SerializeField] private UIFlowDuplicateRoutePolicy _duplicateRoutePolicy;
        [SerializeField] private UIFlowTransition _showTransitionOverride;
        [SerializeField] private UIFlowTransition _hideTransitionOverride;
        [SerializeField] private UIFlowGuard[] _guards = Array.Empty<UIFlowGuard>();
        [TextArea]
        [SerializeField] private string _notes;

        public UIFlowRouteId RouteId
        {
            get { return _routeId; }
        }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(_displayName) ? name : _displayName; }
        }

        public UIFlowChannelId TargetChannel
        {
            get { return _targetChannel; }
        }

        public UIFlowScreenSourceMode SourceMode
        {
            get { return _sourceMode; }
        }

        public UIFlowScreen Prefab
        {
            get { return _prefab; }
        }

        public string ExternalBindingId
        {
            get { return _externalBindingId; }
        }

        public string CustomProviderId
        {
            get { return _customProviderId; }
        }

        public UIFlowScreenLifetime Lifetime
        {
            get { return _lifetime; }
        }

        public UIFlowDuplicateRoutePolicy DuplicateRoutePolicy
        {
            get { return _duplicateRoutePolicy; }
        }

        public UIFlowTransition ShowTransitionOverride
        {
            get { return _showTransitionOverride; }
        }

        public UIFlowTransition HideTransitionOverride
        {
            get { return _hideTransitionOverride; }
        }

        public IReadOnlyList<UIFlowGuard> Guards
        {
            get { return _guards; }
        }

        public string Notes
        {
            get { return _notes; }
        }

        public void RegenerateRouteId()
        {
            _routeId = UIFlowRouteId.NewId();
        }

        private void Reset()
        {
            EnsureRouteId();
        }

        private void OnValidate()
        {
            EnsureRouteId();
            if (_sourceMode == UIFlowScreenSourceMode.ExternalSceneBinding)
            {
                _lifetime = UIFlowScreenLifetime.External;
            }
        }

        private void EnsureRouteId()
        {
            if (_routeId.IsEmpty)
            {
                _routeId = UIFlowRouteId.NewId();
            }
        }
    }
}
