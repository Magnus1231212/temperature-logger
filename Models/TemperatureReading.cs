using System;
using System.Collections.Generic;
using System.Text;

namespace temperature_logger.Models
{
    public class TemperatureReading
    {
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
    }
}
