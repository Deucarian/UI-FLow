using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    internal sealed class UIFlowStackState : IReadOnlyList<UIFlowStackEntry>
    {
        private readonly List<UIFlowStackEntry> _stack = new List<UIFlowStackEntry>();
        public int Count => _stack.Count;
        public UIFlowStackEntry this[int index] { get => _stack[index]; set => _stack[index] = value; }
        public IEnumerator<UIFlowStackEntry> GetEnumerator() => _stack.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        internal void Add(UIFlowStackEntry entry) => _stack.Add(entry);
        internal void Clear() => _stack.Clear();
        internal void RemoveAt(int index) => _stack.RemoveAt(index);
        internal void RemoveRange(int index, int count) => _stack.RemoveRange(index, count);
        internal List<UIFlowStackEntry> GetRange(int index, int count) => _stack.GetRange(index, count);

        internal IReadOnlyList<UIFlowStackEntrySnapshot> CreateStackSnapshot()
        {
            var snapshots = new List<UIFlowStackEntrySnapshot>(_stack.Count);
            for (int i = 0; i < _stack.Count; i++)
            {
                UIFlowStackEntry entry = _stack[i];
                snapshots.Add(new UIFlowStackEntrySnapshot(entry.EntryId, entry.Route.RouteId, entry.Route.DisplayName, entry.Screen.State, entry.Presentation != null));
            }

            return snapshots;
        }

        internal UIFlowStackEntry TopEntry()
        {
            return _stack.Count == 0 ? null : _stack[_stack.Count - 1];
        }

        internal int FindRouteIndex(UIFlowRouteId routeId)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].Route.RouteId == routeId)
                {
                    return i;
                }
            }

            return -1;
        }

        internal int FindEntryIndex(Guid entryId)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].EntryId == entryId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
