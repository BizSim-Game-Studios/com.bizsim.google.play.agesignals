// Copyright (c) BizSim Game Studios. All rights reserved.
//
// Age Signals 0.0.4 bridge. Two-phase flow:
//   requestAgeSignalsAccess(activity) -> AgeSignalsAccessResult.ageSignalsStatus()
//     SHARED                -> checkAgeSignals() -> serialize full result
//     NOT_SHARED            -> return result with accessStatus only (no age data)
//     VERIFICATION_REQUIRED -> return result with accessStatus only (mandatory jurisdiction, unknown age)
//     UNSPECIFIED/other     -> treated as no data (fail-closed) by the C# decision layer

package com.bizsim.google.play.agesignals;

import android.app.Activity;
import android.util.Log;

import com.google.android.play.agesignals.AgeSignalsAccessRequest;
import com.google.android.play.agesignals.AgeSignalsAccessResult;
import com.google.android.play.agesignals.AgeSignalsException;
import com.google.android.play.agesignals.AgeSignalsManager;
import com.google.android.play.agesignals.AgeSignalsManagerFactory;
import com.google.android.play.agesignals.AgeSignalsRequest;
import com.google.android.play.agesignals.AgeSignalsResult;
import com.google.android.play.agesignals.model.AgeRangeSource;
import com.google.android.play.agesignals.model.AgeSignalsStatus;
import com.google.android.play.agesignals.model.SignificantChangeStatus;
import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

public class AgeSignalsBridge {

    private static final String TAG = "AgeSignalsBridge";

    public static void checkAgeSignals(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod) {

        checkAgeSignalsInternal(gameObjectName, successMethod, errorMethod,
                false, null, null, -1, -1);
    }

    // Fake path for on-device testing. fakeSource / fakeChangeStatus are the new 0.0.4 axes.
    public static void checkAgeSignalsWithFake(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final boolean useFake,
            final String fakeSource,
            final String fakeChangeStatus,
            final int fakeAgeLower,
            final int fakeAgeUpper) {

        checkAgeSignalsInternal(gameObjectName, successMethod, errorMethod,
                useFake, fakeSource, fakeChangeStatus, fakeAgeLower, fakeAgeUpper);
    }

    private static void checkAgeSignalsInternal(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final boolean useFake,
            final String fakeSource,
            final String fakeChangeStatus,
            final int fakeAgeLower,
            final int fakeAgeUpper) {

        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            sendError(gameObjectName, errorMethod, -100, "Unity activity is null");
            return;
        }

