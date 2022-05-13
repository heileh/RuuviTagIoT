using System;

namespace Ruuvi.Data.Functions.Misc
{
    public static class Extensions
    {
        public static DateTime FromUnixTimeToDateTimeUtc(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var utc = epoch.AddMilliseconds(unixTime);
            return utc;
        }
    }
}
