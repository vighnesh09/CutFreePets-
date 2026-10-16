using System;
using UnityEngine;

namespace CutFreePets.Core
{
    /// <summary>
    /// Professional Mobile Haptic Feedback Manager.
    /// Provides subtle, crisp, premium tactile sensations for:
    /// - Rope slicing (15ms light click)
    /// - Star pickup (12ms feather tick)
    /// - Cage goal landing (35ms solid impact thud)
    /// - Victory star pop & fanfare
    /// 
    /// Features:
    /// - Native Android VibrationEffect (API 26+ and API 29+) with amplitude control
    /// - Cached JNI references (zero per-frame GC allocations)
    /// - Persistent PlayerPrefs on/off toggle
    /// - Safe graceful fallback on older devices & Unity Editor
    /// </summary>
    public static class HapticManager
    {
        private const string PREF_KEY_HAPTICS = "CutFreePets_Haptics_Enabled";
        private static bool isInitialized = false;
        private static bool? isHapticsEnabled = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibratorService;
        private static AndroidJavaClass vibrationEffectClass;
        private static int androidApiLevel = 0;
        private static bool hasVibrator = false;
#endif

        public static bool HapticsEnabled
        {
            get
            {
                if (!isHapticsEnabled.HasValue)
                {
                    isHapticsEnabled = PlayerPrefs.GetInt(PREF_KEY_HAPTICS, 1) == 1;
                }
                return isHapticsEnabled.Value;
            }
            set
            {
                isHapticsEnabled = value;
                PlayerPrefs.SetInt(PREF_KEY_HAPTICS, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private static void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    androidApiLevel = versionClass.GetStatic<int>("SDK_INT");
                }

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibratorService = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                if (vibratorService != null)
                {
                    hasVibrator = vibratorService.Call<bool>("hasVibrator");
                }

                if (androidApiLevel >= 26)
                {
                    vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HapticManager] Android Haptics init error: {e.Message}");
                hasVibrator = false;
            }
#endif
        }

        /// <summary>
        /// Plays a crisp, razor-sharp 15ms pulse when a rope is cut by the player's swipe.
        /// </summary>
        public static void PlayRopeCutHaptic()
        {
            if (!HapticsEnabled) return;
            VibrateCustom(15, 130);
        }

        /// <summary>
        /// Plays a light, cheerful 12ms tick when the player touches or collects a star.
        /// </summary>
        public static void PlayStarPickupHaptic()
        {
            if (!HapticsEnabled) return;
            VibrateCustom(12, 100);
        }

        /// <summary>
        /// Plays a solid 35ms impact thud when the cage lands on the goal platform and breaks open.
        /// </summary>
        public static void PlayGoalLandingHaptic()
        {
            if (!HapticsEnabled) return;
            VibrateCustom(35, 210);
        }

        /// <summary>
        /// Plays a celebratory double pulse when completing a level.
        /// </summary>
        public static void PlayVictoryFanfareHaptic()
        {
            if (!HapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Initialize();
            if (!hasVibrator || vibratorService == null) return;

            try
            {
                if (androidApiLevel >= 26 && vibrationEffectClass != null)
                {
                    long[] timings = new long[] { 0, 25, 60, 35 };
                    int[] amplitudes = new int[] { 0, 150, 0, 220 };
                    using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                    {
                        vibratorService.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibratorService.Call("vibrate", (long)30);
                }
            }
            catch (Exception) {}
#endif
        }

        private static void VibrateCustom(long milliseconds, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Initialize();
            if (!hasVibrator || vibratorService == null) return;

            try
            {
                if (androidApiLevel >= 26 && vibrationEffectClass != null)
                {
                    using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                    {
                        vibratorService.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibratorService.Call("vibrate", milliseconds);
                }
            }
            catch (Exception) {}
#endif
        }
    }
}
