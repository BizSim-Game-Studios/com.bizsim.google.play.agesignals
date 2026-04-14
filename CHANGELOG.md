# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-04-14

### Fixed

- Corrected the Firebase integration define name in `Documentation~/PRIVACY_MANIFEST.xml`, `Documentation~/DATA_SAFETY.md`, and `SECURITY.md` from `AGESIGNALS_FIREBASE` to `BIZSIM_FIREBASE`. The asmdef's `versionDefines` entry has always used `BIZSIM_FIREBASE` (the workspace standard); users following the old docs would have wired up a define that the package never checks, silently disabling the Firebase analytics code path. Consumers adding Firebase integration must use `BIZSIM_FIREBASE` in their Scripting Define Symbols.


## [1.0.0] - 2026-04-14

### Added

- Initial release of `com.bizsim.google.play.agesignals` — Unity bridge for the Google Play Age Signals API.
- Java JNI bridge with `.androidlib` subproject and ProGuard keep rules.
- C# singleton controller `AgeSignalsController` with async API surface.
- Mock provider `AgeSignalsMockConfig` for editor testing without a device.
- Optional Firebase Analytics integration via `BIZSIM_FIREBASE` versionDefine.
- `Samples~/BasicIntegration` minimal usage example.
- `Samples~/MockPresets` pre-configured mock scenarios.
- Tests under `Tests/Editor/` and `Tests/Runtime/`.

### Notes

- This is the first release under the new `com.bizsim.google.play.*` family naming. The previous incarnation (`com.bizsim.gplay.agesignals`) at version 0.1.8 is archived and no longer maintained.
- Floor: Unity 6.0 LTS (`6000.0`).
