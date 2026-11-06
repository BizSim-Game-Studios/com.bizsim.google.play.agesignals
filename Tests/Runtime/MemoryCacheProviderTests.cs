// Copyright (c) BizSim Game Studios. All rights reserved.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AgeSignals.Tests
{
    /// <summary>
    /// The memory-only provider must keep flags in the process and never touch PlayerPrefs,
    /// so the API result cannot outlive the app session.
    /// </summary>
    [TestFixture]
    public class MemoryCacheProviderTests
    {
        private const string PlainKey = "AgeRestriction_Flags";
        private const string EncryptedKey = "AgeSignals_Cache_Enc";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(PlainKey);
            PlayerPrefs.DeleteKey(EncryptedKey);
        }

        [Test]
        public void SaveLoadClear_RoundTripsInMemory()
        {
            var provider = new MemoryAgeSignalsCacheProvider();
            Assert.IsNull(provider.Load());

            var flags = new AgeRestrictionFlags { FullAccessGranted = true };
            provider.Save(flags);
            Assert.AreSame(flags, provider.Load());

            provider.Clear();
            Assert.IsNull(provider.Load());
        }

        [Test]
        public void Save_WritesNothingToPlayerPrefs()
        {
            new MemoryAgeSignalsCacheProvider().Save(new AgeRestrictionFlags { FullAccessGranted = true });

            Assert.IsFalse(PlayerPrefs.HasKey(PlainKey));
            Assert.IsFalse(PlayerPrefs.HasKey(EncryptedKey));
        }

        [Test]
        public void NewInstance_StartsEmpty()
        {
            new MemoryAgeSignalsCacheProvider().Save(new AgeRestrictionFlags());
            Assert.IsNull(new MemoryAgeSignalsCacheProvider().Load());
        }
    }
}
