# Deucarian UI Flow

Deucarian UI Flow is a Unity Package Manager package for deterministic, asynchronous UI navigation. It replaces project-specific `UIManager` singletons and direct panel references with explicit hosts, channels, routes, queued operations, guards, transitions, and typed modal presentations.

Package ID: `com.deucarian.ui-flow`

## Installation

Install through Unity Package Manager with a Git URL:

```json
{
  "dependencies": {
    "com.deucarian.ui-flow": "https://github.com/Deucarian/UI-FLow.git#develop"
  }
}
```

The package requires Unity `2021.3` or newer. It depends on `com.unity.ugui` for optional button adapters; the core runtime assembly does not reference `UnityEngine.UI`.

## Quick Start

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

## Runtime Rules

Public host APIs are main-thread-only and throw a contextual `UIFlowNavigationException` when called from another thread. Navigation requests before initialization are queued behind initialization. Unexpected provider, guard, lifecycle, or transition failures attempt rollback, raise a failure event, and throw `UIFlowNavigationException`.

## Samples

The package contains one sample:

- `Basic Flow`: `Samples~/BasicFlow`

It demonstrates Push, Pop/Back, Replace, Reset, arguments, typed modal results, guard denial, guard redirect, overlay behavior, rapid-click queueing, and nested-host-friendly routing.

## Integrations

This package intentionally has no runtime dependency on Core State, API, Session, UI Binding, Object Selection, or bridge packages.

Planned separate integrations:

- UI Flow to Core State bridge
- UI Flow to Generic UI Items / UI Binding bridge
- Addressables screen provider
- Input System Back adapter
- UI Toolkit adapter
- navigation-state persistence and deep links
- visual route graph/editor
- Scene Flow integration
