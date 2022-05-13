using System;

namespace Ruuvi.Data.Functions.Models
{
    public class Measurement
    {
        public DateTime Time { get; set; }
        public string Name { get; set; }
        public bool Movement { get; set; }
        public double Temperature { get; set; }

    }
}
