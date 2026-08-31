# Changelog

## Unreleased

- Registered UI Flow validation and the standalone debugger with Deucarian Control Center, removed their global menu entries, and aligned Logging to 1.0.4.

## 0.4.1 - 2026-07-17

- Added an importable Basic Flow scene and aligned exact Common and Logging dependencies.

## 0.4.0 - 2026-06-22

- Added `com.deucarian.common` as a runtime dependency for owned screen GameObject cleanup.
- Replaced direct prefab screen destruction with `UnityObjectUtility.DestroySafely`.

## 0.3.0 - 2026-06-22

- Added the public `UIFlowLog` facade backed by Deucarian Logging.
- Replaced direct Unity Debug calls across runtime, uGUI adapters, editor validation, and samples with package-owned log categories.
- Added `com.deucarian.logging` as a direct dependency and documented stable UI Flow logging categories.

## 0.2.0

- Added the `UIFlowButtonAction` event binder and reusable `UIFlowAction` ScriptableObject action model.
- Added built-in action assets for Push, Replace, Reset, Back, Dismiss, and Present.
- Marked the old `UIFlowNavigateButton`, `UIFlowBackButton`, and `UIFlowDismissButton` components obsolete as compatibility wrappers.
- Updated the Basic Flow sample to use action assets for button behavior.
- Added editor guidance for missing buttons, actions, hosts, and context-sensitive actions.
- Added PlayMode coverage for the action asset model.

## 0.1.0

- Created the first Deucarian UI Flow UPM package.
- Added route assets, stable route IDs, route catalogs, channel configuration, host-owned navigators, queued async navigation, Back routing, modal presentations, guards, providers, transitions, diagnostics, uGUI button adapters, editor validation, tests, sample scripts, and documentation.
