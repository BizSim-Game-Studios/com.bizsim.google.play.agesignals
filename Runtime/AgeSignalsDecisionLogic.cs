// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System.Collections.Generic;
using UnityEngine;

namespace BizSim.Google.Play.AgeSignals
{
    /// <summary>
    /// Pluggable decision logic for converting raw <see cref="AgeSignalsResult"/>
    /// into <see cref="AgeRestrictionFlags"/>.
    ///
    /// Features are configurable from the Inspector via a dynamic list.
    /// Subclass and override <see cref="ComputeFlags"/> for fully custom logic.
    /// </summary>
    [CreateAssetMenu(menuName = "BizSim/Age Signals/Age Signals Decision Logic")]
    public class AgeSignalsDecisionLogic : ScriptableObject
    {
        // Default thresholds are aligned with Google Play Family Policy and COPPA:
        //   • 18+ (requiresAdult) — Real-money gambling, casino, loot boxes with real value.
        //     Required by Google Play Developer Program Policy §Gambling (2024-11) and
        //     most jurisdictions' gambling regulations.
        //   • 16+ — User-to-user trading / marketplace. Aligned with EU Digital Services Act
        //     Art. 28(2) which restricts profiling of minors under 16 for commercial purposes.
        //   • 13+ — Chat / social features. Matches COPPA (16 CFR §312) "actual knowledge"
        //     threshold and Google Play Families Self-Certified Ads SDK Requirements.
        //   • Personalized Ads = 13+ — COPPA safe harbor; users under 13 receive
        //     non-personalized ads only (Google AdMob / IronSource COPPA tag).

        [Header("Feature Definitions")]
        [Tooltip("List of age-gated features. Each defines a key, label, minimum age, and whether adult verification is required.")]
        [SerializeField] private List<AgeFeature> _features = new()
        {
            new AgeFeature { key = "gambling", label = "Gambling / Casino", minAge = 18, requiresAdult = true },
            new AgeFeature { key = "marketplace", label = "Trading / Marketplace", minAge = 16, requiresAdult = false },
            new AgeFeature { key = "chat", label = "Chat / Social", minAge = 13, requiresAdult = false }
        };

        [Header("Ads Threshold")]
        [Tooltip("Minimum age for personalized advertising (COPPA compliance = 13).")]
        [Range(5, 25)]
        [SerializeField] private int _personalizedAdsMinAge = 13;

        /// <summary>Read-only access to the configured features list (for Editor UI).</summary>
        public IReadOnlyList<AgeFeature> Features => _features;

        /// <summary>Configured threshold for personalized ads.</summary>
        public int PersonalizedAdsMinAge => _personalizedAdsMinAge;

        /// <summary>
        /// Resets features to the default set aligned with Google Play and COPPA policies:
        /// gambling 18+ (requiresAdult), marketplace 16+ (EU DSA), chat 13+ (COPPA),
        /// and personalized ads at 13+.
        /// Called by Unity on asset creation and useful for factory reset scenarios.
        /// </summary>
        public void Reset()
        {
            _features = new List<AgeFeature>
            {
                new() { key = "gambling", label = "Gambling / Casino", minAge = 18, requiresAdult = true },
                new() { key = "marketplace", label = "Trading / Marketplace", minAge = 16, requiresAdult = false },
                new() { key = "chat", label = "Chat / Social", minAge = 13, requiresAdult = false }
            };
            _personalizedAdsMinAge = 13;
        }

