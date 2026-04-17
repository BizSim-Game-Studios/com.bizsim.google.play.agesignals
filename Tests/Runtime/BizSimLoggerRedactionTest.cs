using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BizSim.Google.Play.AgeSignals.Tests
{
    /// <summary>
    /// Drift guard for C2.7 (log redaction at Info+ levels) per
    /// development-plans/plans/2026-04-17-enterprise-quality-bar/05-per-package-sketches/agesignals.md
    /// (Wave 0 hotfix, v1.0.4 PATCH).
    ///
    /// Asserts that no BizSimLogger.Info / Warning / Error call in
    /// AgeSignalsController.cs contains raw age numerics. Such content is
    /// captured by Android Logcat and Firebase Crashlytics breadcrumbs and
    /// would violate privacy hard-constraint #3 (analytics PII-free) even
    /// though it does not traverse the analytics adapter directly.
    ///
    /// Future code emitting age numerics at Info+ levels will fail this test.
    /// Use BizSimLogger.Verbose (release-build-silent) for age values.
    /// </summary>
    public class BizSimLoggerRedactionTest
    {
        private static readonly Regex AgeNumericRegex = new Regex(
            @"age=\[|AgeLower|AgeUpper|age_lower|age_upper",
            RegexOptions.IgnoreCase);

        private const string ControllerSearchPath =
            "Packages/com.bizsim.google.play.agesignals/Runtime/AgeSignalsController.cs";

        private static string ReadControllerSource()
        {
            // Works whether package is installed via git URL, file: path, or tested in-place.
            if (File.Exists(ControllerSearchPath))
                return File.ReadAllText(ControllerSearchPath);

            // Fallback: in-package dev context
            const string fallback = "Runtime/AgeSignalsController.cs";
            if (File.Exists(fallback))
                return File.ReadAllText(fallback);

            Assert.Inconclusive("Could not locate AgeSignalsController.cs at either " +
                ControllerSearchPath + " or " + fallback);
            return null;
        }

        [Test]
        public void InfoLevel_DoesNotEmitAgeNumerics()
        {
            var source = ReadControllerSource();
            var infoCallRegex = new Regex(@"BizSimLogger\.Info\([^;]*\)", RegexOptions.Singleline);

            foreach (Match m in infoCallRegex.Matches(source))
            {
                Assert.IsFalse(AgeNumericRegex.IsMatch(m.Value),
                    "BizSimLogger.Info call must not contain age numerics (C2.7 compliance). " +
                    "Offending call:\n" + m.Value.Substring(0, System.Math.Min(200, m.Value.Length)) +
                    "\n...\nMove to BizSimLogger.Verbose (release-build-silent).");
            }
        }

        [Test]
        public void WarningLevel_DoesNotEmitAgeNumerics()
        {
            var source = ReadControllerSource();
            var warningCallRegex = new Regex(@"BizSimLogger\.Warning\([^;]*\)", RegexOptions.Singleline);

            foreach (Match m in warningCallRegex.Matches(source))
            {
                Assert.IsFalse(AgeNumericRegex.IsMatch(m.Value),
                    "BizSimLogger.Warning call must not contain age numerics (C2.7). " +
                    "Offending call:\n" + m.Value);
            }
        }

        [Test]
        public void ErrorLevel_DoesNotEmitAgeNumerics()
        {
            var source = ReadControllerSource();
            var errorCallRegex = new Regex(@"BizSimLogger\.Error\([^;]*\)", RegexOptions.Singleline);

            foreach (Match m in errorCallRegex.Matches(source))
            {
                Assert.IsFalse(AgeNumericRegex.IsMatch(m.Value),
                    "BizSimLogger.Error call must not contain age numerics (C2.7). " +
                    "Offending call:\n" + m.Value);
            }
        }
    }
}
