using System;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Exception thrown when a navigation operation fails unexpectedly after rollback has been attempted.
    /// </summary>
    public sealed class UIFlowNavigationException : Exception
    {
        public UIFlowNavigationException(string channel, string operation, string route, string remediation, Exception innerException = null)
            : base(FormatMessage(channel, operation, route, remediation), innerException)
        {
            Channel = channel;
            Operation = operation;
            Route = route;
            Remediation = remediation;
        }

        public string Channel { get; private set; }
        public string Operation { get; private set; }
        public string Route { get; private set; }
        public string Remediation { get; private set; }

        private static string FormatMessage(string channel, string operation, string route, string remediation)
        {
            return string.Format(
                "UI Flow navigation failed. Channel='{0}', Operation='{1}', Route='{2}'. {3}",
                string.IsNullOrEmpty(channel) ? "<unknown>" : channel,
                string.IsNullOrEmpty(operation) ? "<unknown>" : operation,
                string.IsNullOrEmpty(route) ? "<none>" : route,
                string.IsNullOrEmpty(remediation) ? "Inspect the inner exception and UI Flow configuration." : remediation);
        }
    }

    /// <summary>
    /// Exception thrown when navigation arguments are missing or have the wrong type.
    /// </summary>
    public sealed class UIFlowArgumentException : Exception
    {
        public UIFlowArgumentException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a caller uses an API from an invalid UI Flow state.
    /// </summary>
    public sealed class UIFlowUsageException : Exception
    {
        public UIFlowUsageException(string message)
            : base(message)
        {
        }
    }
}
