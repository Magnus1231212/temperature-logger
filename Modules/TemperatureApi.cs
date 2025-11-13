using System;
using System.Collections;
using System.Text;
using nanoFramework.Json;
using nanoFramework.WebServer;
using temperature_logger.Models;
using temperature_logger.Modules;

// Alias til nanoFrameworks WebServer-klasse
using NanoWebServer = nanoFramework.WebServer.WebServer;

namespace temperature_logger.Web
{
    public enum TimeSpanKind
    {
        Day,   // Dagligt interval
        Week,  // Ugeintervaller
        Month  // Månedlige intervaller
    }

    public class ChartPoint
    {
        // Tidspunkt (x-akse)
        public DateTime x { get; set; }

        // Temperaturværdi (y-akse)
        public double y { get; set; }
    }

    public static class PeriodHelper
    {
        // Tilføjer måneder uden at skabe ugyldige datoer (f.eks. 30 → 28)
        private static DateTime AddMonthsSafe(DateTime date, int months)
        {
            int year = date.Year;
            int month = date.Month + months;   // Flyt måned

            // Rul måneder frem over årsskifte
            while (month > 12)
            {
                month -= 12;
                year++;
            }

            // Rul måneder tilbage over årsskifte
            while (month < 1)
            {
                month += 12;
                year--;
            }

            int day = date.Day;
            int daysInTargetMonth = DateTime.DaysInMonth(year, month);

            // Undgå datoer der ikke findes i måneden
            if (day > daysInTargetMonth)
                day = daysInTargetMonth;

            // Returner korrigeret dato
            return new DateTime(year, month, day, date.Hour, date.Minute, date.Second);
        }

        // Returnerer start/slut tidspunkt baseret på valgt periode
        public static void GetPeriodRange(TimeSpanKind span, int offset, DateTime now, out DateTime from, out DateTime to)
        {
            switch (span)
            {
                case TimeSpanKind.Day:
                    {
                        // Start kl. 00:00 for den valgte dag
                        DateTime dayStart = new DateTime(now.Year, now.Month, now.Day);

                        // offset = hvor mange dage bagud
                        from = dayStart.AddDays(-offset);

                        // Slut 24 timer senere
                        to = from.AddDays(1);
                        break;
                    }

                case TimeSpanKind.Week:
                    {
                        // Beregn hvilken ugedag det er (mandag = 0)
                        int diff = ((int)now.DayOfWeek + 6) % 7;

                        // Find mandag i denne uge
                        DateTime monday = now.Date.AddDays(-diff);

                        // Flyt uger bagud baseret på offset
                        from = monday.AddDays(-7 * offset);

                        // Slut om 7 dage
                        to = from.AddDays(7);
                        break;
                    }

                case TimeSpanKind.Month:
                default:
                    {
                        // Start på 1. i måneden
                        DateTime first = new DateTime(now.Year, now.Month, 1);

                        // Flyt måneder bagud
                        from = AddMonthsSafe(first, -offset);

                        // Slut ved starten af næste måned
                        to = AddMonthsSafe(from, 1);
                        break;
                    }
            }
        }
    }

    public class ControllerTemperature
    {
        // API-endpoint for temperaturhistorik /api/temperature
        [Route("api/temperature")]
        [Method("GET")]
        public void GetTemperatureHistory(WebServerEventArgs e)
        {
            // Hent hele URL'en inkl. query parameters
            string rawUrl = e.Context.Request.RawUrl;

            // Standardværdier hvis parametre ikke er angivet
            string spanStr = "day";
            string offsetStr = "0";

            // Pars querystring til parametre
            var parameters = NanoWebServer.DecodeParam(rawUrl);

            // Udlæs parametre hvis de findes
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    // Periode-type (day/week/month)
                    if (p.Name == "span")
                        spanStr = p.Value;

                    // Periode-offset (0 = nuværende, 1 = forrige osv.)
                    else if (p.Name == "offset")
                        offsetStr = p.Value;
                }
            }

            // Oversæt tekst til enum
            TimeSpanKind span = spanStr switch
            {
                "week" => TimeSpanKind.Week,
                "month" => TimeSpanKind.Month,
                _ => TimeSpanKind.Day
            };

            // Konverter offset-streng til tal
            int offset = 0;
            int.TryParse(offsetStr, out offset);

            // Hent alle temperaturmålinger fra lagring
            TemperatureReading[] all = JsonStorage.ReadArray<TemperatureReading>("temperature.json");

            // Beregn tidsintervallet der skal vises
            DateTime now = DateTime.UtcNow;
            PeriodHelper.GetPeriodRange(span, offset, now, out DateTime from, out DateTime to);

            // Liste til resultater (skal være ArrayList pga. nanoFramework)
            var list = new ArrayList();

            // Filtrer målinger inden for tidsintervallet
            foreach (var r in all)
            {
                if (r.Timestamp >= from && r.Timestamp < to)
                {
                    // Konverter måling til grafpunkt
                    list.Add(new ChartPoint
                    {
                        x = r.Timestamp,
                        y = r.Temperature
                    });
                }
            }

            // Omform ArrayList → ChartPoint[]
            ChartPoint[] result = (ChartPoint[])list.ToArray(typeof(ChartPoint));

            // Konverter resultat til JSON
            string json = JsonConvert.SerializeObject(result);

            // Angiv indholdstype for svar
            e.Context.Response.ContentType = "application/json";

            // Skriv JSON til HTTP-svar
            NanoWebServer.OutputAsStream(e.Context.Response, json);
        }
    }
}
