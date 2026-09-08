using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.UIFlow
{
    internal sealed class UIFlowStackEntry
    {
        public UIFlowStackEntry(UIFlowRoute route, UIFlowScreenLease lease, UIFlowPresentationState presentation)
        {
            EntryId = Guid.NewGuid();
            Route = route;
            Lease = lease;
            Screen = lease.Screen;
            Presentation = presentation;
            EntryLifetimeCts = new CancellationTokenSource();
        }

        public Guid EntryId;
        public UIFlowRoute Route;
        public UIFlowScreenLease Lease;
        public IUIFlowScreenProvider ReleaseProvider;
        public UIFlowScreen Screen;
        public UIFlowContext Context;
        public UIFlowPresentationState Presentation;
        public CancellationTokenSource EntryLifetimeCts;
        public bool Released;

        public CancellationToken EntryLifetimeToken
        {
            get { return EntryLifetimeCts.Token; }
        }
    }
}
