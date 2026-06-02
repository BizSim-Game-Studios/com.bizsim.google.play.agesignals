# BizSim Google Play Age Signals Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.4.1-orange.svg)](CHANGELOG.md)

Unity bridge for the [Google Play Age Signals API](https://developer.android.com/google/play/age-signals) (v0.0.3).
Queries age verification status via JNI, derives behavior restriction flags, and exposes a singleton controller with async callbacks, retry logic, and an editor mock provider.

> **⚠️ Unofficial package.** This is a community-built Unity bridge for the Google Play Age Signals API. It is **not** an official Google product. The underlying Age Signals SDK (v0.0.3) is currently in **beta** — the API surface may change without notice. Use in production at your own risk.

## Features

- **Java-to-C# Bridge** — Play Core `AgeSignalsManager` wrapped in a main-thread-safe singleton
- **Event-based and async APIs** — `CheckAgeSignals()` (events) + `CheckAgeSignalsAsync()` returning `AgeRestrictionFlags`
- **Decision engine** — Pluggable `AgeSignalsDecisionLogic` ScriptableObject with configurable feature list and age thresholds
- **Encrypted flag cache** — Derived restriction flags cached in encrypted `PlayerPrefs`, auto-expire after 24 hours; raw age data never persisted
- **Mock provider** — 14 ScriptableObject presets covering adult/child/teen, supervised/approved/denied, and error scenarios
- **Analytics adapter** — Optional `IAgeSignalsAnalyticsAdapter` with a Firebase implementation guarded by `BIZSIM_FIREBASE`
- **UniTask support** — Optional extension assembly guarded by `BIZSIM_UNITASK`
- **Editor integration** — Auto-registered `BIZSIM_AGESIGNALS_INSTALLED` define via `editor.core`

## Installation

This package depends on Google's [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver), which is published to the OpenUPM scoped registry. Add EDM4U's registry to your project's `Packages/manifest.json` once, then add this package as a Git URL — UPM will auto-install EDM4U on first import.

**Step 1 — Add the OpenUPM scoped registry (one-time per project):**

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

If you already have other OpenUPM-distributed packages, you may already have this registry — just add `com.google.external-dependency-manager` to the existing `scopes` array.

**Step 2 — Install this package via Git URL:**

```json
{
  "dependencies": {
    "com.bizsim.google.play.agesignals": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.agesignals.git#v1.4.1"
  }
}
```

After the package imports, EDM4U is automatically resolved by UPM — no manual `.unitypackage` import required. EDM4U then resolves the Android Maven dependency declared in `Editor/Dependencies.xml` (`com.google.android.play:age-signals:0.0.3`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

## Quick Start

1. Add `AgeSignalsController` to a persistent GameObject (or access the `Instance` singleton).

2. Subscribe and query on launch:
   ```csharp
   using BizSim.Google.Play.AgeSignals;

   public class AgeGate : MonoBehaviour
   {
       void Start()
       {
           AgeSignalsController.Instance.OnRestrictionsUpdated += OnRestrictions;
           AgeSignalsController.Instance.OnError += OnError;
           AgeSignalsController.Instance.CheckAgeSignals();
       }

       void OnRestrictions(AgeRestrictionFlags flags)
       {
           if (flags.FullAccessGranted)
               EnableAllFeatures();
           else if (flags.AccessDenied)
               ShowRestrictedScreen();
           else if (!flags.PersonalizedAdsEnabled)
               SwitchToContextualAds();
       }

       void OnError(AgeSignalsError error)
       {
           // Controller retries automatically up to 3 times for transient errors.
           Debug.LogWarning($"Age Signals error: {error.ErrorCodeName} — {error.errorMessage}");
       }

       void OnDestroy()
       {
           if (AgeSignalsController.Instance != null)
           {
               AgeSignalsController.Instance.OnRestrictionsUpdated -= OnRestrictions;
               AgeSignalsController.Instance.OnError -= OnError;
           }
       }
   }
   ```

3. Read cached flags at any time after the first `CheckAgeSignals()` completes:
   ```csharp
   var flags = AgeSignalsController.Instance.CurrentFlags;
   if (flags != null && !flags.PersonalizedAdsEnabled)
       ShowContextualAdsOnly();
   ```

4. Use the async API for structured error handling:
   ```csharp
   try
   {
       var flags = await AgeSignalsController.Instance.CheckAgeSignalsAsync();
       if (!flags.PersonalizedAdsEnabled) SwitchToContextualAds();
   }
   catch (AgeSignalsException ex)
   {
       Debug.LogWarning($"Age check failed: {ex.Error.ErrorCodeName}");
       // Fallback: CurrentFlags still holds the last cached result.
   }
   ```

5. The controller implements `IAgeSignalsProvider` — use it with any DI framework (Zenject, VContainer) or replace with a `MockAgeProvider` in unit tests without touching the singleton.

6. For editor testing, create a mock config via **Assets → Create → BizSim → Age Signals Mock Config**, set the desired `MockStatus`, assign it to the controller's `MockConfig` field, then enter Play Mode.

Key types:

| Type | Description |
|------|-------------|
| `IAgeSignalsProvider` | DI interface implemented by `AgeSignalsController` |
| `AgeSignalsController` | Singleton MonoBehaviour — `CheckAgeSignals()` / `CheckAgeSignalsAsync()` |
| `AgeVerificationStatus` | Enum: `Verified`, `Supervised`, `SupervisedApprovalPending`, `SupervisedApprovalDenied`, `Unknown`, `NotApplicable` |
| `AgeSignalsResult` | Raw API response — in-memory only, never persisted |
| `AgeRestrictionFlags` | Behavior flags derived from result — encrypted `PlayerPrefs` cache |
| `AgeSignalsDecisionLogic` | Pluggable ScriptableObject with feature list and age thresholds |
| `AgeSignalsError` | Error details: `errorCode`, `errorMessage`, `isRetryable` |
| `AgeSignalsMockConfig` | ScriptableObject for editor testing |

## Privacy and Compliance

This package enforces a strict three-layer privacy model:

- **Raw age data** (`AgeSignalsResult`) exists **in memory only** — it is never written to disk, logs, or network in any form.
- **Derived behavior flags** (`AgeRestrictionFlags`) are the only persisted state. They are written to an encrypted `PlayerPrefs` key and contain no age value — only boolean capability flags (e.g. `PersonalizedAdsEnabled`, `FullAccessGranted`).
- **Analytics events** report only technical success/failure signals — zero age or demographic information is transmitted.

This split means a data breach of `PlayerPrefs` reveals nothing about the user's age. The `IAgeSignalsCacheProvider` interface lets you substitute your own storage backend; any custom implementation must preserve the same raw/derived split.

The `BIZSIM_FIREBASE` scripting define enables the Firebase Analytics adapter. When not defined, no analytics code is compiled in.

## Requirements

- Unity 6000.0 or later
- Android target platform
- **[EDM4U](https://github.com/googlesamples/unity-jar-resolver) (External Dependency Manager for Unity)** — auto-resolved via OpenUPM scoped registry (see Installation)
- Google Play Age Signals library 0.0.3 — **Beta SDK** (resolved automatically via `Editor/Dependencies.xml`)

> **⚠️ Beta SDK risks:** `com.google.android.play:age-signals:0.0.3` carries no long-term support guarantee. Breaking API changes, Gradle version conflicts with other Play libraries, and limited device/region rollout are known risks. Pin the version in `Dependencies.xml`, run **Force Resolve** after any Google Play dependency change, and monitor [Google Play Age Signals release notes](https://developer.android.com/google/play/age-signals) for status changes. The controller gracefully falls back to cached flags when the API is unavailable on a device.

## Google Play Data Safety

### Data Collected

This package stores one item on-device: a set of derived boolean behavior flags (`AgeRestrictionFlags`) written to encrypted `PlayerPrefs`. These flags indicate capability grants (e.g. whether personalized ads are enabled) and **do not contain any age value, date of birth, or demographic data**. They are derived locally on-device from the Age Signals API result and are never transmitted off the device by this package.

The raw age signal result (`AgeSignalsResult`) from the Google Play API is held in memory for the duration of the decision-logic call only and is discarded immediately after the flags are derived.

### Data NOT Collected or Shared

- **No age or demographic data** is persisted or transmitted by this package
- **No personal data** is collected
- **No data is shared** with third parties
- **No network calls** are made by this package (Play Core handles all IPC to Google Play)
- **Analytics events**, if enabled via `BIZSIM_FIREBASE`, carry only binary success/error codes — no age information

### Play Console Data Safety Form

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469) in Google Play Console:

1. **Data types collected**: None collected by this package (the encrypted `PlayerPrefs` entry is local on-device storage, not "data collection" under Play's definition)
2. **Data shared with third parties**: No
3. **Data encrypted in transit**: N/A — no data leaves the device
4. **Users can request deletion**: The encrypted flags can be cleared programmatically via `AgeSignalsController.Instance.ClearCache()` (or the GDPR `ForgetAll()` API)

For detailed data safety declarations, see [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md).

## License

This package's C# and Java source code is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

## Third-Party Licenses

This package does **not** bundle any Google SDK binaries. The native Android dependency is resolved at build time by [EDM4U](https://github.com/googlesamples/unity-jar-resolver) from the Google Maven repository (`maven.google.com`):

| Dependency | Version | License |
|-----------|---------|---------|
| `com.google.android.play:age-signals` | 0.0.3 (Beta) | [Play Core SDK Terms of Service](https://developer.android.com/guide/playcore/license) |

By installing and using this package, you agree to the [Play Core SDK Terms of Service](https://developer.android.com/guide/playcore/license) and the [Google APIs Terms of Service](https://developers.google.com/terms).

For full third-party license details, see [NOTICES.md](NOTICES.md).
