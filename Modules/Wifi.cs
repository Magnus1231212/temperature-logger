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
        private const string ConfigFile = "config.json";
        private static WifiAdapter _wifi;

        public static void Initialize()
        {
            try
            {
                Debug.WriteLine("Initializing Wi-Fi...");

                // Load configuration
                var config = JsonStorage.Load<DeviceConfig>(ConfigFile);
                if (config == null || string.IsNullOrEmpty(config.WifiSSID))
                {
                    Debug.WriteLine("Wi-Fi config not found or invalid. Please save one first.");
                    return;
                }

                Debug.WriteLine($"Loaded Wi-Fi config: SSID={config.WifiSSID}");

                // Get Wi-Fi adapter
                var adapters = WifiAdapter.FindAllAdapters();
                if (adapters.Length == 0)
                {
                    Debug.WriteLine("No Wi-Fi adapters found on this device.");
                    return;
                }

                _wifi = adapters[0];

                // Subscribe to event to know when scanning completes
                _wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged;

                // Give hardware a moment to initialize
                Thread.Sleep(5000);

                // Start scan (will trigger connect inside event)
                Debug.WriteLine("Scanning for Wi-Fi networks...");
                _wifi.ScanAsync();

                // Keep alive
                while (true)
                {
                    if (_wifi.ConnectionStatus == WifiConnectionStatus.Connected)
                    {
                        Debug.WriteLine("Connected to Wi-Fi.");
                    }
                    else
                    {
                        Debug.WriteLine("Not connected. Retrying scan...");
                        _wifi.ScanAsync();
                    }

                    Thread.Sleep(30000); // repeat every 30 seconds
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fatal error: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private static void Wifi_AvailableNetworksChanged(WifiAdapter sender, object e)
        {
            try
            {
                Debug.WriteLine("Scan completed — checking for configured network...");

                var report = sender.NetworkReport;
                var config = JsonStorage.Load<DeviceConfig>(ConfigFile);
                if (config == null) return;

                foreach (var net in report.AvailableNetworks)
                {
                    Debug.WriteLine($"Found: {net.Ssid} ({net.SignalBars} bars, RSSI {net.NetworkRssiInDecibelMilliwatts}dBm)");

                    if (net.Ssid == config.WifiSSID)
                    {
                        Debug.WriteLine($"Connecting to {net.Ssid}...");
                        sender.Disconnect();
                        var result = sender.Connect(net, WifiReconnectionKind.Automatic, config.WifiPassword);

                        if (result.ConnectionStatus == WifiConnectionStatus.Success)
                        {
                            Debug.WriteLine("Connected to Wi-Fi successfully!");
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to connect: {result.ConnectionStatus}");
                        }

                        return;
                    }
                }

                Debug.WriteLine($"Network '{config.WifiSSID}' not found in scan.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Wifi_AvailableNetworksChanged] Error: {ex.Message}");
            }
        }
    }
}
