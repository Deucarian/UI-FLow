using System.Collections.Generic;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Weak diagnostic registry used by editor tooling.
    /// </summary>
    public static class UIFlowDiagnosticRegistry
    {
        private static readonly List<UIFlowHost> Hosts = new List<UIFlowHost>();

        public static IReadOnlyList<UIFlowHost> ActiveHosts
        {
            get
            {
                PruneDestroyedHosts();
                return Hosts;
            }
        }

        internal static void Register(UIFlowHost host)
        {
            if (host == null || Hosts.Contains(host))
            {
                return;
            }

            Hosts.Add(host);
        }

        internal static void Unregister(UIFlowHost host)
        {
            if (host == null)
            {
                return;
            }

            Hosts.Remove(host);
        }

        private static void PruneDestroyedHosts()
        {
            for (int i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i] == null)
                {
                    Hosts.RemoveAt(i);
                }
            }
        }
    }
}
