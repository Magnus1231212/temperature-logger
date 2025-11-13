using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using temperature_logger.Models;

namespace temperature_logger.Modules
{
    class WifiManager
    {
        private const string ConfigFile = "config.json";

        public static void Initialize()
        {
            // Try loading Wi-Fi config
            var config = JsonStorage.Load<DeviceConfig>(ConfigFile);

            if (config != null &&
                !string.IsNullOrEmpty(config.WifiSSID) &&
                !string.IsNullOrEmpty(config.WifiPassword))
            {
                Debug.WriteLine($"[WiFi] Found saved SSID '{config.WifiSSID}'. Trying to connect...");
                Wifi.TryConnectToWifi(config);
            }
            else
            {
                Debug.WriteLine("[WiFi] No valid config found. Starting SoftAP setup mode...");
                AP.StartSoftAp();
            }
        }
    }
}
