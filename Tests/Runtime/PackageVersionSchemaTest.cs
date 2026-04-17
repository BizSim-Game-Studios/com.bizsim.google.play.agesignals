using NUnit.Framework;
using BizSim.Google.Play.AgeSignals;

namespace BizSim.Google.Play.AgeSignals.Tests
{
    /// <summary>K8 PackageVersion schema drift guard (Plan G — first intro).</summary>
    public class PackageVersionSchemaTest
    {
        [Test]
        public void NativeSdkFields_ArePopulated()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkVersion));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkLabel));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkArtifactCoord));
        }

        [Test]
        public void NativeSdkArtifactCoord_EndsWithVersion()
        {
            Assert.IsTrue(PackageVersion.NativeSdkArtifactCoord.EndsWith(":" + PackageVersion.NativeSdkVersion));
        }

        [Test]
        public void NativeSdkFields_MatchExpectedAgeSignalsValues()
        {
            Assert.AreEqual("0.0.3", PackageVersion.NativeSdkVersion);
            Assert.AreEqual("Play Core (age-signals beta)", PackageVersion.NativeSdkLabel);
            Assert.AreEqual("com.google.android.play:age-signals:0.0.3", PackageVersion.NativeSdkArtifactCoord);
        }
    }
}
