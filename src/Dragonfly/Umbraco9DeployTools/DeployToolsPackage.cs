namespace Dragonfly.Umbraco9DeployTools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Umbraco.Cms.Core.Configuration.Models;
    using Umbraco.Cms.Core.Semver;

    /// <summary>
    /// Static class with various information and constants about the package.
    /// </summary>
    public class DeployToolsPackage 
    {
        /// <summary>
        /// Gets the alias of the package.
        /// </summary>
        public const string Alias = "Dragonfly.Umbraco9DeployTools";

        /// <summary>
        /// Gets the friendly name of the package.
        /// </summary>
        public const string Name = "Dragonfly Deploy Tools (for Umbraco 9)";

        /// <summary>
        /// Gets the version of the package.
        /// </summary>
        public static readonly Version Version = typeof(DeployToolsPackage).Assembly.GetName().Version;

        /// <summary>
        /// Gets the URL of the GitHub repository for this package.
        /// </summary>
        public const string GitHubUrl = "https://github.com/hfloyd/Dragonfly.Umbraco9DeployTools";

        /// <summary>
        /// Gets the URL of the issue tracker for this package.
        /// </summary>
        public const string IssuesUrl = "https://github.com/hfloyd/Dragonfly.Umbraco9DeployTools/issues";

        /// <summary>
        /// Gets the URL of the documentation for this package.
        /// </summary>
        public const string DocumentationUrl = "https://github.com/hfloyd/Dragonfly.Umbraco9DeployTools#documentation";



    }


}
