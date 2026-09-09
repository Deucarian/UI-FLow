# Deucarian UI Flow

## What this is

`com.deucarian.ui-flow` is a Unity Package Manager package for deterministic, asynchronous UI navigation. It replaces project-specific `UIManager` singletons and direct panel references with explicit hosts, channels, routes, queued operations, guards, transitions, action assets, and typed modal presentations.

Current package version: `0.4.3`.

## When to use it

- You need explicit screen navigation without singleton UI managers.
- You need channels such as main, modal, and overlay.
- You need queued async navigation with guards, transitions, route assets, and typed modal results.
- You need uGUI button adapters that trigger reusable action assets.

## When not to use it

- Do not use UI Flow for collection binding; `com.deucarian.ui-binding` owns that.
- Do not put Core State coupling, persistence, API/session behavior, or object selection in the core package.
- Do not use this package as a generic UI framework or visual styling owner.

## Install

Stable:

```json
"com.deucarian.ui-flow": "https://github.com/Deucarian/UI-FLow.git#main"
```

Development:

```json
"com.deucarian.ui-flow": "https://github.com/Deucarian/UI-FLow.git#develop"
```

## Dependencies

- `com.deucarian.common`: runtime dependency used for safe owned screen cleanup across Play Mode and Edit Mode.
- `com.deucarian.logging`: runtime dependency used by UI Flow's package-owned log categories. Logs remain local-only and do not add telemetry or remote reporting.
- `com.unity.ugui`: supports the optional uGUI button adapters and action-asset binders.

The core runtime assembly does not reference `UnityEngine.UI`.

## Unity compatibility

Requires Unity 2021.3 or newer.

## 60-second quick start

1. Create `UIFlowRoute` assets for each screen.
2. Add a `UIFlowHost` to the scene.
3. Configure channels such as `main`, `modal`, and `overlay`.
4. Assign a `UIFlowRouteCatalog`.
5. Navigate by route instead of direct panel references.

```csharp
await flow.PushAsync(settingsRoute);

await flow.ReplaceAsync(
    profileRoute,
    new ProfileArguments(userId));

UIFlowPresentationResult<bool> result =
    await flow.PresentAsync<bool>(confirmQuitRoute);

if (result.HasValue && result.Value)
{
    // Quit was confirmed.
}

await flow.BackAsync();
```

For Unity UI buttons, put `UIFlowButtonAction` on the Button GameObject and assign a `UIFlowAction` asset. The MonoBehaviour only binds the click event; the ScriptableObject owns the command and its data.

Built-in action assets:

- `UIFlowPushRouteAction`
- `UIFlowReplaceRouteAction`
- `UIFlowResetRouteAction`
- `UIFlowBackAction`
- `UIFlowDismissAction`
- `UIFlowPresentRouteAction`

## Architecture

```text
Application code
      |
      v
IUIFlowRouter / UIFlowHost
      |
      +-- Main navigator
      +-- Modal navigator
      +-- Overlay navigator
             |
             v
      Providers -> Screens
             |
             v
         Transitions
```

`UIFlowHost` is not a singleton. A scene can contain multiple independent hosts, and a screen can contain a nested host. The host serializes navigation requests through a command queue, owns channel navigators, resolves routes, applies guards, runs transitions, and shuts down queued work when destroyed.

## Public API

- `IUIFlowRouter`: `PushAsync`, `PopAsync`, `ReplaceAsync`, `ResetAsync`, `PopToAsync`, `BackAsync`, `PresentAsync<TResult>`, `InitializeAsync`, route lookup, snapshots, queue count, and current operation.
- `UIFlowHost`: scene component implementing `IUIFlowRouter`.
- `UIFlowRoute`: stable route asset with channel, source mode, lifetime, duplicate policy, transitions, guards, and notes.
- `UIFlowRouteCatalog`: serialized route lookup by stable route ID.
- `UIFlowScreen`: base class for lifecycle hooks and context-aware close/dismiss helpers.
- `IUIFlowScreenProvider`: extension point for custom route sources.
- `UIFlowTransition`: extension point for stateless show/hide transitions.
- `UIFlowGuard`: ordered asynchronous allow, deny, or redirect decisions.
- `UIFlowButtonAction`: uGUI Button event binder.
- `UIFlowAction`: base class for reusable action assets.

## Runtime Rules

Public host APIs are main-thread-only and throw a contextual `UIFlowNavigationException` when called from another thread. Navigation requests before initialization are queued behind initialization. Unexpected provider, guard, lifecycle, or transition failures attempt rollback, raise a failure event, and throw `UIFlowNavigationException`.

## Logging

UI Flow logs through stable Deucarian Logging categories:

- `UIFlow`
- `UIFlow.Navigation`
- `UIFlow.Screens`
- `UIFlow.UGUI`
- `UIFlow.Validation`
- `UIFlow.Samples`

Configure Deucarian Logging filters by category and level to isolate navigation queue, screen lifecycle, uGUI adapter, project validation, or sample output. UI Flow does not add telemetry, analytics, or remote reporting.

## Samples

The package contains one sample:

- `Basic Flow`: `Samples~/BasicFlow`

It demonstrates Push, Pop/Back, Replace, Reset, arguments, typed modal results, guard denial, guard redirect, overlay behavior, rapid-click queueing, and nested-host-friendly routing.

The sample uses `UIFlowButtonAction` plus action assets. The old separate button components remain as obsolete compatibility wrappers.

## Integrations

The editor assembly uses `com.deucarian.editor` for its Control Center contribution and shared debugger workbench. Runtime UI Flow assemblies remain independent of Editor.

Aside from Deucarian Common, Deucarian Logging, and Unity uGUI support, this package intentionally has no runtime dependency on Core State, API, Session, UI Binding, Object Selection, or Integration packages.

Planned separate integrations:

- UI Flow to Core State integration
- UI Flow to Generic UI Items / UI Binding integration
- Addressables screen provider
- Input System Back adapter
- UI Toolkit adapter
- navigation-state persistence and deep links
- visual route graph/editor
- Scene Flow integration

## Troubleshooting

- If navigation is rejected, inspect route guards and the thrown `UIFlowNavigationException`.
- If queued navigation appears stuck, confirm the host was initialized and that a provider/transition did not fail.
- If uGUI button actions do not fire, confirm `UIFlowButtonAction` is on the same GameObject as the Button and references a `UIFlowAction` asset.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package's EditMode and PlayMode tests in Unity after code or assembly definition changes.

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Architecture / Contributor Notes

- [AGENTS.md](AGENTS.md) contains repository-specific ownership and Codex guidance.
- Deucarian architecture rules live in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md).
- Capability ownership is tracked in [CAPABILITY_OWNERSHIP.md](https://github.com/Deucarian/Package-Registry/blob/develop/CAPABILITY_OWNERSHIP.md).

## License

See [LICENSE.md](LICENSE.md).
