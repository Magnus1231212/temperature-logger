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

            // Setup Tempsensor
            TempSensor.initializeTempSensor();

            // Setup knapper
            Knapper.Setup(getCurrentTemperature: () => TempSensor.ReadTemperature());

            // Setup display
            Display.DisplayOled();

            // Setup lysdioder
            Lysdioder.Setup(0.5);

            // Init WiFi
            WifiManager.Initialize();
        }

        // The main entry point for the application
        public static void Main()
        {
            // Setup device configuration
            Setup();

            Debug.WriteLine("Hello from nanoFramework!");

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
