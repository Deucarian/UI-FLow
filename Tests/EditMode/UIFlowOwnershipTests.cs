using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.UIFlow.Tests.EditMode
{
    public sealed class UIFlowOwnershipTests
    {
        private GameObject root;
        private UIFlowHost host;
        private TestProvider provider;
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private CapturingSink sink;

        [SetUp]
        public void SetUp()
        {
            sink = new CapturingSink();
            DeucarianLog.ClearSinks();
            DeucarianLog.RegisterSink(sink);
            root = new GameObject("Ownership test");
            host = root.AddComponent<UIFlowHost>();
            provider = root.AddComponent<TestProvider>();
            var channel = new UIFlowChannelConfig();
            Set(channel, "_channelId", UIFlowChannelId.Main);
            Set(channel, "_root", root.transform);
            Set(channel, "_protectRootFromBack", false);
            Set(host, "_channels", new[] { channel });
            Set(host, "_customProviderComponents", new MonoBehaviour[] { provider });
            var catalog = ScriptableObject.CreateInstance<UIFlowRouteCatalog>();
            assets.Add(catalog);
            Set(host, "_routeCatalog", catalog);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
            DeucarianLog.ResetSinksToDefault();
        }

        [Test]
        public void NavigatorCanUseExplicitPortsWithoutAUnityHost()
        {
            var environment = new TestEnvironment(provider, root.transform);
            var channel = new UIFlowChannelConfig();
            Set(channel, "_channelId", UIFlowChannelId.Main);
            Set(channel, "_root", root.transform);
            Set(channel, "_protectRootFromBack", false);
            int observed = 0;
            var navigator = new UIFlowNavigator(environment, environment, _ => observed++, channel);
            var result = navigator.PushAsync(1, UIFlowOperationType.Push, Route("composed"), null,
                UIFlowNavigationOptions.Default, null, CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(result.Succeeded, Is.True);
            Assert.That(navigator.Count, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(1));
            navigator.ShutdownAsync().GetAwaiter().GetResult();
            Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(2));
        }

        [Test]
        public void RestoreTransitionFailureStillReleasesRejectedScreen()
        {
            var restore = ScriptableObject.CreateInstance<FailingTransition>();
            var reject = ScriptableObject.CreateInstance<FailingTransition>();
            assets.Add(restore);
            assets.Add(reject);
            restore.ThrowOnRestore = true;
            reject.ThrowOnShow = true;
            UIFlowRoute first = Route("previous");
            Set(first, "_showTransitionOverride", restore);
            host.PushAsync(first).GetAwaiter().GetResult();
            TestScreen previous = provider.LastScreen;
            UIFlowRoute second = Route("rejected");
            Set(second, "_showTransitionOverride", reject);

            Assert.Throws<UIFlowNavigationException>(() => host.PushAsync(second).GetAwaiter().GetResult());
            Assert.That(host.GetChannelSnapshots()[0].Stack.Count, Is.EqualTo(1));
            Assert.That(previous.gameObject.activeSelf, Is.True);
            Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            Assert.That(provider.LastScreen == null, Is.True);
            Assert.That(sink.Errors.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void ThrowingPreparationReturnsItsLeaseExactlyOnce()
        {
            provider.PrepareFailure = new InvalidOperationException("Rejected preparation");
            Assert.Throws<UIFlowNavigationException>(() => host.PushAsync(Route("failure")).GetAwaiter().GetResult());
            Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            Assert.That(provider.LastScreen == null, Is.True);
            Assert.That(host.GetChannelSnapshots()[0].Stack, Is.Empty);
        }

        [Test]
        public void CanceledPreparationReturnsItsLeaseExactlyOnce()
        {
            provider.PrepareFailure = new OperationCanceledException();
            var result = host.PushAsync(Route("canceled")).GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(UIFlowNavigationStatus.Cancelled));
            Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            Assert.That(host.GetChannelSnapshots()[0].Stack, Is.Empty);
        }

        [Test]
        public void CleanupFailureCannotRollBackACommittedPop()
        {
            host.PushAsync(Route("first")).GetAwaiter().GetResult();
            host.PushAsync(Route("second")).GetAwaiter().GetResult();
            provider.ThrowOnRelease = true;
            var result = host.PopAsync(UIFlowChannelId.Main).GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(UIFlowNavigationStatus.Succeeded));
            var stack = host.GetChannelSnapshots()[0].Stack;
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(stack[0].RouteId, Is.EqualTo(new UIFlowRouteId("first")));
            Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            Assert.That(sink.Errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void ThrowingLifetimeCallbackDoesNotSkipProviderRelease()
        {
            provider.ThrowOnLifetimeEnd = true;
            host.PushAsync(Route("lifetime")).GetAwaiter().GetResult();
            var result = host.PopAsync(UIFlowChannelId.Main).GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(UIFlowNavigationStatus.Succeeded));
            Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            Assert.That(provider.LastScreen == null, Is.True);
            Assert.That(sink.Errors, Has.Count.EqualTo(1));
        }

        private UIFlowRoute Route(string id)
        {
            var route = ScriptableObject.CreateInstance<UIFlowRoute>();
            assets.Add(route);
            Set(route, "_routeId", new UIFlowRouteId(id));
            Set(route, "_targetChannel", UIFlowChannelId.Main);
            Set(route, "_sourceMode", UIFlowScreenSourceMode.CustomProvider);
            Set(route, "_customProviderId", provider.ProviderId);
            return route;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class TestEnvironment : IUIFlowScreenEnvironment, IUIFlowGuardEnvironment
        {
            private readonly IUIFlowScreenProvider provider;
            private readonly Transform parent;
            internal TestEnvironment(IUIFlowScreenProvider provider, Transform parent)
            {
                this.provider = provider;
                this.parent = parent;
            }
            public IUIFlowScreenProvider GetProvider(UIFlowRoute route) => provider;
            public int RedirectDepthLimit => 8;
            public IReadOnlyList<UIFlowGuard> GlobalGuards => Array.Empty<UIFlowGuard>();
            public UIFlowScreenRequest CreateScreenRequest(UIFlowNavigator navigator, UIFlowRoute route, Transform requestedParent, object arguments)
                => new UIFlowScreenRequest(null, navigator, route, parent, arguments);
            public UIFlowContext CreateScreenContext(UIFlowNavigator navigator, UIFlowRoute route, Guid entryId,
                object arguments, CancellationToken lifetime, UIFlowNavigationReason reason)
                => new UIFlowContext(null, navigator, route, entryId, arguments, lifetime, reason);
            public UIFlowGuardContext CreateGuardContext(UIFlowNavigator navigator, UIFlowOperationType operation,
                UIFlowNavigationReason reason, UIFlowRoute current, UIFlowRoute target, object arguments)
                => new UIFlowGuardContext(null, navigator, operation, reason, current, target, arguments);
            public void NotifyNavigationRedirected(long operationId, UIFlowOperationType operation, UIFlowNavigationReason reason,
                UIFlowChannelId channel, UIFlowRouteId source, UIFlowRouteId target, string message, string diagnosticLabel) { }
        }

        private sealed class FailingTransition : UIFlowTransition
        {
            internal bool ThrowOnShow;
            internal bool ThrowOnRestore;
            public override Task ShowAsync(UIFlowTransitionContext context, CancellationToken token)
            {
                if (ThrowOnShow) throw new InvalidOperationException("show failed");
                ApplyShown(context.Screen);
                return Task.CompletedTask;
            }
            public override Task HideAsync(UIFlowTransitionContext context, CancellationToken token)
            {
                ApplyHidden(context.Screen);
                return Task.CompletedTask;
            }
            public override void ForceShown(UIFlowTransitionContext context)
            {
                if (ThrowOnRestore) throw new InvalidOperationException("restore failed");
                ApplyShown(context.Screen);
            }
            public override void ForceHidden(UIFlowTransitionContext context) => ApplyHidden(context.Screen);
        }

        private sealed class TestProvider : MonoBehaviour, IUIFlowScreenProvider
        {
            public string ProviderId => "test";
            public Exception PrepareFailure;
            public bool ThrowOnRelease;
            public bool ThrowOnLifetimeEnd;
            public int ReleaseCount;
            public TestScreen LastScreen;
            public Task<UIFlowScreenLease> AcquireAsync(UIFlowScreenRequest request, CancellationToken token)
            {
                var child = new GameObject("Leased screen");
                child.transform.SetParent(request.Parent, false);
                LastScreen = child.AddComponent<TestScreen>();
                LastScreen.Failure = PrepareFailure;
                LastScreen.ThrowOnLifetimeEnd = ThrowOnLifetimeEnd;
                return Task.FromResult(new UIFlowScreenLease(LastScreen, request.Route, true, false, false, this));
            }
            public Task ReleaseAsync(UIFlowScreenLease lease, CancellationToken token)
            {
                ReleaseCount++;
                if (lease.Screen != null) UnityEngine.Object.DestroyImmediate(lease.Screen.gameObject);
                if (ThrowOnRelease) throw new InvalidOperationException("Provider cleanup failed");
                return Task.CompletedTask;
            }
        }

        private sealed class TestScreen : UIFlowScreen
        {
            public Exception Failure;
            public bool ThrowOnLifetimeEnd;
            protected override Task OnPrepareAsync(UIFlowContext context, CancellationToken token)
            {
                if (ThrowOnLifetimeEnd) context.EntryLifetimeToken.Register(() => throw new InvalidOperationException("Lifetime observer failed"));
                if (Failure != null) throw Failure;
                return Task.CompletedTask;
            }
        }

        private sealed class CapturingSink : IDeucarianLogSink
        {
            public readonly List<Exception> Errors = new List<Exception>();
            public void Log(in DeucarianLogEntry entry)
            {
                if (entry.Exception != null) Errors.Add(entry.Exception);
            }
        }
    }
}
