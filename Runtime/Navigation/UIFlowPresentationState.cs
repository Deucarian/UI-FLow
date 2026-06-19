using System;
using System.Threading.Tasks;

namespace Deucarian.UIFlow
{
    internal interface IUIFlowPresentationCompletion
    {
        bool IsCompleted { get; }
        Type ResultType { get; }
        Guid EntryId { get; }
        void SetEntryId(Guid entryId);
        void CompleteWithValue(object value);
        void CompleteDismissed(UIFlowDismissalReason reason, string message);
        void CompleteCancelled(string message);
        void CompleteHostDestroyed(string message);
        void CompleteRemoved(UIFlowDismissalReason reason, string message);
    }

    internal sealed class UIFlowPresentationCompletion<TResult> : IUIFlowPresentationCompletion
    {
        private readonly TaskCompletionSource<UIFlowPresentationResult<TResult>> _source = new TaskCompletionSource<UIFlowPresentationResult<TResult>>();

        public Task<UIFlowPresentationResult<TResult>> Task
        {
            get { return _source.Task; }
        }

        public bool IsCompleted
        {
            get { return _source.Task.IsCompleted; }
        }

        public Guid EntryId { get; private set; }

        public Type ResultType
        {
            get { return typeof(TResult); }
        }

        public void SetEntryId(Guid entryId)
        {
            EntryId = entryId;
        }

        public void CompleteWithValue(object value)
        {
            _source.TrySetResult(UIFlowPresentationResult<TResult>.Completed((TResult)value));
        }

        public void CompleteDismissed(UIFlowDismissalReason reason, string message)
        {
            _source.TrySetResult(UIFlowPresentationResult<TResult>.Dismissed(reason, message));
        }

        public void CompleteCancelled(string message)
        {
            _source.TrySetResult(UIFlowPresentationResult<TResult>.Cancelled(message));
        }

        public void CompleteHostDestroyed(string message)
        {
            _source.TrySetResult(UIFlowPresentationResult<TResult>.HostDestroyed(message));
        }

        public void CompleteRemoved(UIFlowDismissalReason reason, string message)
        {
            _source.TrySetResult(UIFlowPresentationResult<TResult>.Removed(reason, message));
        }
    }

    internal sealed class UIFlowPresentationState
    {
        public UIFlowPresentationState(IUIFlowPresentationCompletion completion)
        {
            Completion = completion;
        }

        public IUIFlowPresentationCompletion Completion { get; private set; }
        public bool CompletionRequested { get; private set; }
        public bool HasValue { get; private set; }
        public object Value { get; private set; }
        public UIFlowDismissalReason DismissalReason { get; private set; }

        public bool TryRequestValue(object value, Type suppliedType, out string error)
        {
            error = null;
            if (CompletionRequested)
            {
                error = "Presentation completion has already been requested.";
                return false;
            }

            Type expected = Completion.ResultType;
            if (value == null)
            {
                if (expected.IsValueType && Nullable.GetUnderlyingType(expected) == null)
                {
                    error = "Presentation expected non-null value type '" + expected.FullName + "'.";
                    return false;
                }
            }
            else if (!expected.IsInstanceOfType(value))
            {
                error = "Presentation expected value of type '" + expected.FullName + "' but received '" + value.GetType().FullName + "'.";
                return false;
            }

            CompletionRequested = true;
            HasValue = true;
            Value = value;
            DismissalReason = UIFlowDismissalReason.None;
            return true;
        }

        public bool TryRequestDismissal(UIFlowDismissalReason reason, out string error)
        {
            error = null;
            if (CompletionRequested)
            {
                error = "Presentation completion has already been requested.";
                return false;
            }

            CompletionRequested = true;
            HasValue = false;
            DismissalReason = reason == UIFlowDismissalReason.None ? UIFlowDismissalReason.Dismissed : reason;
            return true;
        }

        public void CompleteAfterExit()
        {
            if (Completion == null || Completion.IsCompleted)
            {
                return;
            }

            if (HasValue)
            {
                Completion.CompleteWithValue(Value);
            }
            else
            {
                Completion.CompleteDismissed(DismissalReason == UIFlowDismissalReason.None ? UIFlowDismissalReason.Dismissed : DismissalReason, null);
            }
        }

        public void CompleteRemoved(UIFlowDismissalReason reason, string message)
        {
            if (Completion == null || Completion.IsCompleted)
            {
                return;
            }

            Completion.CompleteRemoved(reason, message);
        }
    }
}
