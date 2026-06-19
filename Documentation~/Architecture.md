# Architecture

UI Flow uses explicit dependency direction:

```text
Application code -> IUIFlowRouter -> Navigator -> Provider -> Screen
                                      |
                                      v
                                  Transition
```

Routes describe destinations. Channels own independent stacks. The host serializes requests through a queue so one operation mutates a navigator at a time. Screens receive `UIFlowContext` and navigate by route or by closing their exact stack entry.

There is no global `UIManager.Instance`. Multiple hosts and nested hosts are supported because all navigation state is owned by a concrete host component.
