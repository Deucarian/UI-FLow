using System;
using System.Collections.Generic;
using Deucarian.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.UIFlow.Tests.EditMode
{
    public sealed class UIFlowLogTests
    {
        private CapturingSink sink;
        private GameObject context;

        [SetUp]
        public void SetUp()
        {
            DeucarianLogSettings.ResetToDefaults();
            DeucarianLog.ClearSinks();
            sink = new CapturingSink();
            DeucarianLog.RegisterSink(sink);
            context = new GameObject("UI Flow Log Context");
        }

        [TearDown]
        public void TearDown()
        {
            DeucarianLogSettings.ResetToDefaults();
            DeucarianLog.ResetSinksToDefault();

            if (context != null)
            {
                UnityEngine.Object.DestroyImmediate(context);
                context = null;
            }
        }

        [Test]
        public void WarningPreservesCategorySeverityAndContext()
        {
            UIFlowLog.UGUI.Warning("UIFlowButtonAction requires a UIFlowAction asset.", context);

            Assert.AreEqual(1, sink.Entries.Count);
            Assert.AreEqual(DeucarianLogLevel.Warning, sink.Entries[0].Level);
            Assert.AreEqual("UIFlow.UGUI", sink.Entries[0].Category);
            Assert.AreEqual("UIFlowButtonAction requires a UIFlowAction asset.", sink.Entries[0].Message);
            Assert.AreSame(context, sink.Entries[0].Context);
        }

        [Test]
        public void ExceptionPreservesExceptionObjectMessageAndContext()
        {
            var exception = new InvalidOperationException("observer failed");

            UIFlowLog.Navigation.Exception(exception, "A UI Flow observer threw. Navigation state was preserved.", context);

            Assert.AreEqual(1, sink.Entries.Count);
            Assert.AreEqual(DeucarianLogLevel.Exception, sink.Entries[0].Level);
            Assert.AreEqual("UIFlow.Navigation", sink.Entries[0].Category);
            Assert.AreEqual("A UI Flow observer threw. Navigation state was preserved.", sink.Entries[0].Message);
            Assert.AreSame(exception, sink.Entries[0].Exception);
            Assert.AreSame(context, sink.Entries[0].Context);
        }

        private sealed class CapturingSink : IDeucarianLogSink
        {
            private readonly List<DeucarianLogEntry> entries = new List<DeucarianLogEntry>();

            public IReadOnlyList<DeucarianLogEntry> Entries
            {
                get { return entries; }
            }

            public void Log(in DeucarianLogEntry entry)
            {
                entries.Add(entry);
            }
        }
    }
}
