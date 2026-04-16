# Troubleshooting

Last reviewed: 2026-04-16

## 1. API returns Unknown verification status on all test devices

**Problem:** `AgeSignalsResult.VerificationStatus` is always `Unknown` regardless of the test account.

**Cause:** The Google Play Age Signals API is in beta and may not be available on all devices or regions. The API requires Google Play Services and an active Google account with age verification enabled.

**Fix:** Ensure the test device has Google Play Services installed and updated. Use a Google account that has completed age verification. Check the [upstream API status](https://developer.android.com/games/agesignals) for current availability. In beta, coverage is limited.

## 2. Cached flags are stale or expired

**Problem:** `CurrentFlags` returns null or outdated flags.

**Cause:** The encrypted cache has a 24-hour TTL. After expiry, cached flags are automatically cleared.

**Fix:** Call `CheckAgeSignals()` or `CheckAgeSignalsAsync()` on every app launch to refresh flags. The cache is a performance optimization for mid-session access, not a long-term store.

## 3. EncryptedPlayerPrefsCacheProvider throws on first access

**Problem:** An exception occurs when the encrypted cache provider first attempts to read or write.

**Cause:** The per-device encryption key may not be initialized, or PlayerPrefs may be corrupted.

**Fix:** The provider handles initialization automatically. If corruption is suspected, call `AgeSignalsController.Instance.ClearCache()` to wipe the cached flags and re-check on next launch.

## 4. Decision logic does not match expected feature gates

**Problem:** `AgeRestrictionFlags.GamblingEnabled` is true for a user who should be restricted.

**Cause:** The `AgeSignalsDecisionLogic` ScriptableObject may have incorrect age thresholds, or the raw age range from the API is wider than expected.

**Fix:** Review the `AgeSignalsDecisionLogic` asset. Verify each feature's `MinAge` and `RequiresAdult` settings. Use the debug menu (`AgeSignalsDebugMenu`) to inspect the raw result in a development build.

## 5. EDM4U fails to resolve com.google.android.play:age-signals

**Problem:** Android build fails with a missing dependency error for the age-signals SDK.

**Cause:** EDM4U has not resolved the Maven dependency. The beta SDK may have a different artifact path.

**Fix:** Run **Assets > External Dependency Manager > Android Resolver > Force Resolve**. If the error persists, verify the Maven coordinates in `Editor/Dependencies.xml` match the current beta release.

## 6. Gradle version conflicts with other Google Play libraries

**Problem:** Duplicate class errors or version conflicts when building alongside other `com.google.android.play:*` dependencies.

**Cause:** Other Google Play libraries (Play Integrity, Play Review, etc.) may pull different versions of shared transitive dependencies.

**Fix:** Run **Assets > External Dependency Manager > Android Resolver > Force Resolve** to let EDM4U resolve version conflicts. If conflicts persist, add explicit version overrides in your project-level `Dependencies.xml`. See the README's "Gradle Force Resolve" section.

## 7. Mock provider does not return expected flags in Editor

**Problem:** In Editor play mode, the mock provider returns default flags instead of the configured scenario.

**Cause:** No `AgeSignalsMockConfig` ScriptableObject is assigned to the controller.

**Fix:** Create a mock config via **Assets > Create > BizSim > Age Signals Mock Config**. Configure the desired scenario (Adult, Child, Teen, etc.) and assign it to `AgeSignalsController.MockConfig` in the Inspector. Six presets are available in `Samples~/MockPresets`.
