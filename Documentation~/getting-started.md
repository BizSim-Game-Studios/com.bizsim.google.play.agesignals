# Getting Started

Last reviewed: 2026-04-16

## Prerequisites

- Unity 6000.0 or later
- Android build target selected in Build Settings
- Google Play Age Signals SDK v0.0.3 (resolved automatically via EDM4U)
- EDM4U (External Dependency Manager for Unity) installed in your project

## Step 1 — Install the package

Add the package via Git URL in Unity Editor: **Window > Package Manager > + > Add package from git URL**:

```
https://github.com/BizSim-Game-Studios/com.bizsim.google.play.agesignals.git#v1.0.3
```

Or add directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.bizsim.google.play.agesignals": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.agesignals.git#v1.0.3"
  }
}
```

## Step 2 — Resolve Android dependencies

Run **Assets > External Dependency Manager > Android Resolver > Force Resolve**. This pulls `com.google.android.play:age-signals:0.0.3` from Google Maven.

ProGuard rules are applied automatically via the `.androidlib` subproject.

## Step 3 — Check age signals on app launch

Add the following to a MonoBehaviour in your startup scene:

```csharp
using BizSim.Google.Play.AgeSignals;
using UnityEngine;

public class AgeGate : MonoBehaviour
{
    async void Start()
    {
        var flags = await AgeSignalsController.Instance.CheckAgeSignalsAsync();
        if (flags.FullAccessGranted)
            Debug.Log("Full access granted");
        else if (!flags.PersonalizedAdsEnabled)
            Debug.Log("Personalized ads disabled — show contextual ads only");
    }
}
```

## Step 4 — Verify in Editor

Enter Play Mode. The mock provider returns simulated age flags based on the assigned
`AgeSignalsMockConfig` ScriptableObject. Create mock configs via **Assets > Create > BizSim > Age Signals Mock Config**.

## Step 5 — Test on a device

Deploy to an Android device with Google Play Services. The Age Signals API requires a device
with Google Play and an active Google account. The API response depends on the account's
age verification status.

## Privacy reminders

- Raw age data (`AgeSignalsResult`) is ephemeral and never touches disk
- Cached `AgeRestrictionFlags` auto-expire after 24 hours
- Never log, serialize, or transmit raw age data — the architecture prevents it by design
- When implementing custom cache providers, use `IAgeSignalsCacheProvider` and persist only
  the derived flags, never the raw result

## What to expect

- The API call should complete within a few seconds on a connected device
- On error, the controller retries up to 3 times for transient errors
- Cached flags from a previous session are available immediately via `CurrentFlags`
- The decision logic supports configurable feature-level age thresholds via
  `AgeSignalsDecisionLogic` ScriptableObject
