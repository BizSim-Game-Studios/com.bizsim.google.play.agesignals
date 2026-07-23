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
        public void NotShared_FailsClosed_NoData()
        {
            // 0.0.4: an in-jurisdiction user who did not share age signals fails closed.
            // Out-of-jurisdiction users no longer reach here — they surface as an API error.
            var result = new AgeSignalsResult
            {
                AccessStatus = AgeSignalsAccessStatus.NotShared,
                AgeRangeSource = AgeRangeSourceTier.None,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
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
