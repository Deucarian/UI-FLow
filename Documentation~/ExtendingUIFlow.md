# Extending UI Flow

## Custom Providers

Implement `IUIFlowScreenProvider`, expose a stable `ProviderId`, and register a component on `UIFlowHost`. Custom provider routes use `Source Mode = Custom Provider` and the matching provider ID.

## Custom Transitions

Derive from `UIFlowTransition`. Keep transition assets stateless. Use the supplied context and do not retain screen references after the task completes.

## Custom Guards

Derive from `UIFlowGuard`. Guards can allow, deny with a reason, or redirect to another route. Guards must not mutate stacks or start hidden navigation themselves.
