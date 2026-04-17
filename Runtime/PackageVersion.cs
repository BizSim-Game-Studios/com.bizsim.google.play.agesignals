// Copyright (c) BizSim Game Studios. All rights reserved.
// IMPORTANT: Update this value manually when bumping the version in package.json.

namespace BizSim.Google.Play.AgeSignals
{
    /// <summary>
    /// Package version constant used for cache invalidation.
    /// When the SDK version changes after an upgrade, cached restriction flags are
    /// invalidated to ensure they are recomputed with the new decision logic code.
    /// <para>
    /// <b>Maintenance:</b> Keep this value in sync with the <c>"version"</c> field
    /// in <c>package.json</c>. Update both files together when releasing a new version.
    /// </para>
    /// </summary>
    internal static class PackageVersion
    {
        /// <summary>Current package version — must match <c>package.json</c>.</summary>
        public const string Current = "1.1.0";

        /// <summary>Date of the current release (ISO 8601).</summary>
        public const string ReleaseDate = "2026-04-17";

        // === Canonical K8 fields (Plan G — first introduction for this package) ===
        public const string NativeSdkVersion       = "0.0.3";
        public const string NativeSdkLabel         = "Play Core (age-signals beta)";
        public const string NativeSdkArtifactCoord = "com.google.android.play:age-signals:0.0.3";
    }
}
