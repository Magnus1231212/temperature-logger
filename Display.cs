using Iot.Device.Ssd13xx;
using nanoFramework.Hardware.Esp32;
using System.Device.I2c;
using System.Threading;
using Iot.Device.Ssd13xx.Commands;
using temperature_logger.Dep;

namespace temperature_logger
{
    internal class Display
    {
        private static Ssd1306 device;

        // Variabler til temperaturer
        private static double currentTemperature = 21.3;
        private static double desiredTemperature = 22.0;

        public static void DisplayOled()
        {
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);

            SetupOLED();

            Thread.Sleep(Timeout.Infinite);
        }

        // === SETUP METHODE ===
        public static void SetupOLED()
        {
            // Opret SSD1306 display-instans (og gem i felt, så den kan bruges senere)
            device = new Ssd1306(
                I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress)),
                Ssd13xx.DisplayResolution.OLED128x32
            );

            // Klargør displayet
            device.ClearScreen();
            device.Font = new BasicFont();

            // Start med at vise temperaturerne
            ShowTemperatureDisplay();
        }

        // === TEMP DISPLAY METHODE ===
        public static void ShowTemperatureDisplay()
        {
            if (device == null) return;

            device.ClearScreen();
            device.DrawString(2, 2, $"Aktuel: {currentTemperature:F1}°C", 1);
            device.DrawString(2, 14, $"Ønsket: {desiredTemperature:F1}°C", 1);
            device.Display();

            TurnOffDisplay();
        }

        // === SLUK-SKÆRM METHODE ===
        public static void TurnOffDisplay()
        {
            if (device == null) return;

            device.SendCommand(new SetDisplayOff());
        }
    }
}
