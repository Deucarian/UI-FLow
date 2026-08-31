using System.Linq;
using Deucarian.Editor;
using NUnit.Framework;

namespace Deucarian.UIFlow.Tests.EditMode
{
    public sealed class UIFlowControlCenterTests
    {
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