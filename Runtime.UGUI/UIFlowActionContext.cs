using Deucarian.UIFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.UIFlow.UGUI
{
    /// <summary>
    /// Runtime context supplied by a UI Flow event binder to an action asset.
    /// </summary>
    public sealed class UIFlowActionContext
    {
        public UIFlowActionContext(MonoBehaviour source, Button button, UIFlowHost explicitHost)
        {
            Source = source;
            Button = button;
            ExplicitHost = explicitHost;
        }

        public MonoBehaviour Source { get; private set; }
        public Button Button { get; private set; }
        public UIFlowHost ExplicitHost { get; private set; }

        public bool TryGetHost(out UIFlowHost host)
        {
            host = ExplicitHost;
            if (host != null)
            {
                return true;
            }

            host = Source == null ? null : Source.GetComponentInParent<UIFlowHost>();
            return host != null;
        }

        public bool TryGetScreen(out UIFlowScreen screen)
        {
            screen = Source == null ? null : Source.GetComponentInParent<UIFlowScreen>();
            return screen != null && screen.HasContext;
        }
    }
}
