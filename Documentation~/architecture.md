# Architecture

Last reviewed: 2026-04-16

## Overview

The Age Signals package follows the canonical BizSim Google Play bridge pattern with
additional privacy architecture layered on top. A Java bridge on the Android side, a C#
provider abstraction on the Unity side, a decision engine that transforms raw data into
safe behavior flags, and an encrypted cache for the derived flags only.

## Component diagram

```
AgeSignalsController (MonoBehaviour singleton)
    |
    +-- IAgeSignalsProvider (compile-time selection)
    |       |
    |       +-- Android provider (#if UNITY_ANDROID && !UNITY_EDITOR)
    |       |       |
    |       |       +-- AgeSignalsBridge.java (JNI entry point)
    |       |               |
    |       |               +-- AgeSignals SDK (Google Play)
    |       |
    |       +-- Mock provider (Editor + non-Android)
    |               |
    |               +-- AgeSignalsMockConfig (ScriptableObject)
    |
    +-- AgeSignalsDecisionLogic (raw data -> flags)
    |       |
    |       +-- AgeSignalsResult (IN-MEMORY ONLY)
    |       +-- AgeRestrictionFlags (OUTPUT -> cache)
    |
    +-- IAgeSignalsCacheProvider
    |       |
    |       +-- EncryptedPlayerPrefsCacheProvider (default)
    |       +-- PlayerPrefsCacheProvider (alternative)
    |
    +-- IAgeSignalsAnalyticsAdapter (optional, NO age data)
```

## Privacy architecture

The most critical architectural constraint is the data flow split:

```
[Google Play SDK] -> AgeSignalsResult (raw age data)
                        |
                        | IN-MEMORY ONLY (never touches disk)
                        v
                  AgeSignalsDecisionLogic
                        |
                        | Transforms to behavior flags
                        v
                  AgeRestrictionFlags (zero age data)
                        |
                        | SAFE TO PERSIST
                        v
              EncryptedPlayerPrefsCacheProvider
                        |
                        +-- 24-hour TTL auto-expiry
```

Raw age data (`AgeSignalsResult`) is a transient in-memory object. It is consumed by the
decision logic and then discarded. No reference to the raw data survives beyond the
decision evaluation call. The GC reclaims it on the next collection cycle.

Only the derived `AgeRestrictionFlags` (boolean feature flags like "can show personalized
ads") are persisted. These flags contain zero raw age data and auto-expire after 24 hours.

## Thread model

All public methods on `AgeSignalsController` enforce main-thread execution. The Java bridge
posts SDK calls to the main Handler. Callbacks use `UnityMainThreadDispatcher.Enqueue()` to
marshal back to Unity's main thread.

## Provider selection

Provider selection happens at compile time:

- `#if UNITY_ANDROID && !UNITY_EDITOR` selects the Android provider
- All other configurations select the mock provider

## Cache providers

Two built-in cache providers:

- **`EncryptedPlayerPrefsCacheProvider`** (default) — encrypts flag values before storing
  in PlayerPrefs. Uses a per-device key.
- **`PlayerPrefsCacheProvider`** — plain PlayerPrefs storage. Suitable for development
  and testing only.

Custom providers must implement `IAgeSignalsCacheProvider` and MUST persist only derived
flags, never raw age data. This constraint is non-negotiable.

## Retry logic

The controller retries transient errors (network timeouts, temporary service unavailability)
up to `MaxRetries` times with exponential backoff. Non-retryable errors (invalid request,
API not available) are surfaced immediately via `OnError`.

## Data flow

1. Consumer calls `AgeSignalsController.Instance.CheckAgeSignalsAsync()`
2. Provider calls the native Age Signals SDK via JNI (or returns mock data)
3. Raw `AgeSignalsResult` is created in memory
4. Decision logic transforms raw data into `AgeRestrictionFlags`
5. Raw data is discarded (no reference retained)
6. Flags are saved to the encrypted cache with a 24-hour TTL
7. Analytics adapter is notified with success/error only (no age data)
8. `OnRestrictionsUpdated` fires with the derived flags
