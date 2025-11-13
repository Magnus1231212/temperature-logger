using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using nanoFramework.Json;
using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
using temperature_logger.Models;

namespace temperature_logger.Modules
{
    internal static class Datalogger
    {
        private const string ConfigFile = "config.json";

        private static readonly DeviceConfig _config =
            JsonStorage.Load<DeviceConfig>(ConfigFile);

        // Filnavne til lokale datafiler
        private const string MeasurementsFile = "measurements.json";
        private const string SentFile = "sent_measurements.json";
        private const string TopicPrefix = "home/thermostat";

        private static MqttClient _mqtt;

        // Sørger for at MQTT-klienten er forbundet (med TLS, men uden cert-validering)
        private static bool EnsureMqttConnected()
        {
            // Vi antager at _config er korrekt sat (ingen fallback)
            try
            {
                if (_mqtt != null && _mqtt.IsConnected)
                    return true;

                _mqtt = new MqttClient(
                    _config.MQTTHost,
                    _config.MQTTPort,
                    true,      // TLS slået til
                    null,      // ingen CA-cert (simpelt setup)
                    null,      // ingen client-cert
                    MqttSslProtocols.TLSv1_2);

                _mqtt.Connect(_config.MQTTClientId, _config.MQTTUsername, _config.MQTTPassword);

                if (!_mqtt.IsConnected)
                {
                    Debug.WriteLine("[Datalogger] MQTT TLS connect failed");
                    return false;
                }

                Debug.WriteLine("[Datalogger] MQTT connected (TLS)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] EnsureMqttConnected error: {ex.Message}");
                return false;
            }
        }

        // Sender én payload via MQTT
        private static bool PublishMqtt(string topic, string payload)
        {
            try
            {
                if (!EnsureMqttConnected())
                    return false;

                _mqtt.Publish(
                    topic,
                    Encoding.UTF8.GetBytes(payload));

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] MQTT publish failed: {ex.Message}");
                return false;
            }
        }

        // Tilføjer én ny måling til den lokale JSON-fil
        public static void AppendMeasurement(double temperature)
        {
            try
            {
                var reading = new TemperatureReading
                {
                    Timestamp = DateTime.UtcNow,
                    Temperature = ((int)(temperature * 100)) / 100.0 // 2 decimaler
                };

                JsonStorage.Append(reading, MeasurementsFile);

                Debug.WriteLine($"[Datalogger] Appended {reading.Timestamp:o} temp={reading.Temperature}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] AppendMeasurement error: {ex.Message}");
            }
        }

        // Læs alle gemte målinger
        public static TemperatureReading[] ReadMeasurements()
        {
            try
            {
                return JsonStorage.ReadArray<TemperatureReading>(MeasurementsFile);
            }
            catch
            {
                return new TemperatureReading[0];
            }
        }

        // Sender alt der ikke er sendt endnu
        public static int SyncPending()
        {
            int sentCount = 0;

            try
            {
                var items = ReadMeasurements();

                if (items.Length == 0) return 0;

                var remaining = new ArrayList();

                foreach (var item in items)
                {
                    // Brug clientId fra config
                    var topic = $"{TopicPrefix}/{_config.MQTTClientId}/telemetry";

                    string payload;
                    try
                    {
                        payload = JsonConvert.SerializeObject(item);
                    }
                    catch
                    {
                        Debug.WriteLine("[Datalogger] JSON serialize failed");
                        continue;
                    }

                    bool ok = PublishMqtt(topic, payload);

                    if (ok)
                    {
                        JsonStorage.Append(item, SentFile);
                        sentCount++;
                    }
                    else
                    {
                        remaining.Add(item);
                    }
                }

                // Gem de målinger der ikke blev sendt
                var remArr = (TemperatureReading[])remaining.ToArray(typeof(TemperatureReading));
                JsonStorage.Save(remArr, MeasurementsFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] SyncPending error: {ex.Message}");
            }

            Debug.WriteLine($"[Datalogger] SyncPending sent {sentCount}");
            return sentCount;
        }

        // Begræns lokal historik
        public static void TrimToMostRecent(int maxItems)
        {
            var all = ReadMeasurements();
            if (all.Length <= maxItems) return;

            int start = all.Length - maxItems;
            var trimmed = new TemperatureReading[maxItems];

            Array.Copy(all, start, trimmed, 0, maxItems);

            JsonStorage.Save(trimmed, MeasurementsFile);
        }
    }
}
