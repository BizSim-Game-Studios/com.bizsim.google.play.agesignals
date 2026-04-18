# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-04-18

### Added
- **EDM4U declared as `package.json` dependency** (`com.google.external-dependency-manager: 1.2.187`) per `unity-package-standards.md` §"EDM4U as a declared `package.json` dependency". Consumers who add the OpenUPM scoped registry to their host project's `Packages/manifest.json` (one-time setup) will have Unity Package Manager auto-install EDM4U transitively. Installation section of README updated with the manifest snippet.

### Fixed
- Missing `.meta` files for `AgeSignalsEditorInit`, `BizSimLoggerRedactionTest`, `PackageVersionSchemaTest`, and `PredictiveBackManifestTest` added. Prior commits landed source files without their sibling `.meta` companions.

## [1.3.0] - 2026-04-17

### Added
- **GDPR Article 17 Forget API (Wave 2).** New `AgeSignalsController.ForgetAll()` method performs a complete right-to-erasure: clears the encrypted cache payload via the active `IAgeSignalsCacheProvider`, erases the per-install encryption key identifier when the shipped `EncryptedPlayerPrefsCacheProvider` is in use, and nulls the in-memory `CurrentFlags`. After this call, the package behaves as if freshly installed — any subsequent `Save` generates a fresh key id, so data encrypted with the old key becomes permanently unrecoverable. Distinct from the existing `ClearCachedData()` (session-level clear; preserves the key id so future saves encrypt with the same key).
- **`EncryptedPlayerPrefsCacheProvider.EraseAll()`** public method — the engine behind `ForgetAll`. Deletes both the payload key (`AgeSignals_Cache_Enc`) AND the per-install key id (`AgeSignals_KeyId`) and invalidates the in-memory derived AES key. Consumers may call directly when using a custom invocation path (e.g., a dedicated "Delete my data" button).
- `ForgetApiTest` drift guard (4 assertions): `EraseAll` deletes both keys, `Clear` preserves the key id, `EraseAll` is idempotent, post-erasure `Save` generates a fresh key id that differs from the erased one (prevents old-key decryption).

## [1.2.0] - 2026-04-17

### Added
- **C5.2 compliance (Plan E).** `Runtime/Plugins/Android/BizSimAgeSignals.androidlib/AndroidManifest.xml` now explicitly declares `android:enableOnBackInvokedCallback="false"`. This package has no UI — opt-out declared explicitly so consumer apps merging manifests see the intent (they can override at the app level if their UX requires different behavior). MINOR bump because the explicit intent declaration could subtly alter consumer manifest-merge behavior compared to v1.1.0's bare-stub manifest. Added `PredictiveBackManifestTest` drift guard. See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/05-predictive-back-audit.md`.

## [1.1.0] - 2026-04-17

### Added
- **K8 PackageVersion schema unification (Plan G).** Three new `public const string` fields on `PackageVersion`: `NativeSdkVersion` (`"0.0.3"`), `NativeSdkLabel` (`"Play Core (age-signals beta)"`), `NativeSdkArtifactCoord` (`"com.google.android.play:age-signals:0.0.3"`). First introduction of native SDK version metadata for this package — prior versions exposed no such field. Dashboard now correctly shows the age-signals artifact coord and label instead of leaving the SDK version empty. See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/06-package-version-schema.md`.
- `PackageVersionSchemaTest` drift guard.

## [1.0.4] - 2026-04-17

### Fixed
- **Privacy: redact age numerics from Info+ log levels (C2.7 compliance).** Three `BizSimLogger.Info` calls in `AgeSignalsController.cs` (lines 496, 530, 584) emitted raw age range values like `age=[13-17]` which are captured by Android Logcat and Firebase Crashlytics breadcrumbs even though they never traverse the analytics adapter. Split each call into status-only `Info` + age-numerics `Verbose` (release-build-silent by default). Added `BizSimLoggerRedactionTest` drift guard asserting no `Info`/`Warning`/`Error` call contains age regex patterns. `Documentation~/DATA_SAFETY.md` updated with the redaction policy.

## [1.0.3] - 2026-04-16

### Added
- `ReleaseDate` field in PackageVersion.cs for dashboard version display
- `[InitializeOnLoad]` EditorInit registering `BIZSIM_AGESIGNALS_INSTALLED` define

## [1.0.2] - 2026-04-15

### Fixed
- Relaxed runtime asmdef `includePlatforms` from `["Android", "Editor"]` to `[]`
  to fix a consumer-side `CS0246: The type or namespace name 'BizSim' could not
  be found` regression that appeared during Addressables content build on Android
  target. The Editor compile pass resolved the auto-reference correctly, but the
  Player script compile pass did not — a known Unity issue when `autoReferenced`
  library assemblies are platform-gated at the asmdef level.

  Runtime platform safety is preserved by the existing `#if UNITY_ANDROID && !UNITY_EDITOR`
  guards around every JNI call site; non-Android builds continue to route through
  `Mock<Api>Provider` per CROSS-PACKAGE-INVARIANTS §4.

  No API surface change. Consumers with existing `using BizSim.Google.Play.AgeSignals;`
  imports require no action — the fix is transparent on the next package install.

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
