# Migration

Replace direct `SetActive` panel logic with route assets and host requests.

Before:

```csharp
settingsPanel.SetActive(true);
mainPanel.SetActive(false);
```

After:

```csharp
await flow.PushAsync(settingsRoute);
```

Replace per-project Back logic with `flow.BackAsync()`. Replace callback-based modal dialogs with `PresentAsync<TResult>` and `UIFlowPresentationResult<TResult>`.