        try {
            if (useFake) {
                runFake(gameObjectName, successMethod, errorMethod,
                        fakeSource, fakeChangeStatus, fakeAgeLower, fakeAgeUpper);
                return;
            }

            final AgeSignalsManager manager = AgeSignalsManagerFactory.create(activity);

            // Phase 1: request access (may show the Google-managed sharing prompt).
            manager.requestAgeSignalsAccess(
                    AgeSignalsAccessRequest.builder().setActivity(activity).build())
                .addOnSuccessListener(activity, access -> {
                    int accessStatus = access.ageSignalsStatus() != null
                            ? access.ageSignalsStatus()
                            : AgeSignalsStatus.UNSPECIFIED;

                    if (accessStatus == AgeSignalsStatus.SHARED) {
                        // Phase 2: age signals are shared — fetch them.
                        runCheck(activity, manager, gameObjectName, successMethod, errorMethod);
                    } else {
                        // NOT_SHARED / VERIFICATION_REQUIRED / UNSPECIFIED — no age data.
                        sendAccessOnly(gameObjectName, successMethod, mapAccessStatus(accessStatus));
                    }
                })
                .addOnFailureListener(activity, exception ->
                        sendFailure(gameObjectName, errorMethod, exception));

        } catch (Throwable e) {
            Log.e(TAG, "Failed to create AgeSignalsManager", e);
            sendError(gameObjectName, errorMethod, -100,
                    "Manager creation failed: " + e.getMessage());
        }
    }

    private static void runCheck(
            final Activity activity,
            final AgeSignalsManager manager,
            final String gameObjectName,
            final String successMethod,
            final String errorMethod) {

        manager.checkAgeSignals(AgeSignalsRequest.builder().build())
            .addOnSuccessListener(activity, result -> {
                try {
                    JSONObject json = serialize(result, "SHARED");
                    Log.d(TAG, "Age signals result: " + json.toString());
                    UnityPlayer.UnitySendMessage(gameObjectName, successMethod, json.toString());
                } catch (Exception e) {
                    Log.e(TAG, "Result serialization failed", e);
                    sendError(gameObjectName, errorMethod, -100,
                            "Serialization error: " + e.getMessage());
                }
            })
            .addOnFailureListener(activity, exception ->
                    sendFailure(gameObjectName, errorMethod, exception));
    }

    private static void runFake(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final String fakeSource,
            final String fakeChangeStatus,
            final int fakeAgeLower,
            final int fakeAgeUpper) {
        try {
            Class<?> fakeClass = Class.forName(
                    "com.google.android.play.agesignals.testing.FakeAgeSignalsManager");
            Object fakeInstance = fakeClass.getDeclaredConstructor().newInstance();

            AgeSignalsResult.Builder builder = AgeSignalsResult.builder();
            int source = parseAgeRangeSource(fakeSource);
            if (source >= 0) builder.setAgeRangeSource(source);
            int change = parseSignificantChangeStatus(fakeChangeStatus);
            if (change >= 0) builder.setSignificantChangeStatus(change);
            if (fakeAgeLower >= 0) builder.setAgeLower(fakeAgeLower);
            if (fakeAgeUpper >= 0) builder.setAgeUpper(fakeAgeUpper);

            java.lang.reflect.Method setResult = fakeClass.getMethod(
                    "setNextAgeSignalsResult", AgeSignalsResult.class);
            setResult.invoke(fakeInstance, builder.build());

            AgeSignalsManager manager = (AgeSignalsManager) fakeInstance;
            Log.d(TAG, "Using FakeAgeSignalsManager: source=" + fakeSource
                    + " change=" + fakeChangeStatus
                    + " age=[" + fakeAgeLower + "-" + fakeAgeUpper + "]");

            manager.checkAgeSignals(AgeSignalsRequest.builder().build())
                .addOnSuccessListener(result -> {
                    try {
                        JSONObject json = serialize(result, "SHARED");
                        UnityPlayer.UnitySendMessage(gameObjectName, successMethod, json.toString());
                    } catch (Exception e) {
                        sendError(gameObjectName, errorMethod, -100,
                                "Serialization error: " + e.getMessage());
                    }
                })
                .addOnFailureListener(exception ->
                        sendFailure(gameObjectName, errorMethod, exception));
        } catch (Exception e) {
            Log.e(TAG, "FakeAgeSignalsManager not available (testing artifact may be stripped)", e);
            sendError(gameObjectName, errorMethod, -100,
                    "FakeAgeSignalsManager not available: " + e.getMessage());
        }
    }

    private static JSONObject serialize(AgeSignalsResult result, String accessStatus) throws Exception {
        JSONObject json = new JSONObject();
        json.put("accessStatus", accessStatus);
        json.put("ageLower", result.ageLower() != null ? result.ageLower() : JSONObject.NULL);
        json.put("ageUpper", result.ageUpper() != null ? result.ageUpper() : JSONObject.NULL);
        json.put("ageRangeSource",
                result.ageRangeSource() != null
                        ? mapAgeRangeSource(result.ageRangeSource())
                        : JSONObject.NULL);
        json.put("significantChangeStatus",
                result.significantChangeStatus() != null
                        ? mapSignificantChangeStatus(result.significantChangeStatus())
                        : JSONObject.NULL);
        json.put("installId", result.installId() != null ? result.installId() : JSONObject.NULL);
        json.put("significantChangeApprovalDate",
                result.significantChangeApprovalDate() != null
                        ? result.significantChangeApprovalDate().getTime()
                        : JSONObject.NULL);
        return json;
    }

    private static void sendAccessOnly(String gameObjectName, String successMethod, String accessStatus) {
        try {
            JSONObject json = new JSONObject();
            json.put("accessStatus", accessStatus);
            json.put("ageLower", JSONObject.NULL);
            json.put("ageUpper", JSONObject.NULL);
            json.put("ageRangeSource", JSONObject.NULL);
            json.put("significantChangeStatus", JSONObject.NULL);
            json.put("installId", JSONObject.NULL);
            json.put("significantChangeApprovalDate", JSONObject.NULL);
            Log.d(TAG, "Access-only result: " + json.toString());
            UnityPlayer.UnitySendMessage(gameObjectName, successMethod, json.toString());
        } catch (Exception e) {
            sendError(gameObjectName, successMethod, -100, "Access serialization error: " + e.getMessage());
        }
    }

    private static void sendFailure(String gameObjectName, String errorMethod, Exception exception) {
        int errorCode = -100;
        String errorMessage = exception.getMessage();
        if (exception instanceof AgeSignalsException) {
            errorCode = ((AgeSignalsException) exception).getErrorCode();
        }
        Log.e(TAG, "Age signals error: code=" + errorCode + " msg=" + errorMessage);
        sendError(gameObjectName, errorMethod, errorCode, errorMessage);
    }

    public static void cleanup() {
        Log.d(TAG, "cleanup() called");
    }

    private static String mapAccessStatus(int status) {
        if (status == AgeSignalsStatus.SHARED) return "SHARED";
        if (status == AgeSignalsStatus.NOT_SHARED) return "NOT_SHARED";
        if (status == AgeSignalsStatus.VERIFICATION_REQUIRED) return "VERIFICATION_REQUIRED";
        return "UNSPECIFIED";
    }

    private static String mapAgeRangeSource(int source) {
        if (source == AgeRangeSource.TIER_A) return "TIER_A";
        if (source == AgeRangeSource.TIER_B) return "TIER_B";
        if (source == AgeRangeSource.TIER_C) return "TIER_C";
        if (source == AgeRangeSource.TIER_D) return "TIER_D";
        if (source == AgeRangeSource.UNSPECIFIED) return "UNSPECIFIED";
        Log.w(TAG, "Unknown ageRangeSource: " + source);
        return String.valueOf(source);
    }

    private static int parseAgeRangeSource(String source) {
        if (source == null) return -1;
        switch (source) {
            case "TIER_A": return AgeRangeSource.TIER_A;
            case "TIER_B": return AgeRangeSource.TIER_B;
            case "TIER_C": return AgeRangeSource.TIER_C;
            case "TIER_D": return AgeRangeSource.TIER_D;
            case "UNSPECIFIED": return AgeRangeSource.UNSPECIFIED;
            default: return -1;
        }
    }

    private static String mapSignificantChangeStatus(int status) {
        if (status == SignificantChangeStatus.APPROVED) return "APPROVED";
        if (status == SignificantChangeStatus.PENDING) return "PENDING";
        if (status == SignificantChangeStatus.DECLINED) return "DECLINED";
        if (status == SignificantChangeStatus.UNSPECIFIED) return "UNSPECIFIED";
        Log.w(TAG, "Unknown significantChangeStatus: " + status);
        return String.valueOf(status);
    }

    private static int parseSignificantChangeStatus(String status) {
        if (status == null) return -1;
        switch (status) {
            case "APPROVED": return SignificantChangeStatus.APPROVED;
            case "PENDING": return SignificantChangeStatus.PENDING;
            case "DECLINED": return SignificantChangeStatus.DECLINED;
            case "UNSPECIFIED": return SignificantChangeStatus.UNSPECIFIED;
            default: return -1;
        }
    }

    private static void sendError(String gameObjectName, String errorMethod,
                                   int errorCode, String errorMessage) {
        try {
            JSONObject json = new JSONObject();
            json.put("errorCode", errorCode);
            json.put("errorMessage", errorMessage != null ? errorMessage : "Unknown error");
            json.put("isRetryable", isRetryable(errorCode));
            UnityPlayer.UnitySendMessage(gameObjectName, errorMethod, json.toString());
        } catch (Exception e) {
            Log.e(TAG, "Failed to send error to Unity", e);
        }
    }

    private static boolean isRetryable(int errorCode) {
        return errorCode >= -8 && errorCode <= -1; // -9 (AppNotOwned) and -10 (SdkVersionOutdated) are not retryable
    }
}
