# Configuration

Last reviewed: 2026-04-16

## AgeSignalsSettings asset

The project-wide defaults are stored in a ScriptableObject. The controller reads settings
at `Awake()` and uses them as defaults for logging, mock behavior, and analytics.

### Settings fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master switch for all `[BizSim.AgeSignals]` log output |
| `LogLevel` | `LogLevel` | `Info` | Minimum severity: Verbose, Info, Warning, Error, Silent |
| `UseMockInDevelopmentBuild` | `bool` | `false` | When true, Development Builds use the mock provider |
| `EnableAnalyticsByDefault` | `bool` | `false` | Auto-registers the analytics adapter at startup |
| `CacheTtlHours` | `int` | `24` | Hours before cached restriction flags expire |
| `MaxRetries` | `int` | `3` | Maximum retry attempts for transient errors |

## AgeSignalsController Inspector

The `AgeSignalsController` MonoBehaviour exposes:

- **MockConfig** — assign an `AgeSignalsMockConfig` ScriptableObject for editor testing
- **Cache Provider** — defaults to `EncryptedPlayerPrefsCacheProvider`; can be swapped at runtime

### Per-instance overrides

When a `[SerializeField]` field on the controller has a non-default value, it overrides the
Settings asset value for that instance. The Settings asset provides defaults; the
MonoBehaviour fields still win when explicitly set.

## Editor Configuration window

Open via the BizSim menu if available. The agesignals package predates the standardized
Configuration window pattern used by Review and AppUpdate, but provides equivalent
functionality through the Inspector.

## Decision Logic

The `AgeSignalsDecisionLogic` ScriptableObject configures per-feature age thresholds:

1. Create via **Assets > Create > BizSim > Age Signals Decision Logic**
2. Add features with keys from `AgeFeatureKeys` (e.g., `Gambling`, `Marketplace`, `Chat`)
3. Set `MinAge` and `RequiresAdult` per feature
4. Assign to the controller

The decision engine evaluates raw age data (in memory) against these thresholds and produces
the `AgeRestrictionFlags` that get cached.

## Mock Config presets

Six preset mock configs are available via `Samples~/MockPresets`:

| Preset | Simulated scenario |
|--------|-------------------|
| Adult | Full access, verified adult |
| Child | Access denied, supervised child |
| Teen | Partial access, no personalized ads |
| Unknown | Unknown verification status |
| Denied | Parental approval denied |
| Error | API error with retryable flag |

## Privacy-sensitive configuration

When configuring analytics:

- The analytics adapter MUST only log technical success/failure events
- No age data, verification status, or restriction flags may appear in analytics events
- Custom `IAgeSignalsAnalyticsAdapter` implementations must follow these constraints
- The `BIZSIM_FIREBASE` define gates the optional Firebase integration
