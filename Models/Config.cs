using System;

namespace temperature_logger.Models
{
    public class DeviceConfig
    {
        // WiFi
        public string WifiSSID { get; set; }
        public string WifiPassword { get; set; }

        // MQTT
        public string MQTTHost { get; set; }
        public int MQTTPort { get; set; }
        public string MQTTClientId { get; set; }
        public string MQTTUsername { get; set; }
        public string MQTTPassword { get; set; }
    }
}
