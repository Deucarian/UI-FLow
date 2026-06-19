using System;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Stable channel identity inside a UI Flow host.
    /// </summary>
    [Serializable]
    public struct UIFlowChannelId : IEquatable<UIFlowChannelId>
    {
        [SerializeField] private string _value;

        public UIFlowChannelId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Channel ID cannot be empty or whitespace.", nameof(value));
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

        public static UIFlowChannelId Main
        {
            get { return new UIFlowChannelId("main"); }
        }

        public static UIFlowChannelId Modal
        {
            get { return new UIFlowChannelId("modal"); }
        }

        public static UIFlowChannelId Overlay
        {
            get { return new UIFlowChannelId("overlay"); }
        }

        public bool Equals(UIFlowChannelId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UIFlowChannelId && Equals((UIFlowChannelId)obj);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : _value.GetHashCode();
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(_value) ? "<empty-channel>" : _value;
        }

        public static bool operator ==(UIFlowChannelId left, UIFlowChannelId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UIFlowChannelId left, UIFlowChannelId right)
        {
            return !left.Equals(right);
        }
    }
}
