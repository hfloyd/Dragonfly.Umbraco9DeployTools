using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Umbraco9DeployTools.Models
{
    public class StandardViewInfo
    {
        /// <summary>
        /// Current version of DeployTools Package
        /// </summary>
        public Version CurrentToolVersion { get; set; }

        /// <summary>
        /// Current running environment
        /// </summary>
        public Workspace CurrentEnvironment { get; set; }
}
}
