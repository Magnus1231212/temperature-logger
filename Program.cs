using System;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Device.OneWire;
using temperature_logger.Modules;
using temperature_logger.Modules;
using static temperature_logger.Modules.Display;

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

            TempSensor.initializeTempSensor();

            Debug.WriteLine(TempSensor.ReadTemperature().ToString());
            Display.DisplayOled();
            Lysdioder.Setup(0.5);
            UpdateSystem();



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
