using System;
using System.Diagnostics;
using nanoFramework.Hardware.Esp32;

namespace temperature_logger.Modules
{
    internal static class DeepSleepManager
    {
        // Minutter enheden skal være vågen efter aktivitet
        private const int AWAKE_MINUTES = 3;

        // Minutter enheden sover før auto-wake
        private const int SLEEP_INTERVAL_MINUTES = 5;

        // GPIO-pin som vækker enheden (Reset/Wake = GPIO14)
        private const Sleep.WakeupGpioPin WAKE_GPIO_PIN = Sleep.WakeupGpioPin.Pin14;

        // Årsag til seneste wakeup
        public static Sleep.WakeupCause WakeupCause { get; private set; }

        // Tidspunkt hvor enheden skal gå i sleep
        private static DateTime _sleepDeadlineUtc;

        // Kald ved opstart
        public static void Initialize()
        {
            WakeupCause = Sleep.GetWakeupCause();

            Debug.WriteLine($"[SLEEP] Wakeup: {WakeupCause}");
            ResetAwakeWindow();
        }

        // Forlæng vågnevindue efter aktivitet
        public static void ResetAwakeWindow()
        {
            _sleepDeadlineUtc = DateTime.UtcNow.AddMinutes(AWAKE_MINUTES);

            Debug.WriteLine($"[SLEEP] Awake until: {_sleepDeadlineUtc:O}");
        }

        // Tjek om vi skal gå i sleep
        public static void Tick()
        {
            if (DateTime.UtcNow >= _sleepDeadlineUtc)
            {
                EnterDeepSleep();
            }
        }

        // Gå i deep sleep (vender aldrig tilbage)
        public static void EnterDeepSleep()
        {
            Debug.WriteLine("[SLEEP] Går i deep sleep...");

            Sleep.EnableWakeupByTimer(TimeSpan.FromMinutes(SLEEP_INTERVAL_MINUTES));
            Sleep.EnableWakeupByPin(WAKE_GPIO_PIN, 1);

            Sleep.StartDeepSleep();
        }
    }
}
