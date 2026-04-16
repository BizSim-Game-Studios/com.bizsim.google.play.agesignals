# [BETA] BizSim Google Play Age Signals

Last reviewed: 2026-04-16

## Overview

This package provides a Unity bridge for the Google Play Age Signals API (v0.0.3 Beta).
It wraps the native Age Signals SDK via a JNI bridge, exposes a main-thread-safe singleton
controller, and enforces strict privacy constraints: raw age data stays in memory only,
only derived behavior flags are persisted via an encrypted cache, and analytics events
carry zero age information.

**BETA:** The upstream Google Play Age Signals API is in beta. The API surface and
availability may change without notice. This package pins to `v0.0.3` of the SDK.

The package compiles only for Android and Editor platforms. On non-Android builds and in the
Unity Editor, the mock provider is used automatically so you can iterate without a device.

## Contents

| File | Description |
|------|-------------|
| [getting-started.md](getting-started.md) | Step-by-step installation and first API call |
| [api-reference.md](api-reference.md) | Full public API surface with types, methods, and parameters |
| [configuration.md](configuration.md) | Settings asset fields and Editor window walkthrough |
| [architecture.md](architecture.md) | JNI bridge diagram, thread model, privacy architecture |
| [troubleshooting.md](troubleshooting.md) | Common errors with root causes and fixes |
| [DATA_SAFETY.md](DATA_SAFETY.md) | Play Store Data Safety form input |

## Privacy constraints

These constraints are non-negotiable and enforced at the architecture level:

1. Raw age data (`AgeSignalsResult`) is kept in memory only -- never persisted
2. Only derived behavior flags (`AgeRestrictionFlags`) are saved to encrypted PlayerPrefs
3. Analytics events log only technical success/failure -- no age info transmitted
4. Custom cache providers must implement `IAgeSignalsCacheProvider` and preserve this split

## Links

- [README](../README.md) — Quick-start experience and feature overview
- [CHANGELOG](../CHANGELOG.md) — Release history
- [GitHub Repository](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.agesignals)
