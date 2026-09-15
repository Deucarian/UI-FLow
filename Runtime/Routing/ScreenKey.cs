using System;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>A declared Screen identity. Reuse a named definition or select it in the Inspector.</summary>
    [Serializable]
    public class ScreenKey : IEquatable<ScreenKey>
    {
        [SerializeField] private string definitionId;

        /// <summary>For central definition sets and generated declarations; ordinary callers reuse those keys.</summary>
        protected ScreenKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                throw new ArgumentException("A ScreenKey definition needs a non-empty stable ID without surrounding whitespace.", nameof(id));
            definitionId = id;
        }

        public string Id => !string.IsNullOrWhiteSpace(definitionId) ? definitionId :
            throw new InvalidOperationException("No ScreenKey is selected. Select an existing definition in the Inspector or assign a named key from a ScreenKeySet declaration.");
        public bool Equals(ScreenKey other) => other != null && string.Equals(definitionId, other.definitionId, StringComparison.Ordinal);
        public override bool Equals(object other) => other is ScreenKey key && Equals(key);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(definitionId ?? string.Empty);
        public override string ToString() => definitionId ?? string.Empty;
    }
}
