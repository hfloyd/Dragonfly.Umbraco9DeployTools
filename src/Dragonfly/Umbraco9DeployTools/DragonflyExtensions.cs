namespace Dragonfly.Umbraco9DeployTools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dragonfly.NetModels;

    public static class DragonflyExtensions
    {
        public static string GetReadableUtcTimestamp(this DateTime Date, bool ConvertToLocalTime)
        {
            if (Date != default(DateTime))
            {
                if (ConvertToLocalTime)
                {
                    var localTime = Date.ToLocalTime();
                    var localZone = TimeZone.CurrentTimeZone;
                    var zoneString = localZone.IsDaylightSavingTime(localTime) ? localZone.DaylightName : localZone.StandardName;
                    return $"{localTime.ToString("MMM d, yyyy hh:mm:ss tt")} ({zoneString})";
                }
                else
                {
                    return Date.ToString("yyyy-MM-dd-HH-mm-ss-UTC");
                }

            }
            else
            {
                return "NONE";
            }
        }


        //public static bool HasAnyExceptions(this StatusMessage Msg)
        //{
        //    if (Msg.RelatedException != null)
        //    {
        //        return true;
        //    }

        //    if (Msg.InnerStatuses.Any())
        //    {
        //        return Msg.InnerStatuses.Select(n => n.RelatedException).Any(n => n != null);
        //    }

        //    return false;
        //}

        //public static bool HasAnyFailures(this StatusMessage Msg)
        //{
        //    if (!Msg.Success)
        //    {
        //        return true;
        //    }

        //    if (Msg.InnerStatuses.Any())
        //    {
        //        return Msg.InnerStatuses.Any(n => n.Success != true);
        //    }

        //    return false;
        //}

    }
}
