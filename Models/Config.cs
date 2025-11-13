using System;
using System.Collections.Generic;
using System.Text;

namespace temperature_logger.Models
{
    public class DeviceConfig
    {
        public string WifiSSID { get; set; }
        public string WifiPassword { get; set; }
        public string MQTTURL { get; set; }
        public string MQTTUsername { get; set; }
        public string MQTTPassword { get; set; }
    }
}
