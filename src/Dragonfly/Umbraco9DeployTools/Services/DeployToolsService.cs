namespace Dragonfly.Umbraco9DeployTools.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Drawing;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Dragonfly.NetModels;
    using Dragonfly.Umbraco9DeployTools.Models;
    using Dragonfly.UmbracoServices;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using NPoco.Expressions;
    using Umbraco.Cms.Core;
    using Umbraco.Cms.Core.Hosting;
    using Umbraco.Cms.Core.Models;
    using Umbraco.Cms.Core.Services;
    using Umbraco.Cms.Core.Models.PublishedContent;
    using Umbraco.Cms.Web.Common;
    using Umbraco.Extensions;

    public class DeployToolsService
    {
        public enum NodesType
        {
            Content,
            Media
        }

        #region Private Vars

        private readonly IHostingEnvironment _HostingEnvironment;
        private readonly UmbracoHelper _UmbracoHelper;
        private readonly DependencyLoader _Dependencies;
        private readonly FileHelperService _FileHelperService;
        private readonly HttpContext _Context;
        private readonly ILogger _logger;
        private readonly ServiceContext _services;

        private CloudDeployConfig _DeployConfig;

        private List<ContentNodeDataItem> _ContentResultsList = new List<ContentNodeDataItem>();
        private int _ContentResultsCounter = 0;

        private List<MediaNodeDataItem> _MediaResultsList = new List<MediaNodeDataItem>();
        private int _MediaResultsCounter = 0;

        //private List<FormsNodeDataItem> _FormsResultsList = new List<FormsNodeDataItem>();
        //private int _FormsResultsCounter = 0;

        #endregion

        public DeployToolsService(DependencyLoader dependencies, ILogger<DeployToolsService> logger)
        {
            _Dependencies = dependencies;
            _HostingEnvironment = dependencies.HostingEnvironment;
            _UmbracoHelper = dependencies.UmbHelper;
            _FileHelperService = dependencies.DragonflyFileHelperService;
            _Context = dependencies.Context;
            _logger = logger;
            _services = dependencies.Services;

            var filePath = "/umbraco-cloud.json";
            _DeployConfig = LoadDeployConfig(filePath);
        }

        internal static string DataPath()
        {
            //var config = Config.GetConfig();
            //return config.GetDataPath();

            return "/App_Data/DragonflyDeployTools/";
        }

        internal static string PluginPath()
        {
            //var config = Config.GetConfig();
            //return config.GetDataPath();

            return "~/App_Plugins/Dragonfly.DeployTools/";
        }

        internal static string TimestringFormat()
        {
            return "yyyy-MM-dd-HH-mm-ss-UTC";
        }

        #region Content Nodes

        private const string CONTENT_FILE_NAME = "ContentNodesData";

        #region READ
        public ContentNodesDataFile AccessContentDataFile(string EnvironmentType)
        {

            var data = new ContentNodesDataFile();
            var environment = LookupEnvironmentByType(EnvironmentType);
            var readResult = ReadContentNodesDataFile(environment, out data);

            return data;
        }

        public StatusMessage FetchRemoteContentNodesData(string EnvironmentType, bool UpdateRemoteDataBeforeFetching = false)
        {
            var status = new StatusMessage(true);

            var targetEnvironment = LookupEnvironmentByType(EnvironmentType);

            if (targetEnvironment == null)
            {
                status.Success = false;
                status.Message = $"No Deploy Workspace found to match type '{EnvironmentType}'.";
                return status;
            }

            var remoteFileAccessUrl = "";
            if (UpdateRemoteDataBeforeFetching)
            {
                remoteFileAccessUrl = ConstructRemoteUrl(targetEnvironment, "ExportContentData?UpdateNow=true&ReturnType=File", true);
            }
            else
            {
                remoteFileAccessUrl = ConstructRemoteUrl(targetEnvironment, "ExportContentData?UpdateNow=false&ReturnType=File", true);
            }

            var localSavePathVirtual = EnvironmentFilePath(NodesType.Content, targetEnvironment);
            var localSavePathPhysical = _FileHelperService.GetMappedPath(localSavePathVirtual);
            status.Message = $"Fetching ContentNodesData from {targetEnvironment.Name} to '{localSavePathVirtual}'";
            status.MessageDetails = localSavePathPhysical;
            status.ObjectName = remoteFileAccessUrl;

            try
            {
                WebClient client = new WebClient();
                var remoteUri = new Uri(remoteFileAccessUrl);
                client.DownloadFile(remoteUri, localSavePathPhysical);
            }
            catch (Exception e)
            {
                status.Success = false;
                status.RelatedException = e;
                // throw;
            }

            return status;
        }

        public ComparisonResults CompareContentNodes(string EnvironmentType)
        {
            var compareModel = new ComparisonResults();
            var status = new StatusMessage(true);

            //Get environments
            var remoteEnvironment = LookupEnvironmentByType(EnvironmentType);

            if (remoteEnvironment == null)
            {
                //throw new Exception($"No Deploy Workspace found to match type '{EnvironmentType}'.");
                status.Success = false;
                status.Message = $"No Deploy Workspace found to match type '{EnvironmentType}'.";
                compareModel.Status = status;
                return compareModel;
            }

            var localEnvironment = GetCurrentEnvironment();

            compareModel.LocalEnvironment = localEnvironment;
            compareModel.RemoteEnvironment = remoteEnvironment;

            //Get local data set
            ContentNodesDataFile localData;
            var statusReadLocal = ReadContentNodesDataFile(localEnvironment, out localData);
            status.InnerStatuses.Add(statusReadLocal);
            //....If no file, try saving.
            var localUdis = localData.ContentNodes.Select(n => n.NodeUdi).ToList();

            //Get remote data set
            ContentNodesDataFile remoteData;
            var statusReadRemote = ReadContentNodesDataFile(remoteEnvironment, out remoteData);
            status.InnerStatuses.Add(statusReadRemote);
            //...If no file, try downloading. If no file on remote, trigger remote Save
            //...  /umbraco/Dragonfly/RemoteDeployTools/TriggerMediaDataSave?Key=xxx
            var remoteUdis = remoteData.ContentNodes.Select(n => n.NodeUdi).ToList();

            //Find Nodes which are local and NOT remote
            var localNotRemote = localUdis.Except(remoteUdis);
            compareModel.LocalNotRemote = localData.ContentNodes.Where(n => localNotRemote.Contains(n.NodeUdi));

            //Find Nodes which are remote and NOT local
            var remoteNotLocal = remoteUdis.Except(localUdis);
            compareModel.RemoteNotLocal = remoteData.ContentNodes.Where(n => remoteNotLocal.Contains(n.NodeUdi));

            //Compare nodes which are on both environments


            compareModel.Status = status;
            return compareModel;
        }

        private StatusMessage ReadContentNodesDataFile(Workspace Environment, out ContentNodesDataFile Data)
        {
            var msg = new StatusMessage();

            var fullFilename = EnvironmentFilePath(NodesType.Content, Environment);

            try
            {
                //Get saved data
                var json = _FileHelperService.GetTextFileContents(fullFilename);
                Data = JsonConvert.DeserializeObject<ContentNodesDataFile>(json);

                msg.Success = true;
                msg.Message = $"Data read from '{fullFilename}'.";
            }
            catch (Exception e)
            {
                msg.Success = false;
                msg.Message = $"Unable to read data from '{fullFilename}'.";
                msg.RelatedException = e;
                Data = null;
            }

            msg.TimestampEnd = DateTime.Now;
            return msg;
        }

        #endregion

        #region WRITE
        public StatusMessage SaveContentNodesData()
        {
            var status = new StatusMessage(true);

            //Setup
            var requestUri = Dragonfly.NetHelpers.Urls.CurrentRequestUri(_Context.Request);
            var hostname = requestUri != null ? requestUri.Host : "UNKNOWN";
            var environment = LookupEnvironment(requestUri);

            status.Message = $"Running SaveContentNodesData() on '{hostname}' [{environment.Name}] environment";

            var results = GetContentNodesData(hostname, environment.Name);

            //Save Results
            status.InnerStatuses.Add(SaveContentData(results, environment));

            //var filename = "";
            //if (status.Success)
            //{
            //    filename = status.ObjectName.Replace(_TesterConfig.GetDataPath(), "");
            //}

            status.TimestampEnd = DateTime.Now;
            status.MessageDetails = $"Operation took {status.TimeDuration().ToString()}";
            return status;
        }

        private StatusMessage SaveContentData(ContentNodesDataFile Data, Workspace Environment)
        {
            var status = new StatusMessage(true);
            status.Message = $"Running SaveContentData() [{Environment.Name}] environment.";
            status.MessageDetails = $"There are {Data.ContentNodes.Count} Content nodes in the Data.";
            //status.RelatedObject = Data;

            var fullFilename = EnvironmentFilePath(NodesType.Content, Environment);

            try
            {
                var json = JsonConvert.SerializeObject(Data);
                status.InnerStatuses.Add(DoFileSave(fullFilename, json));
            }
            catch (Exception e)
            {
                status.Success = false;
                status.Message = $"Unable to save Content data to '{fullFilename}'.";
                status.RelatedException = e;
            }

            status.TimestampEnd = DateTime.Now;
            return status;
        }

        public ContentNodesDataFile GetContentNodesData(string EnvironmentHost, string EnvironmentName)
        {
            var functionName = "Dragonfly.DeployToolsService GetContentNodesData";
            _logger.LogInformation($"{functionName} Started ...");

            var results = new ContentNodesDataFile();
            results.Timestamp = DateTime.Now;
            results.EnvironmentHost = EnvironmentHost;
            results.EnvironmentName = EnvironmentName;


            //CONTENT NODES
            _ContentResultsList = new List<ContentNodeDataItem>();
            _ContentResultsCounter = 0;

            var rootContent = _services.ContentService.GetRootContent();
            if (rootContent != null)
            {
                foreach (var c in rootContent.OrderBy(n => n.SortOrder))
                {
                    _logger.LogDebug("{Function} : {Message} : {NodeId}", functionName, "Starting RecursiveGetContentNodes()", c.Id);
                    RecursiveGetContentNodes(c);
                }
            }
            else
            {
                _logger.LogWarning("{Function} : {Message}", functionName, "ROOT CONTENT IS NULL");
            }

            results.ContentNodes = _ContentResultsList;
            results.TotalContentNodes = _ContentResultsList.Count;

            //Finish up
            var duration = DateTime.Now - results.Timestamp;
            results.TimeToGenerate = duration;
            results.TimeToGenerateDisplay = $"{duration.Hours}:{duration.Minutes}:{duration.Seconds}.{duration.Milliseconds}";

            _logger.LogInformation($"... {functionName}  Completed in {results.TimeToGenerateDisplay }");
            return results;
        }

        protected void RecursiveGetContentNodes(IContent Content)
        {
            var functionName = "Dragonfly.DeployToolsService RecursiveGetContentNodes";

            if (Content != null && !Content.Trashed)
            {
                _logger.LogDebug("{Function} : {Message} : {NodeId}", functionName, "Starting LogContentNode()", Content.Id);
                LogContentNode((Content)Content);

                if (_services.ContentService.HasChildren(Content.Id))
                {
                    var countChildren = _services.ContentService.CountChildren(Content.Id);
                    long xTotalRecs;
                    var allChildren = _services.ContentService.GetPagedChildren(Content.Id, 0, countChildren, out xTotalRecs);

                    foreach (var child in allChildren.OrderBy(n => n.SortOrder))
                    {
                        RecursiveGetContentNodes(child);
                    }
                }
            }
        }

        protected void LogContentNode(IContent Content)
        {
            if (Content != null && Content.Id > 0)
            {
                _ContentResultsCounter += 1;

                //Set basic node info
                var result = new ContentNodeDataItem();
                result.NodeId = Content.Id;
                result.NodeUdi = Content.GetUdi();
                result.NodeName = Content.Name;
                result.ContentTypeAlias = Content.ContentType.Alias;
                result.IsPublished = Content.Published;
                result.LastEditedDate = Content.UpdateDate;
                result.LastEditedByUser = GetUserName(Content.WriterId);
                result.ParentNodeUdi = GetContentNodeUdi(Content.ParentId);
                result.LevelNum = Content.Level;
                result.OrderNum = Content.SortOrder;
                result.UniversalSortInt = _ContentResultsCounter;



                _ContentResultsList.Add(result);
            }
        }
        #endregion

        private Udi GetContentNodeUdi(int ContentId)
        {
            var node = _services.ContentService.GetById(ContentId);
            if (node != null)
            {
                return node.GetUdi();
            }
            else
            {
                return null;
            }

        }

        #endregion

        #region Media Nodes

        private const string MEDIA_FILE_NAME = "MediaNodesData";

        #region READ
        public MediaNodesDataFile AccessMediaDataFile(string EnvironmentType)
        {
            var data = new MediaNodesDataFile();
            var environment = LookupEnvironmentByType(EnvironmentType);
            var readResult = ReadMediaNodesDataFile(environment, out data);
            return data;
        }

        public StatusMessage FetchRemoteMediaNodesData(string EnvironmentType, bool UpdateRemoteDataBeforeFetching = false)
        {
            var status = new StatusMessage(true);

            var targetEnvironment = LookupEnvironmentByType(EnvironmentType);

            if (targetEnvironment == null)
            {
                status.Success = false;
                status.Message = $"No Deploy Workspace found to match type '{EnvironmentType}'.";
                return status;
            }

            var remoteFileAccessUrl = "";
            if (UpdateRemoteDataBeforeFetching)
            {
                remoteFileAccessUrl = ConstructRemoteUrl(targetEnvironment, "ExportMediaData?UpdateNow=true&ReturnType=File", true);
            }
            else
            {
                remoteFileAccessUrl = ConstructRemoteUrl(targetEnvironment, "ExportMediaData?UpdateNow=false&ReturnType=File", true);
            }

            var localSavePathVirtual = EnvironmentFilePath(NodesType.Media, targetEnvironment);
            var localSavePathPhysical = _FileHelperService.GetMappedPath(localSavePathVirtual);
            status.Message = $"Fetching MediaNodesData from {targetEnvironment.Name} to '{localSavePathVirtual}'";
            status.MessageDetails = localSavePathPhysical;
            status.ObjectName = remoteFileAccessUrl;

            try
            {
                WebClient client = new WebClient();
                var remoteUri = new Uri(remoteFileAccessUrl);
                client.DownloadFile(remoteUri, localSavePathPhysical);
            }
            catch (Exception e)
            {
                status.Success = false;
                status.RelatedException = e;
                // throw;
            }

            return status;
        }


        private StatusMessage ReadMediaNodesDataFile(Workspace Environment, out MediaNodesDataFile Data)
        {
            var msg = new StatusMessage();

            var fullFilename = EnvironmentFilePath(NodesType.Media, Environment);
            try
            {
                //Get saved data
                var json = _FileHelperService.GetTextFileContents(fullFilename);
                Data = JsonConvert.DeserializeObject<MediaNodesDataFile>(json);

                msg.Success = true;
                msg.Message = $"Data read from '{fullFilename}'.";
            }
            catch (Exception e)
            {
                msg.Success = false;
                msg.Message = $"Unable to read data from '{fullFilename}'.";
                msg.RelatedException = e;
                Data = null;
            }

            msg.TimestampEnd = DateTime.Now;
            return msg;
        }
        #endregion

        #region WRITE
        public StatusMessage SaveMediaNodesData()
        {
            var status = new StatusMessage(true);

            //Setup
            var requestUri = Dragonfly.NetHelpers.Urls.CurrentRequestUri(_Context.Request);
            var hostname = requestUri != null ? requestUri.Host : "UNKNOWN";
            var environment = LookupEnvironment(requestUri);

            status.Message = $"Running SaveMediaNodesData() on '{hostname}' [{environment.Name}] environment";

            var results = GetMediaNodesData(hostname, environment.Name);

            //Save Results
            status.InnerStatuses.Add(SaveMediaData(results, environment));

            //var filename = "";
            //if (status.Success)
            //{
            //    filename = status.ObjectName.Replace(_TesterConfig.GetDataPath(), "");
            //}

            status.TimestampEnd = DateTime.Now;
            status.MessageDetails = $"Operation took {status.TimeDuration().ToString()}";
            return status;
        }

        private StatusMessage SaveMediaData(MediaNodesDataFile Data, Workspace Environment)
        {
            var status = new StatusMessage(true);
            status.Message = $"Running SaveMediaData() [{Environment.Name}] environment.";
            status.MessageDetails = $"There are {Data.MediaNodes.Count} Media nodes in the Data.";
            //status.RelatedObject = Data;

            var fullFilename = EnvironmentFilePath(NodesType.Media, Environment);

            try
            {
                var json = JsonConvert.SerializeObject(Data);
                status.InnerStatuses.Add(DoFileSave(fullFilename, json));
            }
            catch (Exception e)
            {
                status.Success = false;
                status.Message = $"Unable to save Media data to '{fullFilename}'.";
                status.RelatedException = e;
            }

            status.TimestampEnd = DateTime.Now;
            return status;
        }

        public MediaNodesDataFile GetMediaNodesData(string EnvironmentHost, string EnvironmentName)
        {
            var functionName = "Dragonfly.DeployToolsService GetMediaNodesData";
            _logger.LogInformation($"{functionName} Started ...");

            var results = new MediaNodesDataFile();
            results.Timestamp = DateTime.Now;
            results.EnvironmentHost = EnvironmentHost;
            results.EnvironmentName = EnvironmentName;

            //MEDIA NODES
            _MediaResultsList = new List<MediaNodeDataItem>();
            _MediaResultsCounter = 0;

            var rootMedia = _services.MediaService.GetRootMedia();
            if (rootMedia != null)
            {
                foreach (var m in rootMedia.OrderBy(n => n.SortOrder))
                {
                    _logger.LogDebug("{Function} : {Message} : {NodeId}", functionName, "Starting RecursiveGetMediaNodes()", m.Id);
                    RecursiveGetMediaNodes(m);
                }
            }
            else
            {
                _logger.LogWarning("{Function} : {Message}", functionName, "ROOT MEDIA IS NULL");
            }

            results.MediaNodes = _MediaResultsList;
            results.TotalMediaNodes = _MediaResultsList.Count;



            //Finish up
            var duration = DateTime.Now - results.Timestamp;
            results.TimeToGenerate = duration;
            results.TimeToGenerateDisplay = $"{duration.Hours}:{duration.Minutes}:{duration.Seconds}.{duration.Milliseconds}";

            _logger.LogInformation($"... {functionName}  Completed in {results.TimeToGenerateDisplay }");
            return results;
        }

        protected void RecursiveGetMediaNodes(IMedia Media)
        {
            var functionName = "Dragonfly.DeployToolsService RecursiveGetMediaNodes";

            if (Media != null && !Media.Trashed)
            {
                _logger.LogDebug("{Function} : {Message} : {NodeId}", functionName, "Starting LogMediaNode()", Media.Id);
                LogMediaNode((Media)Media);

                if (_services.MediaService.HasChildren(Media.Id))
                {
                    var countChildren = _services.MediaService.CountChildren(Media.Id);
                    long xTotalRecs;
                    var allChildren = _services.MediaService.GetPagedChildren(Media.Id, 0, countChildren, out xTotalRecs);

                    foreach (var child in allChildren.OrderBy(n => n.SortOrder))
                    {
                        RecursiveGetMediaNodes(child);
                    }
                }
            }
        }

        protected void LogMediaNode(IMedia Media)
        {
            if (Media != null && Media.Id > 0)
            {
                _MediaResultsCounter += 1;

                //Set basic node info
                var result = new MediaNodeDataItem();
                result.NodeId = Media.Id;
                result.NodeUdi = Media.GetUdi();
                result.NodeName = Media.Name;
                result.MediaTypeAlias = Media.ContentType.Alias;
                result.FilePath = Media.HasProperty("umbracoFile") ? (Media.GetValue("umbracoFile") != null ? Media.GetValue("umbracoFile").ToString() : "MISSING") : "N/A";
                result.LastEditedDate = Media.UpdateDate;
                result.LastEditedByUser = GetUserName(Media.WriterId);
                result.ParentNodeUdi = GetMediaNodeUdi(Media.ParentId);
                result.LevelNum = Media.Level;
                result.OrderNum = Media.SortOrder;
                result.UniversalSortInt = _MediaResultsCounter;



                _MediaResultsList.Add(result);
            }
        }
        #endregion

        private Udi GetMediaNodeUdi(int MediaId)
        {
            var node = _services.MediaService.GetById(MediaId);
            if (node != null)
            {
                return node.GetUdi();
            }
            else
            {
                return null;
            }

        }



        #endregion

        #region General Helpers
        private string GetUserName(int UserId)
        {
            var user = _services.UserService.GetUserById(UserId);
            if (user != null)
            {
                return user.Name;
            }
            else
            {
                return "UNKNOWN";
            }

        }

        private StatusMessage DoFileSave(string FullFilename, string Json)
        {
            var msg = new StatusMessage();

            try
            {
                var savedSuccessfully = _FileHelperService.CreateTextFile(FullFilename, Json, true, false);

                if (savedSuccessfully)
                {
                    msg.Success = true;
                    msg.Message = $"Data saved successfully to '{FullFilename}'.";
                    msg.ObjectName = FullFilename;
                }
                else
                {
                    msg.Success = false;
                    msg.Message = $"Unable to save data to '{FullFilename}'.";
                    msg.MessageDetails = "Unknown issue";
                }
            }
            catch (Exception e)
            {
                msg.Success = false;
                msg.Message = $"Unable to save data to '{FullFilename}'.";
                msg.RelatedException = e;
            }

            msg.TimestampEnd = DateTime.Now;
            return msg;
        }

        //private string GetFullFilename(string BaseFilename, string EnvironmentName)
        //{
        //    var filename = EnvironmentName + "_" + BaseFilename;
        //    var ext = filename.EndsWith(".json") ? "" : ".json";
        //    return filename + ext;
        //}
        //private static string BuildFilePath(string Filename)
        //{
        //    var fullFilename = DataPath() + Filename ;

        //    return fullFilename;
        //}

        public string EnvironmentFilePath(NodesType Type, Workspace Environment, bool ReturnFilenameOnly = false)
        {
            var baseFilename = "";
            switch (Type)
            {
                case NodesType.Content:
                    baseFilename = CONTENT_FILE_NAME;
                    break;
                case NodesType.Media:
                    baseFilename = MEDIA_FILE_NAME;
                    break;
            }

            var filename = Environment.Name + "_" + baseFilename;
            var ext = filename.EndsWith(".json") ? "" : ".json";

            if (ReturnFilenameOnly)
            {
                return filename + ext;
            }
            else
            {
                return DataPath() + filename + ext;
            }


        }
        private CloudDeployConfig LoadDeployConfig(string FilePath)
        {
            var functionName = "Dragonfly.DeployToolsService LoadDeployConfig";

            try
            {
                var json = _FileHelperService.GetTextFileContents(FilePath);

                var config = JsonConvert.DeserializeObject<CloudDeployConfig>(json);

                return config;
            }
            catch (Exception ex)
            {
                throw;
                _logger.LogError(ex, "{Function} : {Message}", functionName, $"Unable to load config file '{FilePath}'");
                return new CloudDeployConfig();
            }
        }

        public Workspace GetCurrentEnvironment()
        {
            var requestUri = Dragonfly.NetHelpers.Urls.CurrentRequestUri(_Context.Request);
            var environment = LookupEnvironment(requestUri);

            return environment;
        }

        public List<Workspace> GetAllDeployEnvironments()
        {
            var requestUri = Dragonfly.NetHelpers.Urls.CurrentRequestUri(_Context.Request);
            var workspaces = new List<Workspace>();
            if (_DeployConfig != null)
            {
                workspaces= _DeployConfig.Deploy.Project.Workspaces;
            }

            return workspaces;
        }

        public Workspace LookupEnvironment(Uri EnvironmentUrl)
        {
            if (_DeployConfig != null)
            {
                var matchingWorkspaces = _DeployConfig.Deploy.Project.Workspaces.Where(n => n.Url.Host == EnvironmentUrl.Host);
                if (matchingWorkspaces.Any())
                {
                    return matchingWorkspaces.First();
                }
            }

            //no matches - assume local
            var localWs = new Workspace();
            localWs.Name = "Local";
            localWs.Type = "local";
            localWs.Url = new Uri($"{EnvironmentUrl.Scheme}://{EnvironmentUrl.Host}");
            return localWs;
        }

        public Workspace LookupEnvironmentByType(string EnvironmentType)
        {
            if (_DeployConfig != null)
            {
                var matchingWorkspaces = _DeployConfig.Deploy.Project.Workspaces.Where(n => n.Type == EnvironmentType);
                if (matchingWorkspaces.Any())
                {
                    return matchingWorkspaces.First();
                }
            }

            //Default
            return null;
        }

        public string RetrieveFileContents(NodesType Type, Workspace Environment)
        {
            var localFilePath = EnvironmentFilePath(Type, Environment);
            var fileContents = _FileHelperService.GetTextFileContents(localFilePath);
            return fileContents;
        }

        public bool ValidateApiKey(string ProvidedKey)
        {
            if (_DeployConfig != null)
            {
                var correctKey = _DeployConfig.Deploy.Settings.ApiKey;
                if (correctKey == ProvidedKey)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new Exception("No Deploy Config Loaded");
            }
        }

        private string GetApiKey()
        {
            if (_DeployConfig != null)
            {
                var key = _DeployConfig.Deploy.Settings.ApiKey;
                return key;
            }
            else
            {
                throw new Exception("No Deploy Config Loaded");
            }
        }

        private string ConstructRemoteUrl(Workspace RemoteEnvironment, string RemoteEndPoint, bool AppendApiKey)
        {
            var remoteFileAccessUrl = $"{RemoteEnvironment.Url.AbsoluteUri}umbraco/Dragonfly/RemoteDeployTools/{RemoteEndPoint}";

            if (AppendApiKey)
            {
                var key = GetApiKey();

                if (remoteFileAccessUrl.Contains("?"))
                {
                    return $"{remoteFileAccessUrl}&Key={key}";
                }
                else
                {
                    return $"{remoteFileAccessUrl}?Key={key}";
                }

            }
            return remoteFileAccessUrl;
        }


        #endregion

    }
}
