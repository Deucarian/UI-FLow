# Lifecycle

For Push and Present:

1. Guards run.
2. Provider acquires a screen.
3. `OnPrepareAsync`.
4. Current screen `OnBeforeCoverAsync`.
5. Destination `OnBeforeEnterAsync`.
6. Transitions run.
7. Current screen `OnAfterCover`.
8. Destination `OnAfterEnter`.

For Pop, Back, and Dismiss:

1. Exit guard runs.
2. Top screen `OnBeforeExitAsync`.
3. Revealed screen `OnBeforeRevealAsync`.
4. Transitions run.
5. Top screen `OnAfterExit`.
6. Revealed screen `OnAfterReveal`.
7. Removed entry lifetime token is cancelled.
8. Screen context is cleared and provider release runs.

Synchronous after-hooks are caught and logged after state is committed.
