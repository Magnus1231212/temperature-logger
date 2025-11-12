using nanoFramework.Device.OneWire;
using nanoFramework.Hardware.Esp32;
using System;
using System.Diagnostics;
using System.Threading;
using temperature_logger.Models;
using temperature_logger.Modules;

namespace temperature_logger
{
    public class Program
    {
        // Setup for device configuration
        public static void Setup()
        {
            // Setup OLED pins
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);

            // Setup 1-Wire pin
            Configuration.SetPinFunction(16, DeviceFunction.COM3_RX);
            Configuration.SetPinFunction(17, DeviceFunction.COM3_TX);
        }

        // The main entry point for the application
        public static void Main()
        {
            Setup();

            //TempSensor.initializeTempSensor();

            //Debug.WriteLine(TempSensor.ReadTemperature().ToString());
            //Display.DisplayOled();
            //Lysdioder.Setup(0.5);
            //UpdateSystem();

            // Create a sample DeviceConfig
            //var cfg = new DeviceConfig
            //{
            //    WifiSSID = "MyNetwork",
            //    WifiPassword = "SuperSecret"
            //};

            //// --- Save the config ---
            //JsonStorage.Save(cfg, FileName);
            //Debug.WriteLine("Config saved successfully.");

            //// --- Load the config ---
            //var loadedCfg = JsonStorage.Load<DeviceConfig>(FileName);
            //if (loadedCfg != null)
            //{
            //    Debug.WriteLine("Config loaded successfully:");
            //    Debug.WriteLine($"SSID: {loadedCfg.WifiSSID}");
            //    Debug.WriteLine($"Password: {loadedCfg.WifiPassword}");
            //}
            //else
            //{
            //    Debug.WriteLine("Failed to load config.");
            //}

            Debug.WriteLine("Hello from nanoFramework!");

            Thread.Sleep(Timeout.Infinite);
        }

        private static void UpdateSystem()
        {
            // Opdater displayet
            Display.ShowTemperatureDisplay();

            // Opdater lysdioder ud fra temperaturforskellen
            Lysdioder.UpdateStatus(Display.CurrentTemperature, Display.DesiredTemperature);
        }

    }
}
