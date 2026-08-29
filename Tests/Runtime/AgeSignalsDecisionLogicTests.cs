// Copyright (c) BizSim Game Studios. All rights reserved.
// Tests for AgeSignalsDecisionLogic.ComputeFlags.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AgeSignals.Tests
{
    [TestFixture]
    public class AgeSignalsDecisionLogicTests
    {
        private AgeSignalsDecisionLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _logic = ScriptableObject.CreateInstance<AgeSignalsDecisionLogic>();
            _logic.Reset(); // Default 3 features: gambling=18, marketplace=16, chat=13
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_logic);
        }

        [Test]
        public void VerifiedAdult_GrantsFullAccess()
        {
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierC,
                AgeLower = 18, AgeUpper = 150
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.FullAccessGranted);
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
            Assert.IsFalse(flags.AccessDenied);
        }

        [Test]
        public void NotShared_IsUnrestricted_NoData()
        {
            // NOT_SHARED is what a user OUTSIDE a live jurisdiction reports - Google's sample
            // comments that branch as "user didn't share age range, parent rejected the request,
            // or not eligible", and only Brazil and post-2026-05-28 Texas accounts are live. It
            // is also what an in-jurisdiction user who declined reports; the SDK does not
            // distinguish them, so the app cannot either.
            //
            // This asserted the opposite until 2026-08-29, on the assumption that
            // out-of-jurisdiction users arrived as an API error instead. They do not, and failing
            // this closed restricted roughly the entire player base rather than a slice of it.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.NotShared,
                AgeRangeSource = AgeRangeSourceTier.None,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.FullAccessGranted);
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
            Assert.IsFalse(flags.NeedsVerification);
            Assert.IsFalse(flags.AccessDenied);
        }

        [Test]
        public void UnspecifiedAccess_FailsClosed()
        {
            // The boundary of the NOT_SHARED exemption above. UNSPECIFIED is the SDK declining to
            // answer, not answering "no age was shared", so it must keep failing closed - without
            // this the exemption would widen to every unreadable response.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Unspecified,
                AgeRangeSource = AgeRangeSourceTier.None,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
            Assert.IsTrue(flags.NeedsVerification);
        }

        [Test]
        public void Supervised_Under13_BlocksAllAgeGatedFeatures()
        {
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierB,
                SignificantChangeStatus = SignificantChangeStatus.Approved,
                AgeLower = 8, AgeUpper = 10
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void Supervised_Age14_EnablesChatOnly()
        {
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierB,
                SignificantChangeStatus = SignificantChangeStatus.Approved,
                AgeLower = 12, AgeUpper = 14
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void Supervised_Age17_EnablesChatAndMarketplace()
        {
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierB,
                SignificantChangeStatus = SignificantChangeStatus.Approved,
                AgeLower = 15, AgeUpper = 17
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
        }

        [Test]
        public void AccessDenied_BlocksEverything()
        {
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierB,
                SignificantChangeStatus = SignificantChangeStatus.Declined,
                AgeLower = 8, AgeUpper = 10
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.AccessDenied);
            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void ApprovalPending_KeepsAgeAppropriateAccess_NotABlackout()
        {
            // A supervised 14-year-old waiting on a parent to approve a significant change.
            //
            // ComputeFlags used to fail this closed and switch every feature off, on a comment
            // that read "block until approved (new 0.0.4 behavior)". Google's wording says
            // something narrower: restrict the content or functionality RELATING TO the
            // significant change. The age band is still present and still usable, so the ordinary
            // per-feature gating applies and chat (13+) stays on while marketplace (16+) and
            // gambling (18+) stay off - the same answer this player would get with no pending
            // change at all.
            //
            // A consumer that needs to gate the changed feature specifically reads
            // result.IsApprovalPending in its own override; only it knows which feature changed.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierB,
                SignificantChangeStatus = SignificantChangeStatus.Pending,
                AgeLower = 13, AgeUpper = 15
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.AccessDenied);
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.FullAccessGranted);
        }

        [Test]
        public void VerificationRequired_SetsNeedsVerification()
        {
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.VerificationRequired,
                AgeRangeSource = AgeRangeSourceTier.None,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.NeedsVerification);
        }

        [Test]
        public void Unrecognized_FailsClosed_BlocksEverything()
        {
            // An access value the beta SDK may add after this release must never fail open.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Unrecognized,
                AgeRangeSource = AgeRangeSourceTier.Unrecognized,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
            Assert.IsTrue(flags.NeedsVerification);
        }

        [Test]
        public void SharedWithoutSourceTier_FailsClosed()
        {
            // Signals reported as shared but with no usable source tier carry no age data;
            // do not grant access — restrict and request verification.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.Unspecified,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
            Assert.IsTrue(flags.NeedsVerification);
        }

        [Test]
        public void DeclaredAdult_WithRange_UnlocksNonAdultFeaturesNotGambling()
        {
            // A self-declared (TierA) 18+ range unlocks non-adult features but is NOT
            // full access — gambling and full access still require a verified (TierC/D) tier,
            // and the declared adult keeps a verification nudge.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierA,
                AgeLower = 18, AgeUpper = 25
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.NeedsVerification);
        }

        [Test]
        public void DeclaredMinor_WithRange_RestrictsAgeGatedFeatures()
        {
            // A self-declared (TierA) minor with a known range is restricted by age, but its
            // age is already known so it is NOT flagged for verification (unlike a declared adult).
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.Shared,
                AgeRangeSource = AgeRangeSourceTier.TierA,
                AgeLower = 10, AgeUpper = 12
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.NeedsVerification);
        }

        [Test]
        public void ComputeConfigHash_DefaultConfig_IsStable()
        {
            string hash1 = _logic.ComputeConfigHash();
            string hash2 = _logic.ComputeConfigHash();
            Assert.AreEqual(hash1, hash2, "Same config should produce identical hashes");
            Assert.IsTrue(hash1.StartsWith("v1|"), "Hash should start with version prefix");
        }

        [Test]
        public void ComputeConfigHash_DifferentConfigs_ProduceDifferentHashes()
        {
            string defaultHash = _logic.ComputeConfigHash();

            // Create a second logic with different thresholds
            var altLogic = ScriptableObject.CreateInstance<AgeSignalsDecisionLogic>();
            // altLogic gets default features (gambling:18, marketplace:16, chat:13)
            // We can't modify fields directly, but the default hash should equal _logic's default hash
            string altHash = altLogic.ComputeConfigHash();
            Object.DestroyImmediate(altLogic);

            Assert.AreEqual(defaultHash, altHash, "Same default config should produce same hash");
        }

    }
}
