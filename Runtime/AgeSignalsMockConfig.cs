// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEngine;

namespace BizSim.Google.Play.AgeSignals
{
    /// <summary>
    /// ScriptableObject for configuring mock Age Signals 0.0.4 responses in the Unity Editor.
    /// Create via <b>Assets → Create → BizSim → Age Signals Mock Config</b>.
    ///
    /// Age ranges are computed automatically based on the source tier:
    /// <list type="bullet">
    /// <item><b>TierC / TierD</b> — verified/assessed 18+ adult (range: 18–150)</item>
    /// <item><b>TierB</b> — supervised minor, ±2 year bucket around <see cref="MockAge"/></item>
    /// <item><b>TierA</b> — self-declared, banded around <see cref="MockAge"/></item>
    /// <item><b>None / Unspecified</b> — no age data (range: -1, -1)</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(menuName = "BizSim/Age Signals/Age Signals Mock Config")]
    public class AgeSignalsMockConfig : ScriptableObject
    {
        [Header("Mock API Response (0.0.4)")]
        [Tooltip("Access status to simulate (was the age signal shared).")]
        public AgeSignalsAccessStatus MockAccessStatus = AgeSignalsAccessStatus.Shared;

        [Tooltip("Age range source tier to simulate. TierB = supervised minor.")]
        public AgeRangeSourceTier MockSource = AgeRangeSourceTier.None;

        [Tooltip("Guardian approval status (only meaningful for TierB supervised minors).")]
        public SignificantChangeStatus MockChangeStatus = SignificantChangeStatus.None;

        [Tooltip("Simulated age (5–25). Ignored for verified adult tiers.")]
        [Range(5, 25)]
        public int MockAge = 14;

        [Header("Error Simulation")]
        [Tooltip("When enabled, simulates an API error instead of a successful response.")]
        public bool SimulateError = false;

        [Tooltip("Error code to simulate. See AgeSignalsErrorCode enum for values.")]
        public int SimulatedErrorCode = (int)AgeSignalsErrorCode.NetworkError;

        /// <summary>Computed lower bound based on source tier and age.</summary>
        public int AgeLower
        {
            get
            {
                return MockSource switch
                {
                    AgeRangeSourceTier.TierC or AgeRangeSourceTier.TierD => MockAge >= 18 ? 18 : MockAge,
                    AgeRangeSourceTier.TierB => Mathf.Max(0, MockAge - 2),
                    AgeRangeSourceTier.TierA => MockAge < 13 ? 0 : MockAge < 16 ? 13 : MockAge < 18 ? 16 : 18,
                    _ => -1
                };
            }
        }

        /// <summary>Computed upper bound based on source tier and age.</summary>
        public int AgeUpper
        {
            get
            {
                return MockSource switch
                {
                    AgeRangeSourceTier.TierC or AgeRangeSourceTier.TierD => MockAge >= 18 ? 150 : MockAge,
                    AgeRangeSourceTier.TierB => MockAge + 2,
                    AgeRangeSourceTier.TierA => MockAge < 13 ? 12 : MockAge < 16 ? 15 : MockAge < 18 ? 17 : 150,
                    _ => -1
                };
            }
        }
    }
}
#endif
