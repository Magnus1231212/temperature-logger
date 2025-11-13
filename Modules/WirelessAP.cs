using Iot.Device.DhcpServer;
using nanoFramework.Runtime.Native;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using nanoFramework.Networking;
using System.Device.Wifi;
using temperature_logger.Dep;

namespace temperature_logger.Modules
{
    public static class AP
    {
        private static WebServer _webServer;

        public static void StartSoftAp()
        {
            try
            {
                // Start access point mode
                string apSsid = "Temperature Logger";

                Wireless80211.Configure(apSsid, null);
                Debug.WriteLine($"[SoftAP] Started '{apSsid}'!");
                Debug.WriteLine("[SoftAP] IP: 192.168.4.1");

                // Start embedded web server for config
                _webServer = new WebServer();
                _webServer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SoftAP] Failed to start SoftAP: {ex.Message}");
            }
        }
    }
}
