# Troubleshooting

`Route targets channel X but this navigator is Y`: assign the route to the correct channel or request it through the host so the host can resolve the target channel.

`Add a matching channel configuration`: the host has no channel with the route's target channel ID.

`Custom provider routes require a non-empty provider ID`: set the route's custom provider ID and register a provider component on the host.

`Only the top entry in its channel can close itself`: a screen tried to close after it was covered or removed. Close/dismiss helpers target the exact stack entry for safety.

`UI Flow host APIs must be called from Unity's main thread`: marshal work back to Unity before calling the router.

`UIFlowButtonAction requires a UIFlowAction asset`: create an action asset from `Assets > Create > Deucarian > UI Flow > Actions` and assign it to the binder.

`UI Flow action requires an explicit UIFlowHost or a parent UIFlowHost`: assign the host on the binder or place the button under the host hierarchy.

`UI Flow dismiss action must run from inside an active UIFlowScreen context`: use `UIFlowDismissAction` on buttons inside screen prefabs that are currently presented by UI Flow.
