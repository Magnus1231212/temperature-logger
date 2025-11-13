using System;
using System.Collections.Generic;
using System.Device.Wifi;
using System.Diagnostics;
using System.Text;
using System.Threading;
using temperature_logger.Models;

namespace temperature_logger.Modules
{
    internal class Wifi
    {
        public static void TryConnectToWifi(DeviceConfig config)
        {
            try
            {
                var adapters = WifiAdapter.FindAllAdapters();
                if (adapters.Length == 0)
                {
                    Debug.WriteLine("[WiFi] No adapters found!");
                    return;
                }

                var wifi = adapters[0];
                wifi.Disconnect();

                // Scan for available networks
                wifi.ScanAsync();
                Thread.Sleep(3000);

                var found = false;
                foreach (var net in wifi.NetworkReport.AvailableNetworks)
                {
                    if (net.Ssid == config.WifiSSID)
                    {
                        found = true;
                        var result = wifi.Connect(net, WifiReconnectionKind.Automatic, config.WifiPassword);

                        if (result.ConnectionStatus == WifiConnectionStatus.Success)
                        {
                            Debug.WriteLine($"[WiFi] Connected to {config.WifiSSID}!");

                            var ni = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0];
                            Debug.WriteLine($"[WiFi] IP Address: {ni.IPv4Address}");
                            return;
                        }
                        else
                        {
                            Debug.WriteLine($"[WiFi] Connection failed: {result.ConnectionStatus}");
                            break;
                        }
                    }
                }

                if (!found)
                {
                    Debug.WriteLine($"[WiFi] SSID '{config.WifiSSID}' not found. Starting SoftAP...");
                    AP.StartSoftAp();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WiFi] Exception: {ex.Message}");
                AP.StartSoftAp();
            }
        }
    }
}
