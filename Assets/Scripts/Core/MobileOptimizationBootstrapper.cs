using UnityEngine;

namespace CutFreePets.Core
{
    /// <summary>
    /// Global mobile performance and device settings bootstrapper.
    /// Runs automatically before any scene loads on both Editor and Mobile devices:
    /// 1. Detects native screen refresh rate (60Hz, 90Hz, 120Hz, 144Hz) and unlocks high refresh rates
    /// 2. Disables VSync so targetFrameRate takes full effect on mobile GPUs
    /// 3. Prevents the screen from dimming/sleeping while player is solving puzzles
    /// </summary>
    public static class MobileOptimizationBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeMobileSettings()
        {
            // 1. Detect device native screen refresh rate
            int targetFps = 60;

#if UNITY_2022_2_OR_NEWER
            double maxHz = Screen.currentResolution.refreshRateRatio.value;
            foreach (var res in Screen.resolutions)
            {
                if (res.refreshRateRatio.value > maxHz)
                {
                    maxHz = res.refreshRateRatio.value;
                }
            }
            if (maxHz > 0)
            {
                targetFps = Mathf.RoundToInt((float)maxHz);
            }
#else
            int maxHz = Screen.currentResolution.refreshRate;
            foreach (var res in Screen.resolutions)
            {
                if (res.refreshRate > maxHz)
                {
                    maxHz = res.refreshRate;
                }
            }
            if (maxHz > 0)
            {
                targetFps = maxHz;
            }
#endif

            // Fallback: If device reports 0 or less than 60, default to 60 FPS
            if (targetFps < 60)
            {
                targetFps = 60;
            }

            // Clamp between 60 FPS and 144 FPS
            targetFps = Mathf.Clamp(targetFps, 60, 144);

            Application.targetFrameRate = targetFps;

            // 2. Disable VSync so targetFrameRate governs the loop properly on mobile
            QualitySettings.vSyncCount = 0;

            // 3. Keep the screen awake during gameplay (no dimming while thinking)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log($"<color=cyan>[MobileOptimization]</color> Screen Refresh Rate Detected: {targetFps}Hz | Target FPS set to {targetFps} (VSync: 0, Sleep: NeverSleep)");
        }
    }
}
