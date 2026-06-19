# Basic Flow Sample

This sample demonstrates route-based navigation without direct references between screens.

## Scene Setup

1. Create a `UIFlowHost` with three channels:
   - `main`, kind `Main`, Back priority `0`, protected root enabled.
   - `modal`, kind `Modal`, Back priority `100`, protected root disabled.
   - `overlay`, kind `Overlay`, Back participation disabled.
2. Create route assets for Main Menu, Settings, Audio, Controls, Login, Confirm Quit, Info Dialog, and Loading Overlay.
3. Assign prefab-backed routes to the appropriate channel.
4. Put `BasicFlowSampleController` in the scene and assign the host and routes.
5. Wire Unity UI buttons to the controller methods.

## Demonstrated Behaviors

- `OpenSettings`: Push with arguments.
- `OpenControls`: Replace.
- `ResetToMainMenu`: Reset to a protected root.
- `Back`: Host-level Back with modal priority.
- `ConfirmQuit`: Typed modal result with `PresentAsync<bool>`.
- `ShowInfoDialog`: Dialog dismissal without a result.
- `ShowLoadingOverlay` and `HideOverlay`: Overlay channel that does not participate in Back.
- `BasicFlowLoginRedirectGuard`: Guard redirect to a Login route.
- `BasicFlowDenyGuard`: Guard denial with a structured result.

The sample deliberately avoids Core State, Generic UI Items, Addressables, Session, API, or direct screen-to-screen references.
