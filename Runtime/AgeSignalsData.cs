// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BizSim.Google.Play.AgeSignals
{
    /// <summary>
    /// Well-known feature keys used by the default <see cref="AgeSignalsDecisionLogic"/>.
    /// Use these constants instead of hardcoded strings to avoid typos and enable refactoring.
    /// <example>
    /// <code>
    /// if (flags.IsFeatureEnabled(AgeFeatureKeys.Gambling))
    ///     EnableCasino();
    /// </code>
    /// </example>
    /// </summary>
    public static class AgeFeatureKeys
    {
        /// <summary>Gambling / Casino features — requires 18+ (adult only).</summary>
        public const string Gambling = "gambling";

        /// <summary>Trading / Marketplace features — requires 16+.</summary>
        public const string Marketplace = "marketplace";

        /// <summary>Chat / Social features — requires 13+.</summary>
        public const string Chat = "chat";
    }

    /// <summary>
    /// Defines a single age-gated feature with its minimum age threshold.
    /// Used by <see cref="AgeSignalsDecisionLogic"/> to dynamically configure
    /// which features require which age.
    /// </summary>
    [Serializable]
    public class AgeFeature
    {
        [Tooltip("Unique key used in code (e.g., \"gambling\", \"marketplace\", \"chat\").")]
        public string key;

        [Tooltip("Human-readable label shown in Editor UI (e.g., \"Casino / Gambling\").")]
        public string label;

        [Tooltip("Minimum age required to access this feature.")]
        [Range(5, 25)]
        public int minAge;

        [Tooltip("If true, only Verified (18+) users can access this feature, regardless of age range.")]
        public bool requiresAdult;
    }

    /// <summary>
    /// A key-value pair representing a feature flag's enabled state.
    /// Used in <see cref="AgeRestrictionFlags.Features"/> for JSON-serializable storage.
    /// </summary>
    [Serializable]
    public class FeatureFlagEntry
    {
        public string key;
        public bool enabled;
    }

    /// <summary>
    /// Error codes returned by the Age Signals API or the bridge layer.
    /// Values -1 through -9 are defined by Google's SDK. Values -100 and below are internal.
    /// </summary>
    public enum AgeSignalsErrorCode
    {
        /// <summary>API not available on this device (-1). Retryable.</summary>
        ApiNotAvailable = -1,

        /// <summary>Play Store not found (-2). Retryable.</summary>
        PlayStoreNotFound = -2,

        /// <summary>Network error (-3). Retryable.</summary>
        NetworkError = -3,

        /// <summary>Google Play Services not found (-4). Retryable.</summary>
        PlayServicesNotFound = -4,

        /// <summary>Cannot bind to service (-5). Retryable.</summary>
        CannotBindToService = -5,

        /// <summary>Play Store version outdated (-6). Retryable.</summary>
        PlayStoreVersionOutdated = -6,

        /// <summary>Google Play Services version outdated (-7). Retryable.</summary>
        PlayServicesVersionOutdated = -7,

        /// <summary>Client transient error (-8). Retryable.</summary>
        ClientTransientError = -8,

        /// <summary>App not owned — user did not install via Play Store (-9). Not retryable.</summary>
        AppNotOwned = -9,

        /// <summary>SDK version outdated — update the Age Signals dependency (-10). Not retryable.</summary>
        SdkVersionOutdated = -10,

        /// <summary>Internal bridge error — JNI call failed, serialization error, etc. (-100).</summary>
        InternalError = -100
    }

    /// <summary>
    /// Access status returned by <c>requestAgeSignalsAccess</c> (Age Signals 0.0.4).
    /// Mirrors the native <c>AgeSignalsStatus</c> IntDef.
    /// </summary>
    public enum AgeSignalsAccessStatus
    {
        /// <summary>UNSPECIFIED (0) — no status; treated fail-closed.</summary>
        Unspecified,

        /// <summary>SHARED (1) — the user/parent shared age signals; age fields are populated.</summary>
        Shared,

        /// <summary>NOT_SHARED (2) — the user declined sharing. In jurisdiction, so no age data.</summary>
        NotShared,

        /// <summary>VERIFICATION_REQUIRED (3) — mandatory jurisdiction, age unknown, verification needed.</summary>
        VerificationRequired,

        /// <summary>A value this package version does not recognize (forward-compat, fail-closed).</summary>
        Unrecognized
    }

    /// <summary>
    /// Source/method of the age range (Age Signals 0.0.4 <c>AgeRangeSource</c>).
    /// Per Google docs — VERIFY tier meanings before shipping compliance logic:
    /// TIER_A = self-declared, TIER_B = guardian-managed (supervised),
    /// TIER_C = assessed (credit card / email / selfie / Gov-ID / Tax-ID),
    /// TIER_D = highest verification (Gov-ID + selfie, or Digital ID).
    /// </summary>
    public enum AgeRangeSourceTier
    {
        /// <summary>No source (null / not shared).</summary>
        None,

        /// <summary>UNSPECIFIED (0).</summary>
        Unspecified,

        /// <summary>TIER_A (1) — self-declared.</summary>
        TierA,

        /// <summary>TIER_B (2) — guardian-managed (supervised minor).</summary>
        TierB,

        /// <summary>TIER_C (3) — assessed age.</summary>
        TierC,

        /// <summary>TIER_D (4) — highest verification.</summary>
        TierD,

        /// <summary>A value this package version does not recognize (fail-closed).</summary>
        Unrecognized
    }

    /// <summary>
    /// Parental/guardian approval status for a significant change (Age Signals 0.0.4
    /// <c>SignificantChangeStatus</c>). Applies only to supervised accounts.
    /// </summary>
    public enum SignificantChangeStatus
    {
        /// <summary>No status (null — unsupervised, or supervised with no changes).</summary>
        None,

        /// <summary>UNSPECIFIED (0).</summary>
        Unspecified,

        /// <summary>APPROVED (1) — guardian approved the most recent change.</summary>
        Approved,

        /// <summary>PENDING (2) — awaiting approval; per Google, access should be blocked until approved.</summary>
        Pending,

        /// <summary>DECLINED (3) — guardian denied approval; access should be blocked.</summary>
        Declined,

        /// <summary>A value this package version does not recognize (fail-closed).</summary>
        Unrecognized
    }

    /// <summary>
    /// Parsed result from the Age Signals 0.0.4 API containing raw age data.
    ///
    /// <b>BREAKING (0.0.4):</b> the single <c>UserStatus</c> axis was removed and split into
    /// <see cref="AccessStatus"/> (was it shared), <see cref="AgeRangeSource"/> (verification tier),
    /// and <see cref="SignificantChangeStatus"/> (guardian approval). <c>MostRecentApprovalDate</c>
    /// is renamed <see cref="SignificantChangeApprovalDateMs"/>.
    ///
    /// <b>PRIVACY POLICY:</b> This object is kept <b>in memory only</b>.
    /// It must NEVER be persisted to <c>PlayerPrefs</c>, disk, or transmitted to analytics.
    /// Use <see cref="AgeRestrictionFlags"/> for persistent storage.
    ///
    /// <b>COMPLIANCE:</b> the derived helpers below encode Google's documented mapping but the tier
    /// semantics were NOT confirmed from verbatim primary source — every helper is marked VERIFY and
    /// defaults fail-closed. Confirm with Google docs + legal before relying on the gambling gate.
    /// </summary>
    [Serializable]
    public class AgeSignalsResult
    {
        public AgeSignalsAccessStatus AccessStatus;
        public AgeRangeSourceTier AgeRangeSource;
        public SignificantChangeStatus SignificantChangeStatus;
        public int AgeLower;        // -1 when null (age range lower bound)
        public int AgeUpper;        // -1 when null (age range upper bound; null = 18+ top band)
        public string InstallId;    // null if not supervised
        public long SignificantChangeApprovalDateMs; // Unix ms, 0 when null

        /// <summary>Whether age signals were actually shared and a source tier is present.</summary>
        public bool HasAgeData =>
            AccessStatus == AgeSignalsAccessStatus.Shared
            && AgeRangeSource != AgeRangeSourceTier.None
            && AgeRangeSource != AgeRangeSourceTier.Unspecified;

        /// <summary>
        /// VERIFY: confirmed adult (18+) via assessed/verified tiers (C or D). Self-declared
        /// (TIER_A) is deliberately NOT counted — the gambling gate requires verified adulthood.
        /// </summary>
        public bool IsVerifiedAdult =>
            AgeLower >= 18
            && (AgeRangeSource == AgeRangeSourceTier.TierC
                || AgeRangeSource == AgeRangeSourceTier.TierD);

        /// <summary>VERIFY: self-declared adult (TIER_A, 18+). NOT sufficient for adult-only features.</summary>
        public bool IsDeclaredAdult =>
            AgeLower >= 18 && AgeRangeSource == AgeRangeSourceTier.TierA;

        /// <summary>VERIFY: supervised minor (guardian-managed account, TIER_B).</summary>
        public bool IsSupervisedMinor => AgeRangeSource == AgeRangeSourceTier.TierB;

        /// <summary>VERIFY: guardian approval was denied → block access.</summary>
        public bool IsApprovalDenied =>
            IsSupervisedMinor && SignificantChangeStatus == SignificantChangeStatus.Declined;

        /// <summary>VERIFY: guardian approval pending → block until approved (new 0.0.4 behavior).</summary>
        public bool IsApprovalPending =>
            IsSupervisedMinor && SignificantChangeStatus == SignificantChangeStatus.Pending;

        /// <summary>User is in a mandatory jurisdiction with unknown age — verification needed.</summary>
        public bool NeedsVerification =>
            AccessStatus == AgeSignalsAccessStatus.VerificationRequired;

        /// <summary>
        /// The SDK returned a source/change/access value this package version does not recognize.
        /// Fail-closed forward-compatibility guard.
        /// </summary>
        public bool IsUnrecognized =>
            AgeRangeSource == AgeRangeSourceTier.Unrecognized
            || SignificantChangeStatus == SignificantChangeStatus.Unrecognized
            || AccessStatus == AgeSignalsAccessStatus.Unrecognized;

        /// <summary>Whether a concrete age range is present (<see cref="AgeUpper"/> is non-negative).</summary>
        public bool HasAgeRange => AgeUpper >= 0;

        /// <summary>
        /// Whether the user's entire age range falls below the given age.
        /// Returns false if no upper bound (null upper = 18+ top band) or no data.
        /// </summary>
        public bool IsUnder(int age)
        {
            if (AgeUpper < 0) return false; // No upper bound (top band) or no data
            return AgeUpper < age;
        }
    }

    /// <summary>
    /// Application behavior flags derived from age signals — contains NO raw age data.
    ///
    /// <b>POLICY COMPLIANT:</b> Safe to persist to <c>PlayerPrefs</c>.
    /// Contains only boolean feature flags and a decision timestamp.
    /// Previous session flags serve as fallback until a fresh API response arrives.
    ///
    /// This class is <c>partial</c> — you can extend it in your own project
    /// by creating another <c>partial class AgeRestrictionFlags</c> in the
    /// <c>BizSim.Google.Play.AgeSignals</c> namespace to add project-specific flags
    /// without modifying the package source.
    /// </summary>
    [Serializable]
    public partial class AgeRestrictionFlags
    {
        /// <summary>Full unrestricted access (user is 18+ or outside supported jurisdiction).</summary>
        public bool FullAccessGranted;

        /// <summary>Access denied (parental approval was explicitly rejected).</summary>
        public bool AccessDenied;

        /// <summary>Whether personalized advertising is allowed for this user.</summary>
        public bool PersonalizedAdsEnabled;

        /// <summary>True when user status is Unknown — consider prompting for verification.</summary>
        public bool NeedsVerification;

        /// <summary>ISO 8601 timestamp of when this decision was made.</summary>
        public string DecisionTimestamp;

        /// <summary>
        /// Hash of the <see cref="AgeSignalsDecisionLogic"/> configuration at decision time.
        /// When the config changes (e.g., age thresholds modified in a new app version),
        /// this hash will mismatch and the cache will be invalidated — preventing stale
        /// restriction flags from persisting beyond the update.
        /// </summary>
        public string ConfigHash;

        /// <summary>
        /// Package version that produced these flags (e.g., "0.1.0").
        /// When the SDK version changes after an upgrade, the cache is invalidated
        /// to ensure flags are recomputed with the new decision logic code.
        /// Mirrors the pattern used by the InstallReferrer package.
        /// </summary>
        public string SdkVersion;

        // --- Dynamic Feature Flags ---

        /// <summary>
        /// Dynamic list of feature flags. Each entry maps a feature key to its enabled state.
        /// Populated by <see cref="AgeSignalsDecisionLogic.ComputeFlags"/> or built-in defaults.
        /// Safe for JSON serialization via <see cref="JsonUtility"/>.
        /// </summary>
        public List<FeatureFlagEntry> Features = new();

        /// <summary>
        /// Returns whether the feature with the given key is enabled.
        /// Returns <c>false</c> if the key is not found in the <see cref="Features"/> list.
        /// </summary>
        public bool IsFeatureEnabled(string key)
        {
            if (Features == null) return false;
            for (int i = 0; i < Features.Count; i++)
            {
                if (Features[i].key == key)
                    return Features[i].enabled;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Only warn if Features has been populated — empty list means "not yet resolved"
            if (Features.Count > 0)
                UnityEngine.Debug.LogWarning(
                    $"[AgeSignals] IsFeatureEnabled(\"{key}\") — key not found in Features list. " +
                    "Check for typos or use AgeFeatureKeys constants.");
#endif
            return false;
        }

        /// <summary>
        /// Sets the enabled state for the given feature key.
        /// Creates a new entry if the key does not exist yet.
        /// </summary>
        public void SetFeature(string key, bool enabled)
        {
            Features ??= new List<FeatureFlagEntry>();
            for (int i = 0; i < Features.Count; i++)
            {
                if (Features[i].key == key)
                {
                    Features[i].enabled = enabled;
                    return;
                }
            }
            Features.Add(new FeatureFlagEntry { key = key, enabled = enabled });
        }
    }

    /// <summary>
    /// Error returned by the Age Signals API or the bridge layer.
    /// </summary>
    [Serializable]
    public class AgeSignalsError
    {
        // Field names MUST be camelCase to match the JSON keys sent by AgeSignalsBridge.java.
        // JsonUtility.FromJson is case-sensitive — PascalCase fields will silently fail to deserialize.
        public int errorCode;
        public string errorMessage;
        public bool isRetryable;

        /// <summary>
        /// Whether this error code represents a transient failure that can be retried.
        /// Matches the Java bridge logic: error codes -1 through -8 are transient.
        /// </summary>
        public static bool IsRetryableCode(int code) =>
            code >= (int)AgeSignalsErrorCode.ClientTransientError &&
            code <= (int)AgeSignalsErrorCode.ApiNotAvailable;

        /// <summary>Strongly-typed error code enum. Returns <see cref="AgeSignalsErrorCode.InternalError"/> for unrecognized values.</summary>
        public AgeSignalsErrorCode ErrorCodeEnum => errorCode switch
        {
            -1  => AgeSignalsErrorCode.ApiNotAvailable,
            -2  => AgeSignalsErrorCode.PlayStoreNotFound,
            -3  => AgeSignalsErrorCode.NetworkError,
            -4  => AgeSignalsErrorCode.PlayServicesNotFound,
            -5  => AgeSignalsErrorCode.CannotBindToService,
            -6  => AgeSignalsErrorCode.PlayStoreVersionOutdated,
            -7  => AgeSignalsErrorCode.PlayServicesVersionOutdated,
            -8  => AgeSignalsErrorCode.ClientTransientError,
            -9  => AgeSignalsErrorCode.AppNotOwned,
            -10 => AgeSignalsErrorCode.SdkVersionOutdated,
            -100 => AgeSignalsErrorCode.InternalError,
            _   => AgeSignalsErrorCode.InternalError
        };

        /// <summary>Human-readable error code name for logging and debugging.</summary>
        public string ErrorCodeName => errorCode switch
        {
            -1  => nameof(AgeSignalsErrorCode.ApiNotAvailable),
            -2  => nameof(AgeSignalsErrorCode.PlayStoreNotFound),
            -3  => nameof(AgeSignalsErrorCode.NetworkError),
            -4  => nameof(AgeSignalsErrorCode.PlayServicesNotFound),
            -5  => nameof(AgeSignalsErrorCode.CannotBindToService),
            -6  => nameof(AgeSignalsErrorCode.PlayStoreVersionOutdated),
            -7  => nameof(AgeSignalsErrorCode.PlayServicesVersionOutdated),
            -8  => nameof(AgeSignalsErrorCode.ClientTransientError),
            -9  => nameof(AgeSignalsErrorCode.AppNotOwned),
            -10 => nameof(AgeSignalsErrorCode.SdkVersionOutdated),
            -100 => nameof(AgeSignalsErrorCode.InternalError),
            _   => $"Unknown({errorCode})"
        };
    }

    /// <summary>
    /// Exception thrown by <see cref="IAgeSignalsProvider.CheckAgeSignalsAsync"/>
    /// when the age signals check fails after all retries.
    /// Wraps the underlying <see cref="AgeSignalsError"/> for structured error handling.
    /// </summary>
    public class AgeSignalsException : Exception
    {
        /// <summary>The underlying error from the API or bridge layer.</summary>
        public AgeSignalsError Error { get; }

        public AgeSignalsException(AgeSignalsError error)
            : base($"Age Signals check failed: {error.ErrorCodeName} ({error.errorCode}) — {error.errorMessage}")
        {
            Error = error;
        }
    }
}
