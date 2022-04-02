using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Umbraco9DeployTools.Models
{
    using Dragonfly.Umbraco9DeployTools.Services;

    public interface INodesDataFile
    {
        public Version GeneratorVersion { get; set; }
        public Workspace Environment { get; set; }
        public DeployToolsService.NodesType Type { get; set; }
        public DateTime TimestampUtc { get; set; }
        public TimeSpan TimeToGenerate { get; set; }
        public int TotalNodes { get; set; }

        //[Obsolete("Use .Environment.Url")]
        //public string EnvironmentHost { get; set; }

        //[Obsolete("Use .Environment.Name")]
        //public string EnvironmentName { get; set; }

        //[Obsolete("Use .GetTimeToGenerateDisplay() method")]
        //public string TimeToGenerateDisplay { get; set; }

        //[Obsolete("Use .TimestampUtc Property and .GetReadableTimestamp() method")]
        //public DateTime TimestampLocal { get; set; }

    }

    public static class INodesDataFileExtensions
    {
        public static string GetTimeToGenerateDisplay(this INodesDataFile DataFile)
        {
            if (DataFile.TimeToGenerate != null && DataFile.TimeToGenerate!= TimeSpan.MinValue)
            {
                return
                    $"{DataFile.TimeToGenerate.Hours} hrs {DataFile.TimeToGenerate.Minutes} mins {DataFile.TimeToGenerate.Seconds}.{DataFile.TimeToGenerate.Milliseconds} secs";
            }
            else
            {
                return "Unknown";
            }
        }

        public static string GetReadableTimestamp(this INodesDataFile DataFile, bool ConvertToLocalTime)
        {
            if (DataFile.TimestampUtc != null && DataFile.TimestampUtc != DateTime.MinValue)
            {
                if (ConvertToLocalTime)
                {
                    var localTime = DataFile.TimestampUtc.ToLocalTime();
                    var localZone = TimeZone.CurrentTimeZone;
                    var zoneString = localZone.IsDaylightSavingTime(localTime) ? localZone.DaylightName : localZone.StandardName;
                    return $"{localTime.ToString("MMM d, yyyy hh:mm:ss tt")} ({zoneString})";
                }
                else
                {
                    return DataFile.TimestampUtc.ToString("yyyy-MM-dd-HH-mm-ss-UTC");
                }
                
            }
            else
            {
                return "Unknown";
            }
        }
    }

    public class NodesDataFile : INodesDataFile
    {
        #region Implementation of INodesDataFile

        public Version GeneratorVersion { get; set; }
        public Workspace Environment { get; set; }
        public DeployToolsService.NodesType Type { get; set; }
        public DateTime TimestampUtc { get; set; }
        public TimeSpan TimeToGenerate { get; set; }
        public int TotalNodes { get; set; }

        //[Obsolete("Use Environment.Url")]
        //public string EnvironmentHost { get; set; }

        //[Obsolete("Use Environment.Name")]
        //public string EnvironmentName { get; set; }

        //public DateTime Timestamp { get; set; }

        #endregion

        public NodesDataFile()
        {

        }

        public NodesDataFile(INodesDataFile DataFile)
        {
            this.GeneratorVersion = DataFile.GeneratorVersion;
            this.TimeToGenerate = DataFile.TimeToGenerate;
            this.TimestampUtc = DataFile.TimestampUtc;
            this.TotalNodes = DataFile.TotalNodes;
            this.Type = DataFile.Type;
            this.Environment = DataFile.Environment;

            ////Obsolete
            //this.EnvironmentName = DataFile.EnvironmentName;
            //this.EnvironmentHost = DataFile.EnvironmentHost;

            //if (DataFile.Environment == null)
            //{
            //    //Faux workspace
            //    var ws = new Workspace();
            //    ws.Url = new Uri(DataFile.EnvironmentHost);
            //    ws.Name = DataFile.EnvironmentName;
            //    ws.Type = "UNKNOWN";

            //    this.Environment = ws;
            //}


        }

    }

}
