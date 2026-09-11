using System;
using System.Linq;
using System.Reflection;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Deucarian.UIFlow.Tests.EditMode
{
    public sealed class UIFlowControlCenterTests
    {
        [TestCase("", "details")]
        [TestCase("home", "home")]
        public void MissingSelectionFallsBackToTopVisibleScreenInTheSameRender(string search, string expected)
        {
            var type = typeof(Deucarian.UIFlow.Editor.UIFlowDebuggerWindow).Assembly.GetType("Deucarian.UIFlow.Editor.UIFlowDebuggerPage", true);
            var controller = Activator.CreateInstance(type, true);
            using (var page = (IDeucarianEditorPage)type.GetProperty("Page").GetValue(controller))
            {
                var home = new UIFlowStackEntrySnapshot(Guid.NewGuid(), new UIFlowRouteId("home"), "Home", UIFlowScreenLifecycleState.Covered, false);
                var details = new UIFlowStackEntrySnapshot(Guid.NewGuid(), new UIFlowRouteId("details"), "Details", UIFlowScreenLifecycleState.Active, false);
                var snapshot = new UIFlowHostSnapshot("Fixture", UIFlowInitializationState.Initialized,
                    new[] { new UIFlowChannelSnapshot(UIFlowChannelId.Main, UIFlowChannelKind.Main, new[] { home, details }) }, null, 0, null, null);
                type.GetField("channelId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, UIFlowChannelId.Main.ToString());
                type.GetField("selectedId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, Guid.NewGuid().ToString());
                page.Root.Q<TextField>("workspace-search").SetValueWithoutNotify(search);
                type.GetMethod("RenderCollection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { snapshot });
                Assert.That(page.Root.Q<Label>("ui-flow-route").text, Is.EqualTo(expected));
                Assert.That(page.Root.Q("workspace-collection-rows").Query(className: "dw-selected").ToList(), Has.Count.EqualTo(1));
                Assert.That(page.Root.Q("ui-flow-trail").Query(className: "dw-selected").ToList(), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void DebuggerUsesStackPanelsAndKeepsTransitionDisclosureWhenReturning()
        {
            Assert.That(DeucarianToolRegistry.TryGet("deucarian.ui-flow.debugger", out var tool), Is.True);
            using (var page = tool.CreatePage())
            {
                Assert.That(page.Root.Q(className: "dw-collection-panels"), Is.Not.Null);
                Assert.That(page.Root.Q(className: "dw-detached-details"), Is.Not.Null);
                Assert.That(page.Root.Q("ui-flow-selected-screen").Q<Foldout>(), Is.Null);
                var transition = page.Root.Q<Foldout>("ui-flow-last-transition");
                Assert.That(transition, Is.Not.Null);
                Assert.That(transition.value, Is.False);
                transition.value = true;
                page.Deactivate(); page.Activate(null);
                Assert.That(page.Root.Q<Foldout>("ui-flow-last-transition"), Is.SameAs(transition));
                Assert.That(transition.value, Is.True);
            }
        }

        [Test]
        public void ContributionRegistersStableExperienceToolAndActions()
        {
            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture();
            DeucarianToolDescriptor tool = snapshot.Tools.Single(candidate =>
                candidate.Id == "deucarian.ui-flow.debugger");
            DeucarianControlCenterCard card = snapshot.Cards.Single(candidate =>
                candidate.Id == "com.deucarian.ui-flow.experience");

            Assert.That(tool.Area, Is.EqualTo(DeucarianControlCenterArea.Experience));
            Assert.That(card.Area, Is.EqualTo(DeucarianControlCenterArea.Experience));
            CollectionAssert.AreEqual(
                new[] { "open-debugger", "validate-project" },
                card.Actions.Select(action => action.Id).ToArray());
        }
    }
}
