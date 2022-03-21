using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Umbraco9DeployTools
{
    using Umbraco.Cms.Core.Configuration.Models;

    public class DeployToolsConfig 
    {

        #region Properties

        public Guid Secret { get; set; }
   
     
        public List<EnvironmentHost> EnvironmentHosts { get; set; } = new();

        #endregion

    }

    public class EnvironmentHost
    {
        public string HostName { get; set; }
        public string EnvironmentName { get; set; }
    }
}
