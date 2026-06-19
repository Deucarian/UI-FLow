# Basic Flow Sample

This sample demonstrates route-based navigation without direct references between screens.

## Scene Setup

1. Create a `UIFlowHost` with three channels:
   - `main`, kind `Main`, Back priority `0`, protected root enabled.
   - `modal`, kind `Modal`, Back priority `100`, protected root disabled.
   - `overlay`, kind `Overlay`, Back participation disabled.
2. Create route assets for Main Menu, Settings, Audio, Controls, Login, Confirm Quit, Info Dialog, and Loading Overlay.
3. Assign prefab-backed routes to the appropriate channel.
4. Create UI Flow action assets from `Assets > Create > Deucarian > UI Flow > Actions`.
5. Add `UIFlowButtonAction` to each Unity UI Button and assign the matching action asset.
6. Use sample-specific action assets when a button needs sample data, such as `BasicFlowPushMessageAction` for argument passing or `BasicFlowConfirmQuitAction` for a typed Boolean presentation result.

## Demonstrated Behaviors

- `UIFlowPushRouteAction`: Push.
- `BasicFlowPushMessageAction`: Push with arguments.
- `UIFlowReplaceRouteAction`: Replace.
- `UIFlowResetRouteAction`: Reset to a protected root.
- `UIFlowBackAction`: Host-level Back with modal priority.
- `BasicFlowConfirmQuitAction`: Typed modal result with `PresentAsync<bool>`.
- `UIFlowPresentRouteAction`: Dialog presentation and dismissal without a result.
- `UIFlowDismissAction`: Dialog dismissal from inside a screen.
- `UIFlowPushRouteAction` or `UIFlowResetRouteAction` on the overlay channel: overlay behavior that does not participate in Back.
- `BasicFlowLoginRedirectGuard`: Guard redirect to a Login route.
- `BasicFlowDenyGuard`: Guard denial with a structured result.

The sample deliberately avoids Core State, Generic UI Items, Addressables, Session, API, or direct screen-to-screen references.
