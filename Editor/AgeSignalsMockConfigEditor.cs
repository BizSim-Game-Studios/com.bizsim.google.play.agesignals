// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AgeSignals.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="AgeSignalsMockConfig"/> that provides a visual
    /// mock response editor, error simulation controls, a JSON preview card,
    /// and a reference guide for verification statuses.
    /// </summary>
    [CustomEditor(typeof(AgeSignalsMockConfig))]
    public class AgeSignalsMockConfigEditor : UnityEditor.Editor
    {
        // ── Palette ──
        private static readonly Color Accent = new(0.35f, 0.61f, 1f);
        private static readonly Color AccentDim = new(0.35f, 0.61f, 1f, 0.08f);
        private static readonly Color Green = new(0.24f, 0.78f, 0.42f);
        private static readonly Color GreenDim = new(0.24f, 0.78f, 0.42f, 0.14f);
        private static readonly Color Red = new(0.92f, 0.34f, 0.34f);
        private static readonly Color RedDim = new(0.92f, 0.34f, 0.34f, 0.14f);
        private static readonly Color Warn = new(1f, 0.82f, 0.22f);
        private static readonly Color WarnDim = new(1f, 0.82f, 0.22f, 0.12f);
        private static readonly Color Muted = new(0.6f, 0.6f, 0.6f);
        private static readonly Color CardBg = new(0.22f, 0.22f, 0.22f, 0.55f);
        private static readonly Color SepColor = new(1f, 1f, 1f, 0.06f);

        // ── Serialized Properties ──
        private SerializedProperty _mockAccessStatus;
        private SerializedProperty _mockSource;
        private SerializedProperty _mockChangeStatus;
        private SerializedProperty _mockAge;
        private SerializedProperty _simulateError;
        private SerializedProperty _simulatedErrorCode;

        private void OnEnable()
        {
            _mockAccessStatus = serializedObject.FindProperty("MockAccessStatus");
            _mockSource = serializedObject.FindProperty("MockSource");
            _mockChangeStatus = serializedObject.FindProperty("MockChangeStatus");
            _mockAge = serializedObject.FindProperty("MockAge");
            _simulateError = serializedObject.FindProperty("SimulateError");
            _simulatedErrorCode = serializedObject.FindProperty("SimulatedErrorCode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (AgeSignalsMockConfig)target;

            // ═══════════════════════════════════════════
            // Mock Response
            // ═══════════════════════════════════════════
            DrawMockResponseCard(config);

            EditorGUILayout.Space(6);

            // ═══════════════════════════════════════════
            // Error Simulation
            // ═══════════════════════════════════════════
            DrawErrorSimulationCard();

            EditorGUILayout.Space(6);

            // ═══════════════════════════════════════════
            // Preview — what the API would return
            // ═══════════════════════════════════════════
            DrawPreviewCard(config);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            // ═══════════════════════════════════════════
            // What is this?
            // ═══════════════════════════════════════════
            DrawInfoCard();
        }

        // ─────────────────────────────────────────────
        // Mock Response Card
        // ─────────────────────────────────────────────

        private void DrawMockResponseCard(AgeSignalsMockConfig config)
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Mock API Response", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_mockAccessStatus,
                new GUIContent("Access Status", "Whether the API reports the age signal as shared."));
            EditorGUILayout.PropertyField(_mockSource,
                new GUIContent("Source Tier", "Age range source tier. TierB = supervised minor; TierC/D = verified adult; TierA = self-declared."));

            var source = (AgeRangeSourceTier)_mockSource.enumValueIndex;
            bool isSupervised = source == AgeRangeSourceTier.TierB;

            // Guardian approval — only meaningful for supervised (TierB) minors
            using (new EditorGUI.DisabledGroupScope(!isSupervised))
            {
                EditorGUILayout.PropertyField(_mockChangeStatus,
                    new GUIContent("Approval", "Guardian approval status. Only meaningful for TierB supervised minors."));
            }

            bool hasAge = source == AgeRangeSourceTier.TierA ||
                          source == AgeRangeSourceTier.TierB ||
                          source == AgeRangeSourceTier.TierC ||
                          source == AgeRangeSourceTier.TierD;

            // Age slider — drives the reported range for concrete tiers
            using (new EditorGUI.DisabledGroupScope(!hasAge))
            {
                EditorGUILayout.IntSlider(_mockAge, 5, 25,
                    new GUIContent("Age", "Simulated age. Drives the reported range for supervised and declared tiers."));
            }

            // Computed age range preview (from the pending tier + age)
            GUILayout.Space(4);
            GetFakeRange(source, _mockAge.intValue, out int lo, out int hi);
            string rangeText;
            Color rangeColor;
            if (lo < 0)
            {
                rangeText = "Age range: N/A  (no usable age data)";
                rangeColor = Muted;
            }
            else if (source == AgeRangeSourceTier.TierC || source == AgeRangeSourceTier.TierD)
            {
                rangeText = $"Age range: {lo} – {hi}  (verified tier)";
                rangeColor = lo >= 18 ? Green : Warn;
            }
            else
            {
                string band = source == AgeRangeSourceTier.TierB ? "±2 year bucket" : "declared band";
                rangeText = $"Age range: {lo} – {hi}  ({band})";
                rangeColor = hi < 13 ? Red : hi < 18 ? Warn : Green;
            }

            var rangeRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rangeRect, new Color(rangeColor.r, rangeColor.g, rangeColor.b, 0.08f));
            EditorGUI.DrawRect(new Rect(rangeRect.x, rangeRect.y, 3, rangeRect.height), rangeColor);
            EditorGUI.LabelField(new Rect(rangeRect.x + 10, rangeRect.y, rangeRect.width - 12, rangeRect.height),
                rangeText, new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = rangeColor }
                });

            // Access denied warning — supervised minor whose guardian approval was declined
            var change = (SignificantChangeStatus)_mockChangeStatus.enumValueIndex;
            if (isSupervised && change == SignificantChangeStatus.Declined)
            {
                GUILayout.Space(4);
                var warnRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.DrawRect(warnRect, RedDim);
                EditorGUI.DrawRect(new Rect(warnRect.x, warnRect.y, 3, warnRect.height), Red);
                EditorGUI.LabelField(new Rect(warnRect.x + 10, warnRect.y, warnRect.width - 12, warnRect.height),
                    "⚠  This status blocks all access (parental approval denied).",
                    new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Red }
                    });
            }

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Error Simulation Card
        // ─────────────────────────────────────────────

        private void DrawErrorSimulationCard()
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Error Simulation", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_simulateError,
                new GUIContent("Simulate Error", "Return an error instead of a successful response."));

            if (_simulateError.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_simulatedErrorCode,
                    new GUIContent("Error Code", "API error code to simulate."));

                GUILayout.Space(4);

                // Error code reference
                int code = _simulatedErrorCode.intValue;
                // Reuse AgeSignalsError for consistent name resolution
                var tempError = new AgeSignalsError { errorCode = code };
                string codeName = tempError.ErrorCodeName;
                bool retryable = AgeSignalsError.IsRetryableCode(code);

                var infoRect = EditorGUILayout.GetControlRect(false, 20);
                Color infoColor = retryable ? Warn : Red;
                EditorGUI.DrawRect(infoRect, new Color(infoColor.r, infoColor.g, infoColor.b, 0.08f));
                EditorGUI.DrawRect(new Rect(infoRect.x, infoRect.y, 3, infoRect.height), infoColor);
                string retryText = retryable ? "retryable" : "not retryable";
                EditorGUI.LabelField(
                    new Rect(infoRect.x + 10, infoRect.y, infoRect.width - 12, infoRect.height),
                    $"{codeName}  •  {retryText}",
                    new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        normal = { textColor = infoColor }
                    });

                EditorGUI.indentLevel--;
            }
            else
            {
                DrawNote("Enable to test how your game handles API failures.");
            }

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Preview Card — simulated JSON output
        // ─────────────────────────────────────────────

        private void DrawPreviewCard(AgeSignalsMockConfig config)
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            DrawNote("This is what the controller will receive in Play Mode.");
            GUILayout.Space(4);

            var codeStyle = new GUIStyle(EditorStyles.label)
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                fontSize = 11,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.86f, 0.68f) },
                padding = new RectOffset(8, 8, 6, 6)
            };
            var codeBg = new Color(0.12f, 0.12f, 0.12f, 0.8f);

            string json;
            if (config.SimulateError)
            {
                int code = config.SimulatedErrorCode;
                var tempError = new AgeSignalsError { errorCode = code };
                string codeName = tempError.ErrorCodeName;
                bool retryable = AgeSignalsError.IsRetryableCode(code);
                json = $"<color=#569cd6>Error Response</color>\n" +
                       $"  errorCode: <color=#b5cea8>{code}</color>\n" +
                       $"  errorMessage: <color=#ce9178>\"Simulated error (mock config)\"</color>\n" +
                       $"  isRetryable: <color=#569cd6>{retryable.ToString().ToLower()}</color>\n" +
                       $"  codeName: <color=#ce9178>\"{codeName}\"</color>";
            }
            else
            {
                string accessStr = config.MockAccessStatus switch
                {
                    AgeSignalsAccessStatus.Shared => "SHARED",
                    AgeSignalsAccessStatus.NotShared => "NOT_SHARED",
                    AgeSignalsAccessStatus.VerificationRequired => "VERIFICATION_REQUIRED",
                    AgeSignalsAccessStatus.Unspecified => "UNSPECIFIED",
                    _ => "UNRECOGNIZED"
                };
                string sourceStr = config.MockSource switch
                {
                    AgeRangeSourceTier.TierA => "\"TIER_A\"",
                    AgeRangeSourceTier.TierB => "\"TIER_B\"",
                    AgeRangeSourceTier.TierC => "\"TIER_C\"",
                    AgeRangeSourceTier.TierD => "\"TIER_D\"",
                    AgeRangeSourceTier.Unspecified => "\"UNSPECIFIED\"",
                    AgeRangeSourceTier.Unrecognized => "\"UNRECOGNIZED\"",
                    _ => "null"
                };
                string changeStr = config.MockChangeStatus switch
                {
                    SignificantChangeStatus.Approved => "\"APPROVED\"",
                    SignificantChangeStatus.Pending => "\"PENDING\"",
                    SignificantChangeStatus.Declined => "\"DECLINED\"",
                    SignificantChangeStatus.Unspecified => "\"UNSPECIFIED\"",
                    SignificantChangeStatus.Unrecognized => "\"UNRECOGNIZED\"",
                    _ => "null"
                };
                string loStr = config.AgeLower < 0 ? "null" : config.AgeLower.ToString();
                string hiStr = config.AgeUpper < 0 ? "null" : config.AgeUpper.ToString();

                json = $"<color=#569cd6>Success Response</color>\n" +
                       $"  accessStatus: <color=#ce9178>\"{accessStr}\"</color>\n" +
                       $"  ageRangeSource: <color=#ce9178>{sourceStr}</color>\n" +
                       $"  significantChangeStatus: <color=#ce9178>{changeStr}</color>\n" +
                       $"  ageLower: <color=#b5cea8>{loStr}</color>\n" +
                       $"  ageUpper: <color=#b5cea8>{hiStr}</color>\n" +
                       $"  installId: <color=#569cd6>null</color>\n" +
                       $"  significantChangeApprovalDate: <color=#b5cea8>0</color>";
            }

            var codeRect = EditorGUILayout.GetControlRect(false, 100);
            EditorGUI.DrawRect(codeRect, codeBg);
            EditorGUI.LabelField(codeRect, json, codeStyle);

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // What is this?
        // ─────────────────────────────────────────────

        private static void DrawInfoCard()
        {
            var outer = EditorGUILayout.BeginVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(outer, AccentDim);
                EditorGUI.DrawRect(new Rect(outer.x, outer.y, 3, outer.height), Accent);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("What is this?", EditorStyles.boldLabel);
            GUILayout.Space(2);

            EditorGUILayout.LabelField(
                "This asset lets you test Age Signals without a real device or Google Play account.\n\n" +
                "It simulates what the Google Play Age Signals 0.0.4 API would return — an access\n" +
                "status, an age range source tier, and (for supervised minors) a guardian approval\n" +
                "status. The Age Signals Controller uses this mock data in Play Mode instead of\n" +
                "calling the real API.\n\n" +
                "This is Editor-only. It has no effect in actual builds.",
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                });

            GUILayout.Space(6);

            EditorGUILayout.LabelField("How to use:", EditorStyles.boldLabel);
            DrawBullet("Pick an Access Status and Source Tier — this is the user type you want to simulate.");
            DrawBullet("Set an Age — drives the reported range for supervised and declared tiers.");
            DrawBullet("Set Approval — only matters for TierB supervised minors (Declined blocks access).");
            DrawBullet("Enable \"Simulate Error\" to test error handling in your game.");
            DrawBullet("Assign this asset to the Mock Config field on the Age Signals Controller.");
            DrawBullet("Enter Play Mode → the controller reads this config instead of calling the API.");

            GUILayout.Space(6);

            EditorGUILayout.LabelField("Access status reference:", EditorStyles.boldLabel);
            DrawStatusRef("Shared", "Age signals were shared — age fields are populated.");
            DrawStatusRef("Not Shared", "User declined sharing. In jurisdiction, so no age data.");
            DrawStatusRef("Verification Required", "Mandatory jurisdiction, age unknown — verification needed.");
            DrawStatusRef("Unspecified", "No status — treated fail-closed.");

            GUILayout.Space(6);

            EditorGUILayout.LabelField("Source tier reference:", EditorStyles.boldLabel);
            DrawStatusRef("TierA", "Self-declared age. Unlocks non-adult features but is not full access.");
            DrawStatusRef("TierB", "Guardian-managed supervised minor. Uses the Approval status.");
            DrawStatusRef("TierC / TierD", "Assessed / highest verification — confirmed 18+ adult.");
            DrawStatusRef("None / Unspecified", "No usable age data — fails closed.");

            GUILayout.Space(6);

            EditorGUILayout.LabelField("Approval reference (TierB):", EditorStyles.boldLabel);
            DrawStatusRef("Approved", "Guardian approved the most recent change.");
            DrawStatusRef("Pending", "Awaiting approval — access blocked until approved.");
            DrawStatusRef("Declined", "Guardian denied approval — blocks everything.");

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        /// <summary>
        /// Computes the reported age range from a source tier and simulated age, matching
        /// <see cref="AgeSignalsMockConfig.AgeLower"/> / <see cref="AgeSignalsMockConfig.AgeUpper"/>.
        /// </summary>
        private static void GetFakeRange(AgeRangeSourceTier source, int age, out int lower, out int upper)
        {
            switch (source)
            {
                case AgeRangeSourceTier.TierC:
                case AgeRangeSourceTier.TierD:
                    lower = age >= 18 ? 18 : age;
                    upper = age >= 18 ? 150 : age;
                    break;
                case AgeRangeSourceTier.TierB:
                    lower = Mathf.Max(0, age - 2);
                    upper = age + 2;
                    break;
                case AgeRangeSourceTier.TierA:
                    if (age < 13) { lower = 0; upper = 12; }
                    else if (age < 16) { lower = 13; upper = 15; }
                    else if (age < 18) { lower = 16; upper = 17; }
                    else { lower = 18; upper = 150; }
                    break;
                default:
                    lower = -1;
                    upper = -1;
                    break;
            }
        }

        private static void DrawCardBg(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, CardBg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), SepColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), SepColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), SepColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), SepColor);
        }

        private static void BeginPadded()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();
        }

        private static void EndPadded()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(14);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawNote(string text)
        {
            EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontStyle = FontStyle.Italic
            });
        }

        private static void DrawBullet(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField("•", GUILayout.Width(10));
            EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.78f, 0.78f) }
            });
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStatusRef(string status, string desc)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField($"<b>{status}</b>", new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 11
            }, GUILayout.Width(200));
            EditorGUILayout.LabelField(desc, new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.72f, 0.72f, 0.72f) }
            });
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
