using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    /// <summary>
    /// Host component that owns channels, navigators, route lookup, providers, guards, queueing, Back routing, and shutdown.
    /// </summary>
    public sealed class UIFlowHost : MonoBehaviour, IUIFlowRouter
    {
        [SerializeField] private UIFlowRouteCatalog _routeCatalog;
        [SerializeField] private UIFlowChannelConfig[] _channels = new UIFlowChannelConfig[0];
        [SerializeField] private UIFlowSceneScreenBinding[] _sceneBindings = new UIFlowSceneScreenBinding[0];
        [SerializeField] private UIFlowGuard[] _globalGuards = new UIFlowGuard[0];
        [SerializeField] private MonoBehaviour[] _customProviderComponents = new MonoBehaviour[0];
        [SerializeField] private bool _initializeOnAwake;
        [SerializeField] private int _redirectDepthLimit = 8;

        private readonly Dictionary<UIFlowChannelId, UIFlowNavigator> _navigators = new Dictionary<UIFlowChannelId, UIFlowNavigator>();
        private readonly Dictionary<string, IUIFlowScreenProvider> _providers = new Dictionary<string, IUIFlowScreenProvider>(StringComparer.Ordinal);
        private readonly Queue<QueuedNavigationOperation> _queue = new Queue<QueuedNavigationOperation>();
        private readonly List<QueuedNavigationOperation> _queuedSnapshot = new List<QueuedNavigationOperation>();

        private UIFlowPrefabScreenProvider _prefabProvider;
        private UIFlowExternalSceneScreenProvider _externalProvider;
        private UIFlowInitializationState _initializationState;
        private Task _initializationTask;
        private CancellationTokenSource _shutdownCts;
        private int _mainThreadId;
        private bool _processingQueue;
        private long _nextOperationId;
        private UIFlowOperationSnapshot _currentOperation;
        private UIFlowNavigationResult _lastResult;
        private Exception _lastFailure;

        public event EventHandler<UIFlowNavigationEventArgs> NavigationRequested;
        public event EventHandler<UIFlowNavigationEventArgs> NavigationStarted;
        public event EventHandler<UIFlowNavigationEventArgs> NavigationRedirected;
        public event EventHandler<UIFlowNavigationEventArgs> NavigationRejected;
        public event EventHandler<UIFlowNavigationEventArgs> NavigationCancelled;
        public event EventHandler<UIFlowNavigationEventArgs> NavigationFailed;
        public event EventHandler<UIFlowNavigationEventArgs> NavigationCompleted;
        public event EventHandler<UIFlowStackChangedEventArgs> StackChanged;
        public event EventHandler BackUnhandled;
        public event EventHandler HostInitialized;
        public event EventHandler HostShutdown;

        public UIFlowInitializationState InitializationState
        {
            get { return _initializationState; }
        }

        public UIFlowOperationSnapshot CurrentOperation
        {
            get { return _currentOperation; }
        }

        public int QueuedOperationCount
        {
            get { return _queue.Count; }
        }

        public int RedirectDepthLimit
        {
            get { return Mathf.Max(1, _redirectDepthLimit); }
        }

        internal IReadOnlyList<UIFlowGuard> GlobalGuards
        {
            get { return _globalGuards; }
        }

        private void Awake()
        {
            CaptureMainThread();
            _shutdownCts = new CancellationTokenSource();
            UIFlowDiagnosticRegistry.Register(this);

            if (_initializeOnAwake)
            {
                RunFireAndForget(InitializeAsync, "InitializeAsync");
            }
        }

        private void OnEnable()
        {
            CaptureMainThread();
            UIFlowDiagnosticRegistry.Register(this);
        }

        private void OnDestroy()
        {
            ShutdownNow();
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureMainThread();

            if (_initializationState == UIFlowInitializationState.Initialized)
            {
                return Task.CompletedTask;
            }

            if (_initializationState == UIFlowInitializationState.Shutdown)
            {
                throw new UIFlowUsageException("Cannot initialize a UIFlowHost after it has been shut down.");
            }

            if (_initializationTask != null && !_initializationTask.IsCompleted)
            {
                return _initializationTask;
            }

            _initializationTask = InitializeInternalAsync(cancellationToken);
            return _initializationTask;
        }

        public Task<UIFlowNavigationResult> PushAsync(
            UIFlowRoute route,
            object arguments = null,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            return EnqueueNavigation(
                UIFlowOperationType.Push,
                route == null ? default(UIFlowChannelId) : route.TargetChannel,
                route,
                options,
                cancellationToken,
                (operationId, token) => GetNavigator(route).PushAsync(operationId, UIFlowOperationType.Push, route, arguments, options, null, token));
        }

        public Task<UIFlowNavigationResult> PopAsync(
            UIFlowChannelId channel,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            return EnqueueNavigation(
                UIFlowOperationType.Pop,
                channel,
                null,
                options,
                cancellationToken,
                (operationId, token) => GetNavigator(channel).PopAsync(operationId, UIFlowOperationType.Pop, options, token));
        }

        public Task<UIFlowNavigationResult> ReplaceAsync(
            UIFlowRoute route,
            object arguments = null,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            return EnqueueNavigation(
                UIFlowOperationType.Replace,
                route == null ? default(UIFlowChannelId) : route.TargetChannel,
                route,
                options,
                cancellationToken,
                (operationId, token) => GetNavigator(route).ReplaceAsync(operationId, route, arguments, options, token));
        }

        public Task<UIFlowNavigationResult> ResetAsync(
            UIFlowChannelId channel,
            UIFlowRoute rootRoute = null,
            object arguments = null,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            UIFlowChannelId targetChannel = rootRoute == null ? channel : rootRoute.TargetChannel;
            return EnqueueNavigation(
                UIFlowOperationType.Reset,
                targetChannel,
                rootRoute,
                options,
                cancellationToken,
                (operationId, token) => GetNavigator(targetChannel).ResetAsync(operationId, rootRoute, arguments, options, token));
        }

        public Task<UIFlowNavigationResult> PopToAsync(
            UIFlowChannelId channel,
            UIFlowRouteId routeId,
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            return EnqueueNavigation(
                UIFlowOperationType.PopTo,
                channel,
                null,
                options,
                cancellationToken,
                (operationId, token) => GetNavigator(channel).PopToAsync(operationId, routeId, options, token));
        }

        public Task<UIFlowNavigationResult> BackAsync(
            UIFlowNavigationOptions options = default(UIFlowNavigationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            options.Reason = UIFlowNavigationReason.Back;
            return EnqueueNavigation(
                UIFlowOperationType.Back,
                default(UIFlowChannelId),
                null,
                options,
                cancellationToken,
                (operationId, token) => ExecuteBackAsync(operationId, options, token));
        }

        public async Task<UIFlowPresentationResult<TResult>> PresentAsync<TResult>(
            UIFlowRoute route,
            object arguments = null,
            UIFlowPresentationOptions options = default(UIFlowPresentationOptions),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options.Normalize();
            var completion = new UIFlowPresentationCompletion<TResult>();

            UIFlowNavigationResult navigation = await EnqueueNavigation(
                UIFlowOperationType.Present,
                route == null ? default(UIFlowChannelId) : route.TargetChannel,
                route,
                options.NavigationOptions,
                cancellationToken,
                (operationId, token) => GetNavigator(route).PushAsync(operationId, UIFlowOperationType.Present, route, arguments, options.NavigationOptions, completion, token));

            if (navigation.Status == UIFlowNavigationStatus.Cancelled)
            {
                return UIFlowPresentationResult<TResult>.Cancelled(navigation.Message);
            }

            if (!navigation.Succeeded)
            {
                return UIFlowPresentationResult<TResult>.Removed(UIFlowDismissalReason.NavigationRejected, navigation.Message);
            }

            return await AwaitPresentationAsync(completion, options, cancellationToken);
        }

        public bool TryGetRoute(UIFlowRouteId routeId, out UIFlowRoute route)
        {
            route = null;
            if (_routeCatalog == null)
            {
                return false;
            }

            return _routeCatalog.TryGetRoute(routeId, out route);
        }

        public IReadOnlyList<UIFlowChannelSnapshot> GetChannelSnapshots()
        {
            var snapshots = new List<UIFlowChannelSnapshot>(_navigators.Count);
            foreach (KeyValuePair<UIFlowChannelId, UIFlowNavigator> pair in _navigators)
            {
                snapshots.Add(pair.Value.CreateSnapshot());
            }

            return snapshots;
        }

        public UIFlowHostSnapshot CreateDiagnosticSnapshot()
        {
            return new UIFlowHostSnapshot(name, _initializationState, GetChannelSnapshots(), _currentOperation, _queue.Count, _lastResult, _lastFailure);
        }

        internal IUIFlowScreenProvider GetProvider(UIFlowRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (route.SourceMode == UIFlowScreenSourceMode.Prefab)
            {
                return _prefabProvider;
            }

            if (route.SourceMode == UIFlowScreenSourceMode.ExternalSceneBinding)
            {
                return _externalProvider;
            }

            if (string.IsNullOrWhiteSpace(route.CustomProviderId))
            {
                throw new UIFlowNavigationException(route.TargetChannel.ToString(), "ResolveProvider", route.RouteId.ToString(), "Custom provider routes require a non-empty provider ID.");
            }

            IUIFlowScreenProvider provider;
            if (!_providers.TryGetValue(route.CustomProviderId, out provider))
            {
                throw new UIFlowNavigationException(route.TargetChannel.ToString(), "ResolveProvider", route.RouteId.ToString(), "No custom screen provider is registered for provider ID '" + route.CustomProviderId + "'.");
            }

            return provider;
        }

        internal Task<UIFlowNavigationResult> CloseEntryAsync(
            Guid entryId,
            bool hasValue,
            object value,
            Type valueType,
            UIFlowDismissalReason dismissalReason,
            CancellationToken cancellationToken)
        {
            EnsureMainThread();
            UIFlowNavigator navigator = FindNavigatorContainingEntry(entryId);
            if (navigator == null)
            {
                return Task.FromResult(UIFlowNavigationResult.Rejected(0, UIFlowOperationType.Dismiss, default(UIFlowChannelId), default(UIFlowRouteId), "The entry has already been removed."));
            }

            UIFlowNavigationOptions options = UIFlowNavigationOptions.Default;
            options.Reason = dismissalReason == UIFlowDismissalReason.Back ? UIFlowNavigationReason.Back : UIFlowNavigationReason.User;

            return EnqueueNavigation(
                UIFlowOperationType.Dismiss,
                navigator.ChannelId,
                null,
                options,
                cancellationToken,
                (operationId, token) => navigator.CloseEntryAsync(operationId, entryId, hasValue, value, valueType, dismissalReason, options, token));
        }

        internal void NotifyStackChanged(UIFlowNavigator navigator)
        {
            RaiseStackChanged(new UIFlowStackChangedEventArgs(navigator.CreateSnapshot()));
        }

        internal void NotifyNavigationRedirected(
            long operationId,
            UIFlowOperationType operationType,
            UIFlowNavigationReason reason,
            UIFlowChannelId channelId,
            UIFlowRouteId source,
            UIFlowRouteId target,
            string message,
            string diagnosticLabel)
        {
            RaiseNavigationEvent(
                NavigationRedirected,
                new UIFlowNavigationEventArgs(operationId, operationType, reason, channelId, source, target, null, message, null, TimeSpan.Zero, TimeSpan.Zero, diagnosticLabel));
        }

        private async Task InitializeInternalAsync(CancellationToken cancellationToken)
        {
            _initializationState = UIFlowInitializationState.Initializing;

            try
            {
                _shutdownCts = _shutdownCts ?? new CancellationTokenSource();
                BuildProviders();
                BuildNavigators();
                ValidateConfiguration();

                for (int i = 0; i < _channels.Length; i++)
                {
                    UIFlowChannelConfig channel = _channels[i];
                    if (channel != null && channel.InitialRoute != null)
                    {
                        UIFlowNavigator navigator = GetNavigator(channel.ChannelId);
                        UIFlowNavigationOptions options = UIFlowNavigationOptions.Instant(UIFlowNavigationReason.Initialization);
                        UIFlowNavigationResult result = await navigator.ResetAsync(++_nextOperationId, channel.InitialRoute, null, options, cancellationToken);
                        if (!result.Succeeded)
                        {
                            throw new UIFlowNavigationException(channel.ChannelId.ToString(), "Initialize", channel.InitialRoute.RouteId.ToString(), "Initial route navigation was rejected: " + result.Message);
                        }
                    }
                }

                _initializationState = UIFlowInitializationState.Initialized;
                RaiseSimpleEvent(HostInitialized);
            }
            catch
            {
                _initializationState = UIFlowInitializationState.Failed;
                throw;
            }
        }

        private void BuildProviders()
        {
            _providers.Clear();
            _prefabProvider = new UIFlowPrefabScreenProvider();
            _externalProvider = new UIFlowExternalSceneScreenProvider(_sceneBindings);
            _providers.Add(UIFlowPrefabScreenProvider.Id, _prefabProvider);
            _providers.Add(UIFlowExternalSceneScreenProvider.Id, _externalProvider);

            for (int i = 0; i < _customProviderComponents.Length; i++)
            {
                MonoBehaviour component = _customProviderComponents[i];
                if (component == null)
                {
                    continue;
                }

                IUIFlowScreenProvider provider = component as IUIFlowScreenProvider;
                if (provider == null)
                {
                    throw new UIFlowUsageException("Custom provider component '" + component.name + "' does not implement IUIFlowScreenProvider.");
                }

                if (string.IsNullOrWhiteSpace(provider.ProviderId))
                {
                    throw new UIFlowUsageException("Custom provider component '" + component.name + "' has an empty ProviderId.");
                }

                if (_providers.ContainsKey(provider.ProviderId))
                {
                    throw new UIFlowUsageException("Duplicate UI Flow provider ID '" + provider.ProviderId + "'.");
                }

                _providers.Add(provider.ProviderId, provider);
            }
        }

        private void BuildNavigators()
        {
            _navigators.Clear();
            for (int i = 0; i < _channels.Length; i++)
            {
                UIFlowChannelConfig channel = _channels[i];
                if (channel == null)
                {
                    continue;
                }

                if (channel.ChannelId.IsEmpty)
                {
                    throw new UIFlowUsageException("UI Flow channel at index " + i + " has an empty channel ID.");
                }

                if (_navigators.ContainsKey(channel.ChannelId))
                {
                    throw new UIFlowUsageException("Duplicate UI Flow channel ID '" + channel.ChannelId + "'.");
                }

                _navigators.Add(channel.ChannelId, new UIFlowNavigator(this, channel));
            }
        }

        private void ValidateConfiguration()
        {
            if (_routeCatalog == null)
            {
                Debug.LogWarning("UI Flow host '" + name + "' has no route catalog. Direct route asset navigation will still work.", this);
            }
            else
            {
                IReadOnlyList<string> catalogErrors = _routeCatalog.ValidateCatalog();
                for (int i = 0; i < catalogErrors.Count; i++)
                {
                    Debug.LogWarning(catalogErrors[i], _routeCatalog);
                }
            }

            if (_navigators.Count == 0)
            {
                throw new UIFlowUsageException("UI Flow host '" + name + "' has no channels.");
            }

            for (int i = 0; i < _channels.Length; i++)
            {
                UIFlowChannelConfig channel = _channels[i];
                if (channel == null)
                {
                    throw new UIFlowUsageException("UI Flow host '" + name + "' contains a null channel at index " + i + ".");
                }

                if (channel.Root == null)
                {
                    throw new UIFlowUsageException("UI Flow channel '" + channel.ChannelId + "' has no root transform.");
                }

                if (channel.InitialRoute != null && channel.InitialRoute.TargetChannel != channel.ChannelId)
                {
                    throw new UIFlowUsageException("Initial route '" + channel.InitialRoute.RouteId + "' targets channel '" + channel.InitialRoute.TargetChannel + "' but is assigned to channel '" + channel.ChannelId + "'.");
                }
            }
        }

        private Task<UIFlowNavigationResult> EnqueueNavigation(
            UIFlowOperationType operationType,
            UIFlowChannelId channelId,
            UIFlowRoute route,
            UIFlowNavigationOptions options,
            CancellationToken cancellationToken,
            Func<long, CancellationToken, Task<UIFlowNavigationResult>> execute)
        {
            EnsureMainThread();
            options = options.Normalize();

            long operationId = ++_nextOperationId;
            UIFlowRouteId targetRouteId = route == null ? default(UIFlowRouteId) : route.RouteId;

            if (_initializationState == UIFlowInitializationState.Shutdown)
            {
                return Task.FromResult(UIFlowNavigationResult.Cancelled(operationId, operationType, channelId, targetRouteId, "UI Flow host has shut down."));
            }

            if (options.ConflictPolicy == UIFlowRequestConflictPolicy.RejectIfBusy && (_processingQueue || _queue.Count > 0))
            {
                return Task.FromResult(UIFlowNavigationResult.Rejected(operationId, operationType, channelId, targetRouteId, "UI Flow host is busy."));
            }

            if (options.ConflictPolicy == UIFlowRequestConflictPolicy.CoalesceEquivalent)
            {
                foreach (QueuedNavigationOperation queued in _queue)
                {
                    if (queued.IsEquivalent(operationType, channelId, targetRouteId, options))
                    {
                        return queued.Task;
                    }
                }
            }

            CancellationTokenSource linkedCts = null;
            CancellationToken operationCancellationToken = cancellationToken;
            if (_shutdownCts != null)
            {
                linkedCts = cancellationToken.CanBeCanceled
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdownCts.Token)
                    : CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token);
                operationCancellationToken = linkedCts.Token;
            }

            var operation = new QueuedNavigationOperation(operationId, operationType, channelId, targetRouteId, options, operationCancellationToken, linkedCts, execute);
            operation.RegisterQueuedCancellation();
            _queue.Enqueue(operation);
            RaiseNavigationEvent(NavigationRequested, CreateEventArgs(operation, null, null, TimeSpan.Zero));
            RunFireAndForget(ProcessQueueAsync, "ProcessQueueAsync");
            return operation.Task;
        }

        private async Task ProcessQueueAsync(CancellationToken ignored)
        {
            if (_processingQueue)
            {
                return;
            }

            _processingQueue = true;

            try
            {
                while (_queue.Count > 0)
                {
                    QueuedNavigationOperation operation = _queue.Dequeue();
                    if (operation.IsCompleted)
                    {
                        continue;
                    }

                    operation.MarkStarted();
                    TimeSpan queueDuration = DateTime.UtcNow - operation.RequestedAtUtc;
                    _currentOperation = new UIFlowOperationSnapshot(operation.OperationId, operation.OperationType, operation.Options.Reason, operation.ChannelId, operation.TargetRouteId, operation.Options.DiagnosticLabel, queueDuration);
                    RaiseNavigationEvent(NavigationStarted, CreateEventArgs(operation, null, null, TimeSpan.Zero));

                    DateTime startedAt = DateTime.UtcNow;
                    try
                    {
                        if (operation.CancellationToken.IsCancellationRequested)
                        {
                            UIFlowNavigationResult cancelled = UIFlowNavigationResult.Cancelled(operation.OperationId, operation.OperationType, operation.ChannelId, operation.TargetRouteId, "Request was cancelled before execution.");
                            CompleteOperation(operation, cancelled, startedAt);
                            continue;
                        }

                        if (_initializationState != UIFlowInitializationState.Initialized)
                        {
                            await InitializeAsync(operation.CancellationToken);
                        }

                        UIFlowNavigationResult result = await operation.Execute(operation.OperationId, operation.CancellationToken);
                        CompleteOperation(operation, result, startedAt);
                    }
                    catch (OperationCanceledException)
                    {
                        UIFlowNavigationResult cancelled = UIFlowNavigationResult.Cancelled(operation.OperationId, operation.OperationType, operation.ChannelId, operation.TargetRouteId, "Request was cancelled.");
                        CompleteOperation(operation, cancelled, startedAt);
                    }
                    catch (Exception ex)
                    {
                        _lastFailure = ex;
                        TimeSpan executionDuration = DateTime.UtcNow - startedAt;
                        RaiseNavigationEvent(NavigationFailed, CreateEventArgs(operation, null, ex, executionDuration));
                        operation.TrySetException(ex);
                    }
                    finally
                    {
                        _currentOperation = null;
                    }
                }
            }
            finally
            {
                _processingQueue = false;
                if (_queue.Count > 0)
                {
                    RunFireAndForget(ProcessQueueAsync, "ProcessQueueAsync");
                }
            }
        }

        private void CompleteOperation(QueuedNavigationOperation operation, UIFlowNavigationResult result, DateTime startedAtUtc)
        {
            _lastResult = result;
            TimeSpan executionDuration = DateTime.UtcNow - startedAtUtc;

            if (result.Status == UIFlowNavigationStatus.Cancelled)
            {
                RaiseNavigationEvent(NavigationCancelled, CreateEventArgs(operation, result, null, executionDuration));
            }
            else if (result.Status == UIFlowNavigationStatus.Rejected || result.Status == UIFlowNavigationStatus.GuardDenied)
            {
                RaiseNavigationEvent(NavigationRejected, CreateEventArgs(operation, result, null, executionDuration));
            }

            RaiseNavigationEvent(NavigationCompleted, CreateEventArgs(operation, result, null, executionDuration));
            operation.TrySetResult(result);
        }

        private async Task<UIFlowNavigationResult> ExecuteBackAsync(long operationId, UIFlowNavigationOptions options, CancellationToken cancellationToken)
        {
            List<UIFlowNavigator> navigators = new List<UIFlowNavigator>(_navigators.Values);
            navigators.Sort((left, right) => right.Config.BackPriority.CompareTo(left.Config.BackPriority));

            for (int i = 0; i < navigators.Count; i++)
            {
                UIFlowNavigator navigator = navigators[i];
                if (!navigator.CanHandleBack)
                {
                    continue;
                }

                options.Reason = UIFlowNavigationReason.Back;
                return await navigator.PopAsync(operationId, UIFlowOperationType.Back, options, cancellationToken);
            }

            RaiseSimpleEvent(BackUnhandled);
            return UIFlowNavigationResult.NoOp(operationId, UIFlowOperationType.Back, default(UIFlowChannelId), default(UIFlowRouteId), "No channel handled Back.");
        }

        private async Task<UIFlowPresentationResult<TResult>> AwaitPresentationAsync<TResult>(
            UIFlowPresentationCompletion<TResult> completion,
            UIFlowPresentationOptions options,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await completion.Task;
            }

            Task cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            Task completed = await Task.WhenAny(completion.Task, cancellationTask);
            if (completed == completion.Task)
            {
                return await completion.Task;
            }

            if (options.CloseOnCallerCancellation && completion.EntryId != Guid.Empty)
            {
                await CloseEntryAsync(completion.EntryId, false, null, null, UIFlowDismissalReason.CallerCancelled, CancellationToken.None);
                return await completion.Task;
            }

            completion.CompleteCancelled("Caller cancellation token was cancelled.");
            return await completion.Task;
        }

        private UIFlowNavigator GetNavigator(UIFlowRoute route)
        {
            if (route == null)
            {
                throw new UIFlowNavigationException("<unknown>", "ResolveChannel", "<none>", "Navigation requires a non-null route.");
            }

            return GetNavigator(route.TargetChannel);
        }

        private UIFlowNavigator GetNavigator(UIFlowChannelId channelId)
        {
            UIFlowNavigator navigator;
            if (!_navigators.TryGetValue(channelId, out navigator))
            {
                throw new UIFlowNavigationException(channelId.ToString(), "ResolveChannel", "<none>", "Add a matching channel configuration to the UIFlowHost.");
            }

            return navigator;
        }

        private UIFlowNavigator FindNavigatorContainingEntry(Guid entryId)
        {
            foreach (KeyValuePair<UIFlowChannelId, UIFlowNavigator> pair in _navigators)
            {
                if (pair.Value.ContainsEntry(entryId))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private void ShutdownNow()
        {
            if (_initializationState == UIFlowInitializationState.Shutdown)
            {
                return;
            }

            _initializationState = UIFlowInitializationState.Shutdown;

            if (_shutdownCts != null)
            {
                _shutdownCts.Cancel();
            }

            while (_queue.Count > 0)
            {
                QueuedNavigationOperation queued = _queue.Dequeue();
                queued.TrySetResult(UIFlowNavigationResult.Cancelled(queued.OperationId, queued.OperationType, queued.ChannelId, queued.TargetRouteId, "UI Flow host was destroyed before the request ran."));
            }

            try
            {
                foreach (KeyValuePair<UIFlowChannelId, UIFlowNavigator> pair in _navigators)
                {
                    pair.Value.ShutdownAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }

            if (_shutdownCts != null)
            {
                _shutdownCts.Dispose();
                _shutdownCts = null;
            }

            UIFlowDiagnosticRegistry.Unregister(this);
            RaiseSimpleEvent(HostShutdown);
        }

        private void CaptureMainThread()
        {
            if (_mainThreadId == 0)
            {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }

        private void EnsureMainThread()
        {
            CaptureMainThread();
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                throw new UIFlowNavigationException(name, "MainThread", "<none>", "UI Flow host APIs must be called from Unity's main thread. Marshal work to the Unity synchronization context before calling the router.");
            }
        }

        private void RunFireAndForget(Func<CancellationToken, Task> action, string operation)
        {
            try
            {
                CancellationToken token = _shutdownCts == null ? CancellationToken.None : _shutdownCts.Token;
                Task task = action(token);
                if (task.IsCompleted)
                {
                    if (task.IsFaulted && task.Exception != null)
                    {
                        Debug.LogException(task.Exception.GetBaseException(), this);
                    }

                    return;
                }

                task.ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        Debug.LogException(new UIFlowNavigationException(name, operation, "<none>", "A UI Flow background adapter task faulted.", t.Exception.GetBaseException()), this);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                Debug.LogException(new UIFlowNavigationException(name, operation, "<none>", "A UI Flow fire-and-forget adapter failed to start.", ex), this);
            }
        }

        private UIFlowNavigationEventArgs CreateEventArgs(QueuedNavigationOperation operation, UIFlowNavigationResult result, Exception exception, TimeSpan executionDuration)
        {
            return new UIFlowNavigationEventArgs(
                operation.OperationId,
                operation.OperationType,
                operation.Options.Reason,
                operation.ChannelId,
                default(UIFlowRouteId),
                operation.TargetRouteId,
                result,
                result == null ? null : result.Message,
                exception,
                DateTime.UtcNow - operation.RequestedAtUtc,
                executionDuration,
                operation.Options.DiagnosticLabel);
        }

        private void RaiseNavigationEvent(EventHandler<UIFlowNavigationEventArgs> handler, UIFlowNavigationEventArgs args)
        {
            if (handler == null)
            {
                return;
            }

            foreach (EventHandler<UIFlowNavigationEventArgs> subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new UIFlowNavigationException(args.ChannelId.ToString(), args.OperationType.ToString(), args.TargetRouteId.ToString(), "A UI Flow observer threw. Navigation state was preserved.", ex), this);
                }
            }
        }

        private void RaiseStackChanged(UIFlowStackChangedEventArgs args)
        {
            EventHandler<UIFlowStackChangedEventArgs> handler = StackChanged;
            if (handler == null)
            {
                return;
            }

            foreach (EventHandler<UIFlowStackChangedEventArgs> subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new UIFlowNavigationException(args.Snapshot.ChannelId.ToString(), "StackChanged", "<none>", "A UI Flow stack observer threw. Navigation state was preserved.", ex), this);
                }
            }
        }

        private void RaiseSimpleEvent(EventHandler handler)
        {
            if (handler == null)
            {
                return;
            }

            foreach (EventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }
        }

        private sealed class QueuedNavigationOperation
        {
            private readonly TaskCompletionSource<UIFlowNavigationResult> _source = new TaskCompletionSource<UIFlowNavigationResult>();
            private readonly CancellationToken _operationCancellationToken;
            private readonly CancellationTokenSource _linkedCts;
            private CancellationTokenRegistration _registration;
            private bool _started;

            public QueuedNavigationOperation(
                long operationId,
                UIFlowOperationType operationType,
                UIFlowChannelId channelId,
                UIFlowRouteId targetRouteId,
                UIFlowNavigationOptions options,
                CancellationToken operationCancellationToken,
                CancellationTokenSource linkedCts,
                Func<long, CancellationToken, Task<UIFlowNavigationResult>> execute)
            {
                OperationId = operationId;
                OperationType = operationType;
                ChannelId = channelId;
                TargetRouteId = targetRouteId;
                Options = options;
                _operationCancellationToken = operationCancellationToken;
                _linkedCts = linkedCts;
                Execute = execute;
                RequestedAtUtc = DateTime.UtcNow;
            }

            public long OperationId { get; private set; }
            public UIFlowOperationType OperationType { get; private set; }
            public UIFlowChannelId ChannelId { get; private set; }
            public UIFlowRouteId TargetRouteId { get; private set; }
            public UIFlowNavigationOptions Options { get; private set; }
            public Func<long, CancellationToken, Task<UIFlowNavigationResult>> Execute { get; private set; }
            public DateTime RequestedAtUtc { get; private set; }
            public Task<UIFlowNavigationResult> Task
            {
                get { return _source.Task; }
            }

            public bool IsCompleted
            {
                get { return _source.Task.IsCompleted; }
            }

            public CancellationToken CancellationToken
            {
                get { return _operationCancellationToken; }
            }

            public void RegisterQueuedCancellation()
            {
                if (!_operationCancellationToken.CanBeCanceled)
                {
                    return;
                }

                _registration = _operationCancellationToken.Register(() =>
                {
                    if (!_started)
                    {
                        TrySetResult(UIFlowNavigationResult.Cancelled(OperationId, OperationType, ChannelId, TargetRouteId, "Request was cancelled while queued."));
                    }
                });
            }

            public void MarkStarted()
            {
                _started = true;
                _registration.Dispose();
            }

            public bool IsEquivalent(UIFlowOperationType operationType, UIFlowChannelId channelId, UIFlowRouteId targetRouteId, UIFlowNavigationOptions options)
            {
                return OperationType == operationType
                    && ChannelId == channelId
                    && TargetRouteId == targetRouteId
                    && Options.Reason == options.Reason
                    && string.Equals(Options.DiagnosticLabel, options.DiagnosticLabel, StringComparison.Ordinal);
            }

            public void TrySetResult(UIFlowNavigationResult result)
            {
                _source.TrySetResult(result);
                Dispose();
            }

            public void TrySetException(Exception exception)
            {
                _source.TrySetException(exception);
                Dispose();
            }

            private void Dispose()
            {
                _registration.Dispose();
                if (_linkedCts != null)
                {
                    _linkedCts.Dispose();
                }
            }
        }
    }
}
