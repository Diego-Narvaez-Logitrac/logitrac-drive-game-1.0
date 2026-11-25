using UnityEngine;

namespace LogitracDrive
{
    /// <summary>App-wide startup settings for device builds.</summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Application.targetFrameRate = 60;               // mobile defaults to 30
            Screen.sleepTimeout = SleepTimeout.SystemSetting; // only the race keeps the screen awake
        }
    }
}
