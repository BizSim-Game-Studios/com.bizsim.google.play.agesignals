# API Reference

Last reviewed: 2026-04-16

Namespace: `BizSim.Google.Play.AgeSignals`

## AgeSignalsController

MonoBehaviour singleton. Entry point for all age signals operations.

| Member | Type | Description |
|--------|------|-------------|
| `Instance` | `AgeSignalsController` | Lazy singleton; creates a DontDestroyOnLoad GameObject |
| `CheckAgeSignals()` | `void` | Fire-and-forget check with event callbacks |
| `CheckAgeSignalsAsync(timeout)` | `Task<AgeRestrictionFlags>` | Async check returning derived flags |
| `CurrentFlags` | `AgeRestrictionFlags` | Most recent cached restriction flags (may be null) |
| `IsChecking` | `bool` | Whether a check is currently in progress |
| `OnRestrictionsUpdated` | `event Action<AgeRestrictionFlags>` | Fired when flags are updated |
| `OnError` | `event Action<AgeSignalsError>` | Fired on error |

## IAgeSignalsProvider

DI interface implemented by `AgeSignalsController`. Enables unit testing and DI injection.

| Member | Description |
|--------|-------------|
| `CheckAgeSignals()` | Fire-and-forget age check |
| `CheckAgeSignalsAsync(timeout)` | Async age check |
| `CurrentFlags` | Current cached flags |
| `IsChecking` | In-progress indicator |
| `OnRestrictionsUpdated` | Flags update event |
| `OnError` | Error event |

## AgeRestrictionFlags

Derived behavior flags. These are the ONLY values persisted to PlayerPrefs. Contains zero
raw age data.

| Property | Type | Description |
|----------|------|-------------|
| `FullAccessGranted` | `bool` | User has unrestricted access |
| `AccessDenied` | `bool` | User is below minimum threshold |
| `PersonalizedAdsEnabled` | `bool` | Personalized ads are permitted |
| `GamblingEnabled` | `bool` | Gambling features are permitted |
| `MarketplaceEnabled` | `bool` | Marketplace features are permitted |
| `ChatEnabled` | `bool` | Chat features are permitted |

## AgeSignalsResult

Raw API response. IN-MEMORY ONLY -- never persisted to disk.

| Property | Type | Description |
|----------|------|-------------|
| `VerificationStatus` | `AgeVerificationStatus` | Verification state |
| `AgeRangeLower` | `int?` | Lower bound of age range (nullable) |
| `AgeRangeUpper` | `int?` | Upper bound of age range (nullable) |

## AgeVerificationStatus

Enum representing the verification state.

| Value | Description |
|-------|-------------|
| `Verified` | Age has been verified |
| `Supervised` | Account is supervised by a parent |
| `SupervisedApprovalPending` | Parental approval is pending |
| `SupervisedApprovalDenied` | Parental approval was denied |
| `Unknown` | Verification status is unknown |
| `NotApplicable` | Not applicable for this account |

## AgeSignalsError

Error details from a failed API call.

| Property | Type | Description |
|----------|------|-------------|
| `ErrorCode` | `int` | Numeric error code |
| `ErrorCodeName` | `string` | Human-readable error code name |
| `errorMessage` | `string` | Descriptive error message |
| `IsRetryable` | `bool` | Whether the error is transient |

## AgeSignalsDecisionLogic

Pluggable ScriptableObject for configuring per-feature age thresholds.

| Field | Type | Description |
|-------|------|-------------|
| `Features` | `AgeFeature[]` | List of feature definitions with age thresholds |

## IAgeSignalsCacheProvider

Cache interface. Custom implementations MUST persist only derived flags, never raw data.

| Method | Description |
|--------|-------------|
| `Load()` | Returns cached `AgeRestrictionFlags` or null if expired |
| `Save(flags)` | Persists derived flags |
| `Clear()` | Deletes cached flags |

Built-in providers: `EncryptedPlayerPrefsCacheProvider` (default), `PlayerPrefsCacheProvider`.

## AgeSignalsMockConfig

ScriptableObject for editor testing with presets for adult, child, teen, unknown, denied,
and error scenarios.

| Field | Type | Description |
|-------|------|-------------|
| `MockStatus` | `AgeVerificationStatus` | Verification status to simulate |
| `MockAgeRangeLower` | `int` | Simulated lower age bound |
| `MockAgeRangeUpper` | `int` | Simulated upper age bound |
| `SimulateError` | `bool` | Whether to simulate an error |
| `SimulatedErrorCode` | `int` | Error code to simulate |
