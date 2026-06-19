# Getting Started

Create route assets with `Assets > Create > Deucarian > UI Flow > Route`. Each route receives a stable generated route ID. Configure its target channel, source mode, lifetime, duplicate policy, transitions, and guards.

Add `UIFlowHost` to a scene and configure channel roots. Common IDs are `main`, `modal`, and `overlay`. Assign a `UIFlowRouteCatalog` if you need route-ID lookup.

Use the host as an `IUIFlowRouter`:

```csharp
await host.PushAsync(settingsRoute);
await host.BackAsync();
```

The host is main-thread-only. Marshal work to Unity's synchronization context before calling it from background code.
