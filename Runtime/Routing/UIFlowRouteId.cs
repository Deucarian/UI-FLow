using System;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Stable route identity that survives asset renames and moves.
    /// </summary>
    [Serializable]
    public struct UIFlowRouteId : IEquatable<UIFlowRouteId>
    {
        [SerializeField] private string _value;

        public UIFlowRouteId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Route ID cannot be empty or whitespace.", nameof(value));
            }

            _value = value.Trim();
        }

        public string Value
        {
            get { return _value; }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrWhiteSpace(_value); }
        }

        public static UIFlowRouteId NewId()
        {
            return new UIFlowRouteId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(UIFlowRouteId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UIFlowRouteId && Equals((UIFlowRouteId)obj);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : _value.GetHashCode();
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(_value) ? "<empty-route>" : _value;
        }

        public static bool operator ==(UIFlowRouteId left, UIFlowRouteId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UIFlowRouteId left, UIFlowRouteId right)
        {
            return !left.Equals(right);
        }
    }
}
