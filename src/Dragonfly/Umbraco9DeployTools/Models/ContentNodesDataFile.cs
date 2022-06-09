namespace Dragonfly.Umbraco9DeployTools.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dragonfly.Umbraco9DeployTools.Services;
    using Newtonsoft.Json;
    using Umbraco.Cms.Core;


    public class ContentNodesDataFile : INodesDataFile
    {
        #region Implementation of INodesDataFile

        public Version GeneratorVersion { get; set; }
        public Workspace Environment { get; set; }
        public DeployToolsService.NodesType Type { get; set; }
        public DateTime TimestampUtc { get; set; }
        public TimeSpan TimeToGenerate { get; set; }
        public int TotalNodes { get; set; }

        #endregion
        public List<ContentNodeDataItem> ContentNodes { get; set; }

        //[Obsolete("Use Environment.Url")]
        //public string EnvironmentHost { get; set; }
        
        //[Obsolete("Use Environment.Name")]
        //public string EnvironmentName { get; set; }

        //[Obsolete("Use .GetTimeToGenerateDisplay() method")]
        //public string TimeToGenerateDisplay { get; set; }
        //public DateTime Timestamp { get; set; }
    }

    public class ContentNodeDataItem : INodeDataItem
    {

        #region Implementation of INodeDataItem
        public string NodeName { get; set; }
        public int NodeId { get; set; }
        public Udi NodeUdi { get; set; }
        public DateTime LastEditedDate { get; set; }
        public string LastEditedByUser { get; set; }
        public NodeDataItem ParentNodeInfo { get; set; }
        public int OrderNum { get; set; }
        public int LevelNum { get; set; }
        public int UniversalSortInt { get; set; }
        
        [Obsolete("Use the ParentNodeInfo property")]
        public Udi ParentNodeUdi { get; set; }
        #endregion

        public bool IsPublished { get; set; }
        public string ContentTypeAlias { get; set; }

        public ContentNodeDataItem(INodeDataItem NodeDataItem)
        {
            this.NodeName = NodeDataItem.NodeName;
            this.NodeId = NodeDataItem.NodeId;
            this.NodeUdi = NodeDataItem.NodeUdi;
            this.LastEditedDate = NodeDataItem.LastEditedDate;
            this.LastEditedByUser = NodeDataItem.LastEditedByUser;
            this.ParentNodeInfo = NodeDataItem.ParentNodeInfo;
            this.OrderNum = NodeDataItem.OrderNum;
            this.LevelNum = NodeDataItem.LevelNum;
            this.UniversalSortInt = NodeDataItem.UniversalSortInt;
            this.ParentNodeUdi = NodeDataItem.ParentNodeUdi;
        }

        public ContentNodeDataItem()
        {
            
        }
    }

}
