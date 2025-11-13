using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using temperature_logger.Models;

namespace temperature_logger.Modules
{
    internal class Config
    {
        private const string ConfigFile = "config.json";
        public static DeviceConfig Get()
        {
            var config = JsonStorage.Load<DeviceConfig>(ConfigFile);
            if (config == null)
            {
                Debug.WriteLine("Could not load config file!");
            }
            return config;
        }

        public static void Save(DeviceConfig config)
        {
            JsonStorage.Save(config, ConfigFile);
        }

        public static void Clear()
        {
            JsonStorage.Delete(ConfigFile);
        }

    }
}
