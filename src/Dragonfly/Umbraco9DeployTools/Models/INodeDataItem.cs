namespace Dragonfly.Umbraco9DeployTools.Models
{
    using System;
    using Umbraco.Cms.Core;

    public interface INodeDataItem
    {
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
    }

    public class NodeDataItem : INodeDataItem
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

        public NodeDataItem() { }

        public NodeDataItem(INodeDataItem NodeDataItem)
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
    }
}

