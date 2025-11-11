using System;
using System.Device.Gpio;
using System.Diagnostics;

namespace temperature_logger.Modules
{
    internal static class Lysdioder
    {
        // De tre pins der styrer de fysiske lysdioder (rød, grøn og blå)
        private const int RED_PIN = 33;
        private const int GREEN_PIN = 32;
        private const int BLUE_PIN = 25;

        // Hvor stor forskel der skal være mellem ønsket og aktuel temperatur,
        // før rød/blå diode aktiveres. (Toleranceområde for grøn)
        private static double _tolerance = 0.5;

        // GPIO-controller bruges til at styre ESP32’ens udgange
        private static GpioController _gpio;

        // Flag der viser om lysdioderne allerede er sat op (forhindrer fejl)
        private static bool _isInitialized = false;

        public static void Setup(double toleranceDegreesC = 0.5)
        {
            // Hvis dioderne allerede er sat op, justeres kun tolerance og metoden returnerer
            if (_isInitialized)
            {
                _tolerance = toleranceDegreesC;
                return;
            }

            // Initialisering af GPIO-controlleren
            _tolerance = toleranceDegreesC;
            _gpio = new GpioController();

            // Åbn og klargør hver pin som output
            OpenPinSafe(RED_PIN);
            OpenPinSafe(GREEN_PIN);
            OpenPinSafe(BLUE_PIN);

            // Sluk alle LED’er ved start
            AllOff();
            _isInitialized = true;
            Debug.WriteLine("[LED] Setup done");
        }

        // Åbner en GPIO-pin som output, og sørger for at den lukkes korrekt hvis den allerede er åben
        private static void OpenPinSafe(int pin)
        {
            try
            {
                if (_gpio.IsPinOpen(pin))
                {
                    _gpio.ClosePin(pin);
                }
            }
            catch
            {
                // Nogle drivere understøtter ikke IsPinOpen, så vi ignorerer fejlen
            }

            // Åbn pin som output og sæt den lav (slukket)
            _gpio.OpenPin(pin, PinMode.Output);
            _gpio.Write(pin, PinValue.Low);
        }

        // Slukker alle lysdioder
        public static void AllOff()
        {
            if (!_isInitialized) return;

            _gpio.Write(RED_PIN, PinValue.Low);
            _gpio.Write(GREEN_PIN, PinValue.Low);
            _gpio.Write(BLUE_PIN, PinValue.Low);
        }

        // Vælger hvilken diode der skal lyse ud fra temperaturforskellen
        public static void UpdateStatus(double currentTemperature, double desiredTemperature)
        {
            if (!_isInitialized) return;

            // Udregn absolut forskel mellem aktuel og ønsket temperatur
            var absDiff = Math.Abs(currentTemperature - desiredTemperature);

            // Sluk alt inden vi tænder den rigtige diode
            AllOff();

            if (absDiff <= _tolerance)
            {
                // Temperaturen er inden for tolerance → grøn diode tændes
                _gpio.Write(GREEN_PIN, PinValue.High);
            }
            else if (currentTemperature < desiredTemperature)
            {
                // Temperaturen er lavere end ønsket → rød diode tændes
                _gpio.Write(RED_PIN, PinValue.High);
            }
            else
            {
                // Temperaturen er højere end ønsket → blå diode tændes
                _gpio.Write(BLUE_PIN, PinValue.High);
            }
        }

        // Tillader at justere tolerance under drift
        public static void SetTolerance(double toleranceDegreesC)
        {
            _tolerance = toleranceDegreesC;
        }

        // Frigiver pins og rydder op (kan bruges ved fx deep sleep)
        public static void Dispose()
        {
            if (!_isInitialized) return;

            try
            {
                AllOff();
                _gpio.ClosePin(RED_PIN);
                _gpio.ClosePin(GREEN_PIN);
                _gpio.ClosePin(BLUE_PIN);
                _gpio.Dispose();
            }
            finally
            {
                _isInitialized = false;
            }
        }
    }
}
