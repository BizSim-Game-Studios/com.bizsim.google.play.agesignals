using NUnit.Framework;
using UnityEngine;
using BizSim.Google.Play.AgeSignals;

namespace BizSim.Google.Play.AgeSignals.Tests
{
    /// <summary>
    /// Wave 2 Forget API drift guard. Verifies that
    /// <c>EncryptedPlayerPrefsCacheProvider.EraseAll()</c> — the engine behind
    /// <c>AgeSignalsController.ForgetAll()</c> — wipes both the encrypted cache
    /// payload AND the per-install encryption key identifier, restoring the
    /// package to a "fresh install" state for GDPR Article 17 compliance.
    /// </summary>
    /// <remarks>
    /// Tests <see cref="EncryptedPlayerPrefsCacheProvider"/> directly because
    /// <c>AgeSignalsController.ForgetAll</c> requires a <c>MonoBehaviour</c>
    /// instance and a live scene — the provider-level tests here exercise the
    /// actual erasure logic and are fast enough to run on every CI pass.
    /// </remarks>
    [TestFixture]
    public class ForgetApiTest
    {
        // Must match private const in EncryptedPlayerPrefsCacheProvider.
        private const string PayloadKey = "AgeSignals_Cache_Enc";
        private const string KeyIdKey   = "AgeSignals_KeyId";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(PayloadKey);
            PlayerPrefs.DeleteKey(KeyIdKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void EraseAll_DeletesBothPayloadAndKeyId()
        {
            PlayerPrefs.SetString(PayloadKey, "encrypted-blob");
            PlayerPrefs.SetString(KeyIdKey, "per-install-guid");
            PlayerPrefs.Save();

            var provider = new EncryptedPlayerPrefsCacheProvider();
            provider.EraseAll();

            Assert.IsFalse(PlayerPrefs.HasKey(PayloadKey),
                "EraseAll must delete the encrypted payload.");
            Assert.IsFalse(PlayerPrefs.HasKey(KeyIdKey),
                "EraseAll must also delete the per-install key identifier.");
        }

        [Test]
        public void Clear_PreservesKeyIdButDeletesPayload()
        {
            PlayerPrefs.SetString(PayloadKey, "encrypted-blob");
            PlayerPrefs.SetString(KeyIdKey, "per-install-guid");
            PlayerPrefs.Save();

            var provider = new EncryptedPlayerPrefsCacheProvider();
            provider.Clear();

            Assert.IsFalse(PlayerPrefs.HasKey(PayloadKey),
                "Clear must delete the encrypted payload.");
            Assert.IsTrue(PlayerPrefs.HasKey(KeyIdKey),
                "Clear must preserve the per-install key identifier " +
                "(distinct semantics from EraseAll — see ForgetAll vs ClearCachedData).");
        }

        [Test]
        public void EraseAll_IsIdempotent_NoExceptionOnRepeat()
        {
            var provider = new EncryptedPlayerPrefsCacheProvider();
            Assert.DoesNotThrow(() => provider.EraseAll(), "First EraseAll on empty prefs must not throw.");
            Assert.DoesNotThrow(() => provider.EraseAll(), "Second EraseAll (idempotent) must not throw.");
        }

        [Test]
        public void EraseAll_AfterSaveLoad_RoundtripWorksWithFreshKey()
        {
            // Arrange: write some flags via Save, then EraseAll, then write again.
            // The second Save should generate a fresh key id (the first one was erased).
            var provider = new EncryptedPlayerPrefsCacheProvider();
            var flags = new AgeRestrictionFlags { FullAccessGranted = true };
            provider.Save(flags);
            string firstKeyId = PlayerPrefs.GetString(KeyIdKey, "");
            Assert.IsNotEmpty(firstKeyId, "Save must create a key id on first run.");

            // Act.
            provider.EraseAll();
            var freshProvider = new EncryptedPlayerPrefsCacheProvider();
            freshProvider.Save(flags);

            // Assert: a new key id exists and differs from the erased one.
            string secondKeyId = PlayerPrefs.GetString(KeyIdKey, "");
            Assert.IsNotEmpty(secondKeyId, "Save after EraseAll must generate a fresh key id.");
            Assert.AreNotEqual(firstKeyId, secondKeyId,
                "The key id after EraseAll must differ from the previous one — otherwise " +
                "GDPR-erased data would remain decryptable by the old key.");
        }
    }
}
