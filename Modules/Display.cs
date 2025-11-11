using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Commands;
using nanoFramework.Hardware.Esp32;
using System.Device.I2c;
using System.Threading;
using temperature_logger.Dep;

namespace temperature_logger.Modules
{
    internal class Display
    {
        // Objekt der repræsenterer det fysiske OLED-display (SSD1306)
        private static Ssd1306 device;

        // Aktuel temperatur målt af sensoren
        public static double CurrentTemperature { get; set; } = 22.0;

        // Ønsket temperatur som brugeren har indstillet via knapper
        public static double DesiredTemperature { get; set; } = 22.0;

        public static void DisplayOled()
        {
            // Klargør og initialiserer selve OLED-displayet
            SetupOLED();
        }

        public static void SetupOLED()
        {
            // Opretter en ny instans af SSD1306-displayet via I2C-bussen
            device = new Ssd1306(
                I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress)),
                Ssd13xx.DisplayResolution.OLED128x32
            );

            // Rydder displayet og vælger standardfont
            device.ClearScreen();
            device.Font = new BasicFont();

            // Viser de første temperaturværdier på skærmen
            ShowTemperatureDisplay();
        }

        public static void ShowTemperatureDisplay()
        {
            // Sørger for, at displayet er initialiseret, før der skrives til det
            if (device == null) return;

            // Ryd skærmen før ny tekst skrives
            device.ClearScreen();

            // Skriver både den aktuelle og ønskede temperatur på OLED-displayet
            device.DrawString(2, 2, $"Aktuel: {CurrentTemperature:F1}°C", 1);
            device.DrawString(2, 14, $"Ønsket: {DesiredTemperature:F1}°C", 1);

            // Sender den nye buffer til displayet, så teksten vises
            device.Display();
        }

        public static void TurnOffDisplay()
        {
            // Slukker for displayet, hvis det er tændt
            if (device == null) return;
            device.ClearScreen();
            device.SendCommand(new SetDisplayOff());
        }

        public static void TurnOnDisplay()
        {
            // Tænder for displayet igen og viser temperaturerne
            if (device == null) return;
            device.SendCommand(new SetDisplayOn());
            ShowTemperatureDisplay();
        }
    }
}