        /// <summary>
        /// Populates <paramref name="flags"/> based on the raw <paramref name="result"/>
        /// using the configured feature list and age thresholds.
        /// Override in a subclass for fully custom logic.
        /// </summary>
        public virtual void ComputeFlags(AgeSignalsResult result, AgeRestrictionFlags flags)
        {
            // ================================================================================
            // COMPLIANCE — VERIFY BEFORE SHIPPING (Age Signals 0.0.4)
            // The 0.0.4 model splits the old single userStatus into three axes: access
            // (shared/not-shared/verification-required), source tier (A/B/C/D), and guardian
            // approval (approved/pending/declined). The tier→adult mapping below follows
            // Google's documented pattern (verified adult = TIER_C/D + 18+) but was NOT confirmed
            // from verbatim primary source and is NOT legally reviewed. It defaults FAIL-CLOSED.
            //
            // OPEN COMPLIANCE QUESTION (do NOT ship without resolving): out-of-jurisdiction users
            // (most of the world) no longer reach here as "no data = full access". In 0.0.4 they
            // surface as the API_NOT_AVAILABLE error, handled by the controller's error path — NOT
            // this method. Here, any in-jurisdiction no-data state (NOT_SHARED / VERIFICATION_REQUIRED
            // / UNSPECIFIED) FAILS CLOSED. Confirm the controller's error fallback grants full access
            // for genuine out-of-jurisdiction before release, or non-jurisdiction users get restricted.
            // ================================================================================

            // Guardian approval denied → block everything (highest priority).
            if (result.IsApprovalDenied)
            {
                flags.AccessDenied = true;
                flags.FullAccessGranted = false;
                flags.PersonalizedAdsEnabled = false;
                flags.NeedsVerification = false;
                foreach (var feature in _features)
                    flags.SetFeature(feature.key, false);
                return;
            }

            flags.AccessDenied = false;

            // Fail-closed guards: unrecognized SDK values, guardian approval PENDING (new 0.0.4
            // "block until approved" behavior), mandatory-jurisdiction verification required, or any
            // in-jurisdiction state where age was not actually shared. Restrict + request verification.
            bool failClosed =
                result.IsUnrecognized
                || result.IsApprovalPending
                || result.NeedsVerification
                || !result.HasAgeData;

            if (failClosed)
            {
                flags.FullAccessGranted = false;
                flags.PersonalizedAdsEnabled = false;
                flags.NeedsVerification = true;
                foreach (var feature in _features)
                    flags.SetFeature(feature.key, false);
                return;
            }

            // From here: age signals WERE shared with a concrete tier. Decide from age + tier.
            // VERIFY: "verified adult" = assessed/verified tiers (C/D) at 18+. Self-declared adult
            // (TIER_A) is intentionally NOT full access — it does not unlock adult-only features.
            bool verifiedAdult = result.IsVerifiedAdult;

            flags.FullAccessGranted = verifiedAdult;

            foreach (var feature in _features)
            {
                bool enabled;
                if (feature.requiresAdult)
                    enabled = verifiedAdult;                       // e.g. gambling: TIER_C/D + 18+ only
                else
                    enabled = verifiedAdult || !result.IsUnder(feature.minAge);

                flags.SetFeature(feature.key, enabled);
            }

            flags.PersonalizedAdsEnabled = !result.IsUnder(_personalizedAdsMinAge);
            // Self-declared adults still get a verification nudge (not verified for adult features).
            flags.NeedsVerification = result.IsDeclaredAdult;
        }

        /// <summary>
        /// Computes a deterministic hash string from the current feature configuration.
        /// Used by <see cref="AgeSignalsCacheLogic"/> to detect config changes and
        /// invalidate stale cached flags when thresholds are modified between app updates.
        /// </summary>
        public string ComputeConfigHash()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("v1|ads=").Append(_personalizedAdsMinAge);
            if (_features != null)
            {
                foreach (var f in _features)
                {
                    if (f == null) continue;
                    sb.Append('|').Append(f.key)
                      .Append(':').Append(f.minAge)
                      .Append(':').Append(f.requiresAdult ? '1' : '0');
                }
            }
            return sb.ToString();
        }

        private void OnValidate()
        {
            if (_features == null) return;
            var seen = new HashSet<string>();
            foreach (var f in _features)
            {
                if (f == null) continue;
                if (string.IsNullOrWhiteSpace(f.key))
                {
                    Debug.LogError($"[AgeSignals] Feature with label \"{f.label}\" has an empty key in {name}. Keys must not be empty.", this);
                    continue;
                }
                if (!seen.Add(f.key))
                    Debug.LogWarning($"[AgeSignals] Duplicate feature key \"{f.key}\" in {name}. Each key must be unique.", this);
            }
        }
    }
}
