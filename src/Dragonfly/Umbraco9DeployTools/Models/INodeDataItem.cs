
namespace Dragonfly.Umbraco9DeployTools.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Umbraco.Cms.Core;

    public interface INodeDataItem
    {
     public string NodeName { get; set; }
        public int NodeId { get; set; }
        public Udi NodeUdi { get; set; }
        public DateTime LastEditedDate { get; set; }
        public string LastEditedByUser { get; set; }
        public Udi ParentNodeUdi { get; set; }
        public int OrderNum { get; set; }
        public int LevelNum { get; set; }
        public int UniversalSortInt { get; set; }
    }
}
