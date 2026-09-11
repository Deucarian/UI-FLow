using System.Collections.Generic;

namespace Deucarian.UIFlow
{
    internal static class UIFlowHostValidation
    {
        internal static void ValidateCatalog(UIFlowRouteCatalog catalog, UIFlowHost host)
        {
            if (catalog == null)
            {
                UIFlowLog.Validation.Warning("UI Flow host '" + host.name + "' has no route catalog. Direct route asset navigation will still work.", host);
                return;
            }
            IReadOnlyList<string> errors = catalog.ValidateCatalog();
            for (int i = 0; i < errors.Count; i++) UIFlowLog.Validation.Warning(errors[i], catalog);
        }
    }
}
