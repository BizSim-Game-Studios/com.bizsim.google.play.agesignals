using UnityEditor;

namespace BizSim.Google.Play.AgeSignals.Editor
{
    [InitializeOnLoad]
    static class AgeSignalsEditorInit
    {
        static AgeSignalsEditorInit()
        {
            BizSim.Google.Play.Editor.Core.BizSimDefineManager.AddDefine(
                "BIZSIM_AGESIGNALS_INSTALLED",
                BizSim.Google.Play.Editor.Core.BizSimDefineManager.GetRelevantPlatforms());
        }
    }
}
