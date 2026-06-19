using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.UIFlow;
using Deucarian.UIFlow.UGUI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.UIFlow.Tests.PlayMode
{
    public sealed class UIFlowRuntimeNavigationTests
    {
        private GameObject _hostObject;

        [TearDown]
        public void TearDown()
        {
            if (_hostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hostObject);
            }

            BlockingScreen.ResetGate();
        }

        [UnityTest]
        public IEnumerator PushThenPopReturnsToPreviousEntry()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute a = CreateRoute("a", world.MainRoot, typeof(TestScreen));
            UIFlowRoute b = CreateRoute("b", world.MainRoot, typeof(TestScreen));

            yield return Await(world.Host.PushAsync(a));
            yield return Await(world.Host.PushAsync(b));
            yield return Await(world.Host.PopAsync(UIFlowChannelId.Main));

            UIFlowChannelSnapshot main = world.Host.GetChannelSnapshots()[0];
            Assert.AreEqual(1, main.Stack.Count);
            Assert.AreEqual(new UIFlowRouteId("a"), main.Stack[0].RouteId);
        }

        [UnityTest]
        public IEnumerator ReplaceDoesNotRetainPreviousEntry()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute a = CreateRoute("a", world.MainRoot, typeof(TestScreen));
            UIFlowRoute b = CreateRoute("b", world.MainRoot, typeof(TestScreen));

            yield return Await(world.Host.PushAsync(a));
            yield return Await(world.Host.ReplaceAsync(b));

            UIFlowChannelSnapshot main = world.Host.GetChannelSnapshots()[0];
            Assert.AreEqual(1, main.Stack.Count);
            Assert.AreEqual(new UIFlowRouteId("b"), main.Stack[0].RouteId);
        }

        [UnityTest]
        public IEnumerator ResetClearsPreviousHistory()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute a = CreateRoute("a", world.MainRoot, typeof(TestScreen));
            UIFlowRoute b = CreateRoute("b", world.MainRoot, typeof(TestScreen));
            UIFlowRoute c = CreateRoute("c", world.MainRoot, typeof(TestScreen));

            yield return Await(world.Host.PushAsync(a));
            yield return Await(world.Host.PushAsync(b));
            yield return Await(world.Host.ResetAsync(UIFlowChannelId.Main, c));

            UIFlowChannelSnapshot main = world.Host.GetChannelSnapshots()[0];
            Assert.AreEqual(1, main.Stack.Count);
            Assert.AreEqual(new UIFlowRouteId("c"), main.Stack[0].RouteId);
        }

        [UnityTest]
        public IEnumerator PopToRemovesEntriesAboveRoute()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute a = CreateRoute("a", world.MainRoot, typeof(TestScreen));
            UIFlowRoute b = CreateRoute("b", world.MainRoot, typeof(TestScreen));
            UIFlowRoute c = CreateRoute("c", world.MainRoot, typeof(TestScreen));

            yield return Await(world.Host.PushAsync(a));
            yield return Await(world.Host.PushAsync(b));
            yield return Await(world.Host.PushAsync(c));
            yield return Await(world.Host.PopToAsync(UIFlowChannelId.Main, new UIFlowRouteId("a")));

            UIFlowChannelSnapshot main = world.Host.GetChannelSnapshots()[0];
            Assert.AreEqual(1, main.Stack.Count);
            Assert.AreEqual(new UIFlowRouteId("a"), main.Stack[0].RouteId);
        }

        [UnityTest]
        public IEnumerator ProtectedRootBackReturnsNoOp()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute a = CreateRoute("a", world.MainRoot, typeof(TestScreen));

            yield return Await(world.Host.PushAsync(a));
            Task<UIFlowNavigationResult> back = world.Host.BackAsync();
            yield return Await(back);

            Assert.AreEqual(UIFlowNavigationStatus.NoOp, back.Result.Status);
        }

        [UnityTest]
        public IEnumerator ModalBackIsHandledBeforeMainChannel()
        {
            TestWorld world = CreateWorld(includeModal: true);
            UIFlowRoute mainA = CreateRoute("main-a", world.MainRoot, typeof(TestScreen), UIFlowChannelId.Main);
            UIFlowRoute mainB = CreateRoute("main-b", world.MainRoot, typeof(TestScreen), UIFlowChannelId.Main);
            UIFlowRoute modal = CreateRoute("modal", world.ModalRoot, typeof(TestScreen), UIFlowChannelId.Modal);

            yield return Await(world.Host.PushAsync(mainA));
            yield return Await(world.Host.PushAsync(mainB));
            yield return Await(world.Host.PushAsync(modal));
            yield return Await(world.Host.BackAsync());

            Assert.AreEqual(2, FindChannel(world.Host, UIFlowChannelId.Main).Stack.Count);
            Assert.AreEqual(0, FindChannel(world.Host, UIFlowChannelId.Modal).Stack.Count);
        }

        [UnityTest]
        public IEnumerator OverlayIgnoredByBackWhenNotConfigured()
        {
            TestWorld world = CreateWorld(includeOverlay: true);
            UIFlowRoute mainA = CreateRoute("main-a", world.MainRoot, typeof(TestScreen), UIFlowChannelId.Main);
            UIFlowRoute mainB = CreateRoute("main-b", world.MainRoot, typeof(TestScreen), UIFlowChannelId.Main);
            UIFlowRoute overlay = CreateRoute("overlay", world.OverlayRoot, typeof(TestScreen), UIFlowChannelId.Overlay);

            yield return Await(world.Host.PushAsync(mainA));
            yield return Await(world.Host.PushAsync(mainB));
            yield return Await(world.Host.PushAsync(overlay));
            yield return Await(world.Host.BackAsync());

            Assert.AreEqual(1, FindChannel(world.Host, UIFlowChannelId.Main).Stack.Count);
            Assert.AreEqual(1, FindChannel(world.Host, UIFlowChannelId.Overlay).Stack.Count);
        }

        [UnityTest]
        public IEnumerator RapidPushRequestsExecuteDeterministically()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute route = CreateRoute("repeat", world.MainRoot, typeof(TestScreen));

            var tasks = new Task<UIFlowNavigationResult>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = world.Host.PushAsync(route);
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                yield return Await(tasks[i]);
                Assert.AreEqual(UIFlowNavigationStatus.Succeeded, tasks[i].Result.Status);
            }

            Assert.AreEqual(10, world.Host.GetChannelSnapshots()[0].Stack.Count);
        }

        [UnityTest]
        public IEnumerator RejectIfBusyRejectsWhileOperationIsActive()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute slow = CreateRoute("slow", world.MainRoot, typeof(BlockingScreen));
            UIFlowRoute fast = CreateRoute("fast", world.MainRoot, typeof(TestScreen));

            Task<UIFlowNavigationResult> first = world.Host.PushAsync(slow);
            yield return null;

            var options = UIFlowNavigationOptions.Default;
            options.ConflictPolicy = UIFlowRequestConflictPolicy.RejectIfBusy;
            Task<UIFlowNavigationResult> second = world.Host.PushAsync(fast, null, options);
            yield return Await(second);

            Assert.AreEqual(UIFlowNavigationStatus.Rejected, second.Result.Status);
            BlockingScreen.ReleaseGate();
            yield return Await(first);
        }

        [UnityTest]
        public IEnumerator PresentCompletesWithTypedResult()
        {
            TestWorld world = CreateWorld(includeModal: true);
            UIFlowRoute modal = CreateRoute("confirm", world.ModalRoot, typeof(CompletingScreen), UIFlowChannelId.Modal);

            Task<UIFlowPresentationResult<bool>> presentation = world.Host.PresentAsync<bool>(modal);
            yield return null;
            CompletingScreen.LastInstance.RequestClose(true);
            yield return Await(presentation);

            Assert.IsTrue(presentation.Result.HasValue);
            Assert.IsTrue(presentation.Result.Value);
        }

        [UnityTest]
        public IEnumerator PushRouteActionExecutesThroughHostRouter()
        {
            TestWorld world = CreateWorld();
            UIFlowRoute route = CreateRoute("action-target", world.MainRoot, typeof(TestScreen));
            UIFlowPushRouteAction action = ScriptableObject.CreateInstance<UIFlowPushRouteAction>();
            SetField(action, "_route", route);

            Task task = action.ExecuteAsync(new UIFlowActionContext(world.Host, null, world.Host), CancellationToken.None);
            yield return Await(task);

            UIFlowChannelSnapshot main = FindChannel(world.Host, UIFlowChannelId.Main);
            Assert.AreEqual(1, main.Stack.Count);
            Assert.AreEqual(new UIFlowRouteId("action-target"), main.Stack[0].RouteId);
        }

        [Test]
        public void BackAndDismissActionsDoNotExposeRouteFields()
        {
            Assert.IsNull(typeof(UIFlowBackAction).GetField("_route", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(UIFlowDismissAction).GetField("_route", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        private TestWorld CreateWorld(bool includeModal = false, bool includeOverlay = false)
        {
            _hostObject = new GameObject("UI Flow Host");
            UIFlowHost host = _hostObject.AddComponent<UIFlowHost>();

            Transform mainRoot = new GameObject("Main").transform;
            mainRoot.SetParent(_hostObject.transform, false);
            Transform modalRoot = null;
            Transform overlayRoot = null;

            var channels = new System.Collections.Generic.List<UIFlowChannelConfig>
            {
                CreateChannel(UIFlowChannelId.Main, UIFlowChannelKind.Main, mainRoot, true, 0, true)
            };

            if (includeModal)
            {
                modalRoot = new GameObject("Modal").transform;
                modalRoot.SetParent(_hostObject.transform, false);
                channels.Add(CreateChannel(UIFlowChannelId.Modal, UIFlowChannelKind.Modal, modalRoot, true, 100, false));
            }

            if (includeOverlay)
            {
                overlayRoot = new GameObject("Overlay").transform;
                overlayRoot.SetParent(_hostObject.transform, false);
                channels.Add(CreateChannel(UIFlowChannelId.Overlay, UIFlowChannelKind.Overlay, overlayRoot, false, 50, false));
            }

            SetField(host, "_channels", channels.ToArray());
            UIFlowRouteCatalog catalog = ScriptableObject.CreateInstance<UIFlowRouteCatalog>();
            SetField(catalog, "_routes", new UIFlowRoute[0]);
            SetField(host, "_routeCatalog", catalog);
            return new TestWorld(host, mainRoot, modalRoot, overlayRoot);
        }

        private static UIFlowChannelConfig CreateChannel(UIFlowChannelId id, UIFlowChannelKind kind, Transform root, bool participatesInBack, int priority, bool protectRoot)
        {
            var channel = new UIFlowChannelConfig();
            SetField(channel, "_channelId", id);
            SetField(channel, "_kind", kind);
            SetField(channel, "_root", root);
            SetField(channel, "_participatesInBack", participatesInBack);
            SetField(channel, "_backPriority", priority);
            SetField(channel, "_protectRootFromBack", protectRoot);
            return channel;
        }

        private static UIFlowRoute CreateRoute(string id, Transform root, Type screenType, UIFlowChannelId? channelId = null)
        {
            GameObject prefab = new GameObject(id + " Prefab");
            UIFlowScreen screen = (UIFlowScreen)prefab.AddComponent(screenType);

            UIFlowRoute route = ScriptableObject.CreateInstance<UIFlowRoute>();
            route.name = id;
            SetField(route, "_routeId", new UIFlowRouteId(id));
            SetField(route, "_displayName", id);
            SetField(route, "_targetChannel", channelId.HasValue ? channelId.Value : UIFlowChannelId.Main);
            SetField(route, "_sourceMode", UIFlowScreenSourceMode.Prefab);
            SetField(route, "_prefab", screen);
            SetField(route, "_lifetime", UIFlowScreenLifetime.Transient);
            SetField(route, "_duplicateRoutePolicy", UIFlowDuplicateRoutePolicy.Allow);
            return route;
        }

        private static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted && task.Exception != null)
            {
                throw task.Exception.GetBaseException();
            }

            if (task.IsCanceled)
            {
                throw new OperationCanceledException();
            }
        }

        private static UIFlowChannelSnapshot FindChannel(UIFlowHost host, UIFlowChannelId channelId)
        {
            IReadOnlyList<UIFlowChannelSnapshot> snapshots = host.GetChannelSnapshots();
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].ChannelId == channelId)
                {
                    return snapshots[i];
                }
            }

            Assert.Fail("Missing channel " + channelId);
            return null;
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, "Missing field " + fieldName + " on " + instance.GetType().Name);
            field.SetValue(instance, value);
        }

        private sealed class TestWorld
        {
            public TestWorld(UIFlowHost host, Transform mainRoot, Transform modalRoot, Transform overlayRoot)
            {
                Host = host;
                MainRoot = mainRoot;
                ModalRoot = modalRoot;
                OverlayRoot = overlayRoot;
            }

            public UIFlowHost Host;
            public Transform MainRoot;
            public Transform ModalRoot;
            public Transform OverlayRoot;
        }

        private class TestScreen : UIFlowScreen
        {
        }

        private sealed class BlockingScreen : UIFlowScreen
        {
            private static TaskCompletionSource<bool> Gate = new TaskCompletionSource<bool>();

            public static void ReleaseGate()
            {
                Gate.TrySetResult(true);
            }

            public static void ResetGate()
            {
                Gate = new TaskCompletionSource<bool>();
            }

            protected override Task OnPrepareAsync(UIFlowContext context, CancellationToken cancellationToken)
            {
                return Gate.Task;
            }
        }

        private sealed class CompletingScreen : UIFlowScreen
        {
            public static CompletingScreen LastInstance;

            protected override Task OnPrepareAsync(UIFlowContext context, CancellationToken cancellationToken)
            {
                LastInstance = this;
                return Task.CompletedTask;
            }

            public void RequestClose(bool value)
            {
                CloseAsync(value);
            }
        }
    }
}
