namespace Dragonfly.Umbraco9DeployTools.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class CloudDeployConfig
    {
        [JsonProperty("Deploy", NullValueHandling = NullValueHandling.Ignore)]
        public Deploy Deploy { get; set; }

        [JsonProperty("Identity", NullValueHandling = NullValueHandling.Ignore)]
        public Identity Identity { get; set; }
    }

    public class Deploy
    {
        [JsonProperty("Project", NullValueHandling = NullValueHandling.Ignore)]
        public Project Project { get; set; }

        [JsonProperty("Settings", NullValueHandling = NullValueHandling.Ignore)]
        public Settings Settings { get; set; }
    }

    public class Project
    {
        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("Alias", NullValueHandling = NullValueHandling.Ignore)]
        public string Alias { get; set; }

        [JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? Id { get; set; }

        [JsonProperty("Workspaces", NullValueHandling = NullValueHandling.Ignore)]
        public List<Workspace> Workspaces { get; set; }
    }

    public class Workspace
    {
        [JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? Id { get; set; }

        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("Type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("Url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Url { get; set; }
    }

    public class Settings
    {
        [JsonProperty("ApiKey", NullValueHandling = NullValueHandling.Ignore)]
        public string ApiKey { get; set; }
    }

    public class Identity
    {
        [JsonProperty("ClientId", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? ClientId { get; set; }

        [JsonProperty("ClientSecret", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientSecret { get; set; }

        [JsonProperty("EnvironmentId", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? EnvironmentId { get; set; }

        [JsonProperty("LocalLoginRedirectUri", NullValueHandling = NullValueHandling.Ignore)]
        public Uri LocalLoginRedirectUri { get; set; }
    }
}

