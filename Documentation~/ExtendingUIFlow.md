# Extending UI Flow

## Custom Providers

Implement `IUIFlowScreenProvider`, expose a stable `ProviderId`, and register a component on `UIFlowHost`. Custom provider routes use `Source Mode = Custom Provider` and the matching provider ID.

## Custom Transitions

Derive from `UIFlowTransition`. Keep transition assets stateless. Use the supplied context and do not retain screen references after the task completes.

## Custom Guards

Derive from `UIFlowGuard`. Guards can allow, deny with a reason, or redirect to another route. Guards must not mutate stacks or start hidden navigation themselves.

## Custom Button Actions

Derive from `UIFlowAction` in the uGUI assembly when a button needs reusable behavior with its own serialized data. The action receives `UIFlowActionContext`, can resolve the host, and can execute through `IUIFlowRouter`. Keep the MonoBehaviour binder generic and put command-specific fields on the action asset.
