// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

namespace BizSim.Google.Play.AgeSignals
{
    /// <summary>
    /// Where <see cref="AgeSignalsController"/> keeps the derived restriction flags between app
    /// launches. <see cref="Memory"/> is the default: the API result is not stored on the device.
    /// </summary>
    public enum AgeSignalsCacheMode
    {
        /// <summary>In memory only (<see cref="MemoryAgeSignalsCacheProvider"/>). Nothing is written to disk.</summary>
        Memory = 0,

        /// <summary>Plain JSON in <c>PlayerPrefs</c> (<see cref="PlayerPrefsCacheProvider"/>). Explicit opt-in.</summary>
        PlayerPrefs = 1,

        /// <summary>AES-encrypted <c>PlayerPrefs</c> (<see cref="EncryptedPlayerPrefsCacheProvider"/>). Explicit opt-in.</summary>
        EncryptedPlayerPrefs = 2
    }
}
