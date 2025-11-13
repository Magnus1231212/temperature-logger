using System;
using System.Collections;
using System.Diagnostics;
using nanoFramework.Json;

namespace temperature_logger.Modules
{
    internal static class Datalogger
    {
        // Filnavne til lokale datafiler
        private const string MeasurementsFile = "measurements.json";
        private const string SentFile = "sent_measurements.json";
        private const string TopicPrefix = "home/thermostat"; // MQTT-emne-prefix

        // Klasse der beskriver én måling
        public class Measurement
        {
            public string ts { get; set; }    // Tidsstempel (ISO-format)
            public double temp { get; set; }  // Aktuel temperatur
            public double desired { get; set; } // Ønsket temperatur
            public string dev { get; set; }   // Enheds-id
        }

        // Tilføjer én ny måling til den lokale JSON-fil
        public static void AppendMeasurement(double temperature, double desired, string deviceId)
        {
            try
            {
                var m = new Measurement
                {
                    ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), // Gem tid i UTC-format
                    temp = ((int)(temperature * 100)) / 100.0,              // Afrund til 2 decimaler
                    desired = ((int)(desired * 100)) / 100.0,
                    dev = deviceId ?? "thermo-unknown"                     // Fallback hvis ingen ID
                };

                JsonStorage.Append(m, MeasurementsFile); // Gem i measurements.json

                Debug.WriteLine($"[Datalogger] Appended measurement {m.ts} temp={m.temp}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] AppendMeasurement error: {ex.Message}");
            }
        }

        // Læser alle gemte målinger (returnerer tomt array hvis ingen findes)
        public static Measurement[] ReadMeasurements()
        {
            try
            {
                return JsonStorage.ReadArray<Measurement>(MeasurementsFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] ReadMeasurements error: {ex.Message}");
                return new Measurement[0];
            }
        }

        // Forsøger at sende alle lokale målinger via en publisher-funktion
        public static int SyncPending(Func<string, string, bool> publisher, string overrideDeviceId = null)
        {
            if (publisher == null) return 0;
            int sentCount = 0; // Antal målinger sendt

            try
            {
                var items = ReadMeasurements();
                if (items == null || items.Length == 0) return 0;

                var remaining = new ArrayList(); // Liste med målinger der ikke blev sendt

                foreach (var item in items)
                {
                    var topic = $"{TopicPrefix}/{(overrideDeviceId ?? item.dev)}/telemetry"; // MQTT-emne
                    string payload;

                    try
                    {
                        payload = JsonConvert.SerializeObject(item); // Konverter objekt til JSON
                        // eller JsonSerializer.SerializeObject(item) afhængig af version
                    }
                    catch
                    {
                        Debug.WriteLine("[Datalogger] Failed to serialize measurement -> skip");
                        continue;
                    }

                    bool ok = false;
                    try
                    {
                        ok = publisher(topic, payload); // Forsøg at sende via publisher
                    }
                    catch (Exception exPub)
                    {
                        Debug.WriteLine($"[Datalogger] Publisher threw: {exPub.Message}");
                        ok = false;
                    }

                    if (ok)
                    {
                        try { JsonStorage.Append(item, SentFile); } catch { } // Gem som "sendt"
                        sentCount++;
                    }
                    else
                    {
                        remaining.Add(item); // Behold målingen til senere
                    }
                }

                try
                {
                    var remArr = (Measurement[])remaining.ToArray(typeof(Measurement));
                    JsonStorage.Save(remArr, MeasurementsFile); // Gem kun de resterende (usendte)
                }
                catch (Exception exSave)
                {
                    Debug.WriteLine($"[Datalogger] Failed to save remaining measurements: {exSave.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] SyncPending error: {ex.Message}");
            }

            Debug.WriteLine($"[Datalogger] SyncPending sent {sentCount}");
            return sentCount;
        }

        // Begræns antallet af målinger der gemmes lokalt (fjerner ældste)
        public static void TrimToMostRecent(int maxItems)
        {
            if (maxItems <= 0) return;
            try
            {
                var all = ReadMeasurements();
                if (all.Length <= maxItems) return;

                int start = all.Length - maxItems; // Startindeks for nyeste målinger
                var trimmed = new Measurement[maxItems];
                Array.Copy(all, start, trimmed, 0, maxItems); // Kopiér kun de nyeste
                JsonStorage.Save(trimmed, MeasurementsFile); // Gem igen med færre elementer
                Debug.WriteLine($"[Datalogger] Trimmed to {maxItems}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Datalogger] TrimToMostRecent error: {ex.Message}");
            }
        }
    }
}
