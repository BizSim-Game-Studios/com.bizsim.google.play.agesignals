// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

namespace BizSim.Google.Play.AgeSignals
{
    /// <summary>
    /// Cache provider that keeps the restriction flags in memory only: nothing derived from the
    /// Age Signals API result is written to disk, so every app launch starts without flags
    /// (<see cref="AgeSignalsController.CurrentFlags"/> falls back to the restrictive defaults)
    /// until the next API call answers.
    /// Plug in before the first check: <c>AgeSignalsController.Instance.SetCacheProvider(new MemoryAgeSignalsCacheProvider());</c>
    /// </summary>
    public class MemoryAgeSignalsCacheProvider : IAgeSignalsCacheProvider
    {
        private AgeRestrictionFlags _flags;

        /// <inheritdoc />
        public AgeRestrictionFlags Load() => _flags;

        /// <inheritdoc />
        public void Save(AgeRestrictionFlags flags) => _flags = flags;

        /// <inheritdoc />
        public void Clear() => _flags = null;
    }
}
