namespace Dragonfly.Umbraco9DeployTools.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Umbraco.Cms.Core;


    public class ContentNodesDataFile
    {
        public string EnvironmentHost { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan TimeToGenerate { get; set; }
        public string TimeToGenerateDisplay { get; set; }
        public int TotalContentNodes { get; set; }
        public List<ContentNodeDataItem> ContentNodes { get; set; }
        public string EnvironmentName { get; set; }
    }

    public class ContentNodeDataItem : INodeDataItem
    {
        public string NodeName { get; set; }
        public bool IsPublished { get; set; }
        public int NodeId { get; set; }
        public Udi NodeUdi { get; set; }
        public string ContentTypeAlias { get; set; }
        public DateTime LastEditedDate { get; set; }
        public string LastEditedByUser { get; set; }
        public Udi ParentNodeUdi { get; set; }
        public int OrderNum { get; set; }
        public int LevelNum { get; set; }
        public int UniversalSortInt { get; set; }
    }

}
