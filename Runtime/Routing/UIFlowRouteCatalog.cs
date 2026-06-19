using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// ScriptableObject catalog for route lookup by stable route ID.
    /// </summary>
    [CreateAssetMenu(menuName = "Deucarian/UI Flow/Route Catalog", fileName = "UIFlowRouteCatalog")]
    public sealed class UIFlowRouteCatalog : ScriptableObject
    {
        [SerializeField] private UIFlowRoute[] _routes = new UIFlowRoute[0];

        private Dictionary<UIFlowRouteId, UIFlowRoute> _runtimeLookup;

        public IReadOnlyList<UIFlowRoute> Routes
        {
            get { return _routes; }
        }

        public bool TryGetRoute(UIFlowRouteId routeId, out UIFlowRoute route)
        {
            EnsureLookup();
            return _runtimeLookup.TryGetValue(routeId, out route);
        }

        public IReadOnlyList<string> ValidateCatalog()
        {
            var errors = new List<string>();
            var seen = new HashSet<UIFlowRouteId>();

            for (int i = 0; i < _routes.Length; i++)
            {
                UIFlowRoute route = _routes[i];
                if (route == null)
                {
                    errors.Add("Route catalog contains a null route at index " + i + ".");
                    continue;
                }

                if (route.RouteId.IsEmpty)
                {
                    errors.Add("Route '" + route.name + "' has an empty route ID.");
                    continue;
                }

                if (!seen.Add(route.RouteId))
                {
                    errors.Add("Duplicate route ID '" + route.RouteId + "' in catalog.");
                }
            }

            return errors;
        }

        public IReadOnlyDictionary<UIFlowRouteId, UIFlowRoute> BuildRuntimeLookup()
        {
            EnsureLookup();
            return _runtimeLookup;
        }

        private void EnsureLookup()
        {
            if (_runtimeLookup != null)
            {
                return;
            }

            _runtimeLookup = new Dictionary<UIFlowRouteId, UIFlowRoute>();
            for (int i = 0; i < _routes.Length; i++)
            {
                UIFlowRoute route = _routes[i];
                if (route == null || route.RouteId.IsEmpty)
                {
                    continue;
                }

                if (!_runtimeLookup.ContainsKey(route.RouteId))
                {
                    _runtimeLookup.Add(route.RouteId, route);
                }
            }
        }

        private void OnValidate()
        {
            _runtimeLookup = null;
        }
    }
}
