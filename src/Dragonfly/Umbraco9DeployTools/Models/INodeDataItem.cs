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
        public INodeDataItem ParentNodeInfo { get; set; }
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
        public INodeDataItem ParentNodeInfo { get; set; }
        public int OrderNum { get; set; }
        public int LevelNum { get; set; }
        public int UniversalSortInt { get; set; }


        [Obsolete("Use the ParentNodeInfo property")]
        public Udi ParentNodeUdi { get; set; }

        #endregion
    }
}
