# Integrations

UI Flow 0.1.0 intentionally ships without bridge packages.

Planned integrations belong in separate packages:

- UI Flow to Core State bridge
- UI Flow to Generic UI Items / UI Binding bridge
- Addressables provider
- Input System adapter
- UI Toolkit adapter
- navigation-state persistence and deep links
- visual route graph/editor
- Scene Flow integration

Applications can compose UI Flow with those systems today in project code by requesting routes from their own application services.
