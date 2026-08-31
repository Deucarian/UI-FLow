using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.UIFlow;
using UnityEditor;

namespace Deucarian.UIFlow.Editor
{
    [InitializeOnLoad]
    internal static class UIFlowControlCenter
    {
        private const string PackageId = "com.deucarian.ui-flow";
        private const string ToolId = "deucarian.ui-flow.debugger";

        static UIFlowControlCenter()
        {
            DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                ToolId,
                "UI Flow Debugger",
                "Inspect active UI Flow hosts and navigation state.",
                DeucarianControlCenterArea.Experience,
                UIFlowDebuggerWindow.Open,
                PackageId,
                searchTerms: new[] { "ui", "navigation", "routes", "debugger" },
                order: 300));
            DeucarianControlCenterRegistry.RegisterCardProvider(new Provider());
        }

        private sealed class Provider : IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterCard> Capture(
                DeucarianControlCenterContext context)
            {
                int hostCount = UIFlowDiagnosticRegistry.ActiveHosts.Count;
                yield return new DeucarianControlCenterCard(
                    PackageId + ".experience",
                    DeucarianControlCenterArea.Experience,
                    "UI Flow",
                    "Validate project routes and inspect active navigation hosts.",
                    PackageId,
                    hostCount > 0
                        ? DeucarianControlCenterStatus.Success
                        : DeucarianControlCenterStatus.Info,
                    hostCount > 0
                        ? hostCount + " active host(s)"
                        : "No active hosts",
                    order: 300,
                    details: new[]
                    {
                        "Only the active host count is summarized; route payloads stay in the debugger."
                    },
                    actions: new[]
                    {
                        new DeucarianControlCenterAction(
                            "open-debugger",
                            "Open Debugger",
                            UIFlowDebuggerWindow.Open),
                        new DeucarianControlCenterAction(
                            "validate-project",
                            "Validate Project",
                            UIFlowProjectValidator.ValidateProject)
                    },
                    searchTerms: new[] { "ui", "flow", "routes", "validation" });
            }
        }
    }
}
