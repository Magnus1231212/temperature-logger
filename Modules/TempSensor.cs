using Iot.Device.Ds18b20;
using nanoFramework.Device.OneWire;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace temperature_logger.Modules
{
    internal class TempSensor
    {
        private static Ds18b20 sensor;
        public static void initializeTempSensor()
        {
            // 1-Wire host
            OneWireHost oneWire = new OneWireHost();

            // Create Ds18b20 instance. Passing null for address means "use first found".
            sensor = new Ds18b20(oneWire, null, false, TemperatureResolution.VeryHigh);

            if (!sensor.Initialize())
            {
                Debug.WriteLine("Sensor initialization failed!");
                return;
            }

            Debug.WriteLine($"Is parasite powered? {sensor.IsParasitePowered}");

            // Show address
            string addr = "";
            foreach (var b in sensor.Address)
            {
                addr += b.ToString("X2");
            }
            Debug.WriteLine($"Sensor address: {addr}");
        }

        // Read temperature with averaging over multiple attempts
        public static double ReadTemperature(int attempts = 3)
        {
            double sum = 0;
            int success = 0;

            for (int i = 0; i < attempts; i++)
            {
                if (sensor.TryReadTemperature(out var temp))
                {
                    sum += temp.DegreesCelsius;
                    success++;
                }

                Thread.Sleep(100); // small delay between reads
            }

            return success > 0 ? sum / success : double.NaN;
        }
    }
}
