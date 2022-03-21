using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Umbraco9DeployTools.Models
{
    using Dragonfly.NetModels;

    public class ComparisonResults
    {
        public StatusMessage Status { get; set; }
        public Workspace LocalEnvironment { get; set; }
        public Workspace RemoteEnvironment { get; set; }
        public IEnumerable<ContentNodeDataItem> LocalNotRemote { get; set; }
        public IEnumerable<ContentNodeDataItem> RemoteNotLocal { get; set; }
    }
}
