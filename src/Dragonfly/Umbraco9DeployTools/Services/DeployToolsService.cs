namespace Dragonfly.Umbraco9DeployTools.Services
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Drawing;
    using System.IO;
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

    public partial class DeployToolsService
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


        #region Content Nodes

        //See separate partial file for Content 

        #endregion

        #region Media Nodes

        //See separate partial file for  Media

        #endregion

        #region General INodeDataItem Functions

        private IEnumerable<LocalRemoteComparison> CompareINodeDataItems(INodeDataItem LocalNode, INodeDataItem RemoteNode)
        {
            var comparisons = new List<LocalRemoteComparison>();

            //Compare Props
            var compName = new LocalRemoteComparison();
            compName.PropertyName = "NodeName";
            compName.LocalValue = LocalNode.NodeName;
            compName.RemoteValue = RemoteNode.NodeName;
            compName.Result = LocalNode.NodeName == RemoteNode.NodeName
                ? ComparisonResult.Same
                : ComparisonResult.Different;
            comparisons.Add(compName);

            var compLastEditedDate = new LocalRemoteComparison();
            compLastEditedDate.PropertyName = "LastEditedDate";
            compLastEditedDate.LocalValue = LocalNode.LastEditedDate;
            compLastEditedDate.RemoteValue = RemoteNode.LastEditedDate;
            if (LocalNode.LastEditedDate == RemoteNode.LastEditedDate)
            {
                compLastEditedDate.Result = ComparisonResult.Same;
            }
            else if (LocalNode.LastEditedDate > RemoteNode.LastEditedDate)
            {
                compLastEditedDate.Result = ComparisonResult.DifferentLocalPreferred;
            }
            else
            {
                compLastEditedDate.Result = ComparisonResult.DifferentRemotePreferred;
            }
            comparisons.Add(compLastEditedDate);

            var compLastEditedByUser = new LocalRemoteComparison();
            compLastEditedByUser.PropertyName = "LastEditedByUser";
            compLastEditedByUser.LocalValue = LocalNode.LastEditedByUser;
            compLastEditedByUser.RemoteValue = RemoteNode.LastEditedByUser;
            if (LocalNode.LastEditedByUser == "Administrator" && RemoteNode.LastEditedByUser == "Administrator")
            {
                compLastEditedByUser.Result = ComparisonResult.Same;
            }
            else if (LocalNode.LastEditedByUser == "Administrator" || RemoteNode.LastEditedByUser == "Administrator")
            {
                compLastEditedByUser.Result = ComparisonResult.DifferentIrrelevant;
            }
            else
            {
                compLastEditedByUser.Result = LocalNode.LastEditedByUser == RemoteNode.LastEditedByUser
                ? ComparisonResult.Same
                : ComparisonResult.Different;
            }
            comparisons.Add(compLastEditedByUser);

            var compParentNodeUdi = new LocalRemoteComparison();
            var localParentNodeUdi = LocalNode.ParentNodeInfo != null ? LocalNode.ParentNodeInfo.NodeUdi : LocalNode.ParentNodeUdi;
            var remoteParentNodeUdi = RemoteNode.ParentNodeInfo != null ? RemoteNode.ParentNodeInfo.NodeUdi : RemoteNode.ParentNodeUdi;
            compParentNodeUdi.PropertyName = "ParentNodeUdi";
            compParentNodeUdi.LocalValue = localParentNodeUdi;
            compParentNodeUdi.RemoteValue = remoteParentNodeUdi;
            if (localParentNodeUdi == null && remoteParentNodeUdi == null)
            {
                compParentNodeUdi.Result = ComparisonResult.Same;
            }
            else if (localParentNodeUdi == null || remoteParentNodeUdi == null)
            {
                compParentNodeUdi.Result = ComparisonResult.Different;
            }
            else
            {
                compParentNodeUdi.Result = localParentNodeUdi.ToString() == remoteParentNodeUdi.ToString()
                 ? ComparisonResult.Same
                 : ComparisonResult.Different;
            }
            comparisons.Add(compParentNodeUdi);

            var compOrderNum = new LocalRemoteComparison();
            compOrderNum.PropertyName = "OrderNum";
            compOrderNum.LocalValue = LocalNode.OrderNum;
            compOrderNum.RemoteValue = RemoteNode.OrderNum;
            compOrderNum.Result = LocalNode.OrderNum == RemoteNode.OrderNum
                ? ComparisonResult.Same
                : ComparisonResult.Different;
            comparisons.Add(compOrderNum);


            var compLevelNum = new LocalRemoteComparison();
            compLevelNum.PropertyName = "LevelNum";
            compLevelNum.LocalValue = LocalNode.LevelNum;
            compLevelNum.RemoteValue = RemoteNode.LevelNum;
            compLevelNum.Result = LocalNode.LevelNum == RemoteNode.LevelNum
                ? ComparisonResult.Same
                : ComparisonResult.Different;
            comparisons.Add(compLevelNum);


            return comparisons;

        }

        #endregion

        #region SyncDate Info

        private const string SYNCDATE_FILE_NAME = "SyncDateInfo";

        public StatusMessage SetSyncDate(string EnvironmentType, NodesType Type, out SyncDateInfoFile SyncInfo)
        {
            var status = new StatusMessage(true);
            status.RunningFunctionName = "SetSyncDate";

            //Get remote data set
            SyncDateInfoFile currentData = null;
            var statusRead = ReadSyncDateInfoFile(out currentData);
            status.InnerStatuses.Add(statusRead);

            if (currentData == null)
            {
                //Unable to read file - likely doesn't exist
                currentData = new SyncDateInfoFile();
            }

            var environment = LookupEnvironmentByType(EnvironmentType);
            if (environment == null)
            {
                status.Success = false;
                status.Message = $"No Deploy Workspace found to match type '{EnvironmentType}'.";
                SyncInfo = currentData;
                return status;
            }

            //TODO: Update existing data if exists
            var existingMatchData =
                currentData.Syncs.Where(n => n.RemoteEnvironment.Type == EnvironmentType && n.Type == Type).OrderByDescending(n => n.SyncDate);
            if (existingMatchData.Any())
            {
                var firstMatch = existingMatchData.First();
                var matchStatus = new StatusMessage(true);
                matchStatus.RunningFunctionName = "SetSyncDate";
                matchStatus.Message =
                    $"Existing Sync Date for {Type.ToString()} on '{EnvironmentType}' found: {firstMatch.SyncDate.GetReadableUtcTimestamp(true)}. It will be updated.";
                status.InnerStatuses.Add(matchStatus);

                foreach (var info in existingMatchData)
                {
                    currentData.Syncs.Remove(info);
                }

            }

            var newSync = new SyncDateInfo(environment, Type, DateTime.Now.ToUniversalTime());
            currentData.Syncs.Add(newSync);
            status.Message = $"Sync Date for {Type.ToString()} on '{EnvironmentType}' updated.";

            //Save Results
            var saveStatus = SaveSyncData(currentData);
            status.InnerStatuses.Add(saveStatus);
            if (saveStatus.Success)
            {
                status.Message = $"Sync Date for {Type.ToString()} on '{EnvironmentType}' updated successfully.";
            }
            else
            {
                status.Success = false;
                status.Message = $"There was a problem updating Sync Date for {Type.ToString()} on '{EnvironmentType}'.";
            }

            SyncInfo = currentData;
            return status;
        }

        private StatusMessage SaveSyncData(SyncDateInfoFile Data)
        {
            var status = new StatusMessage(true);
            status.RunningFunctionName = "SaveSyncData";
            status.Message = $"Running {status.RunningFunctionName}.";
            //status.MessageDetails = $"There are {Data.ContentNodes.Count} Content nodes in the Data.";
            //status.RelatedObject = Data;

            var fullFilename = GeneralFilePath(SYNCDATE_FILE_NAME);

            try
            {
                var json = JsonConvert.SerializeObject(Data);
                status.InnerStatuses.Add(DoFileSave(fullFilename, json));
            }
            catch (Exception e)
            {
                status.Success = false;
                status.Message = $"Unable to save Sync data to '{fullFilename}'.";
                status.RelatedException = e;
            }

            status.TimestampEnd = DateTime.Now;
            return status;
        }

        //public object GetAllSyncData()
        //{
        //    return null;
        //}
        public StatusMessage ReadSyncDateInfoFile(out SyncDateInfoFile Data)
        {
            var msg = new StatusMessage(true);
            msg.RunningFunctionName = "ReadSyncDateInfoFile";

            var fullFilename = GeneralFilePath(SYNCDATE_FILE_NAME);

            try
            {
                //Get saved data
                var json = _FileHelperService.GetTextFileContents(fullFilename);
                Data = JsonConvert.DeserializeObject<SyncDateInfoFile>(json);

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

        #region General Helpers

        #region Public
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

        public string GeneralFilePath(string FileName, bool ReturnFilenameOnly = false)
        {

            var ext = FileName.EndsWith(".json") ? "" : ".json";

            if (ReturnFilenameOnly)
            {
                return FileName + ext;
            }
            else
            {
                return DataPath() + FileName + ext;
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
                workspaces = _DeployConfig.Deploy.Project.Workspaces;
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

        public StatusMessage GetLocalFilesInfo(out List<INodesDataFile> DataList)
        {
            DataList = new List<INodesDataFile>();
            var returnStatus = new StatusMessage(true);
            returnStatus.RunningFunctionName = "GetLocalFilesInfo";

            IEnumerable<FileInfo> filesList;
            var statusGetListOfFiles = GetListOfFiles(out filesList);
            returnStatus.InnerStatuses.Add(statusGetListOfFiles);

            if (filesList.Any())
            {
                foreach (var fileInfo in filesList)
                {
                    var addStatus = true;
                    StatusMessage readStatus = new StatusMessage(true);
                    readStatus.RunningFunctionName = "GetLocalFilesInfo";
                    try
                    {
                        //Read file
                        NodesDataFile nodeFile;
                        readStatus = ReadNodesDataFile(fileInfo.FullName, out nodeFile);

                        
                        if (nodeFile.Environment != null)
                        {
                            DataList.Add(nodeFile);
                        }
                        else
                        {
                            addStatus = false;
                        }
                    }
                    catch (Exception e)
                    {
                        readStatus.Success = false;
                        readStatus.Message = $"GetLocalFilesInfo: Failure getting file '{fileInfo.FullName}'.";
                        readStatus.RelatedException = e;
                    }

                    if (addStatus)
                    {
                        returnStatus.InnerStatuses.Add(readStatus);
                    }
                }
            }

            return returnStatus;
        }

        #endregion

        #region Private
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
            msg.ObjectName = "DoFileSave";

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

        private StatusMessage GetListOfFiles(out IEnumerable<FileInfo> FilesList)
        {
            var returnStatusMsg = new StatusMessage(true);
            returnStatusMsg.ObjectName = "GetListOfFiles";

            var filesList = new List<FileInfo>();

            var dirMapped = _FileHelperService.GetMappedPath(DataPath());

            try
            {
                var files = Directory.GetFiles(dirMapped).ToList();
                foreach (var filepath in files)
                {
                    var filename = "";
                    try
                    {
                        //filename = filepath.Replace(dirMapped, "");
                        //   var fileInfo = ParseFilePath(filepath);
                        var fileInfo = new FileInfo(filepath);
                        filesList.Add(fileInfo);
                        //filesList.Add(filename, GetTimestampFromFileName(filename));
                    }
                    catch (Exception e)
                    {
                        var fileMsg = new StatusMessage(false);
                        fileMsg.ObjectName = "GetListOfFiles";
                        returnStatusMsg.Message = $"Error processing file '{filename}'.";
                        fileMsg.RelatedException = e;

                        returnStatusMsg.InnerStatuses.Add(fileMsg);
                    }
                }
            }
            //catch (System.IO.DirectoryNotFoundException missingDirEx)
            //{
            //    //continue
            //}
            catch (Exception e)
            {
                returnStatusMsg.Success = false;
                returnStatusMsg.Message = $"Error accessing files in '{dirMapped}'.";
                returnStatusMsg.RelatedException = e;
            }

            if (filesList.Any())
            {
                returnStatusMsg.Success = true;
                returnStatusMsg.Message = $"{filesList.Count} files found.";
            }
            else
            {
                if (returnStatusMsg.Message == "")
                {
                    returnStatusMsg.Success = false;
                    returnStatusMsg.Message = $"No files found.";
                }
            }

            FilesList = filesList;
            return returnStatusMsg;
        }

        private StatusMessage ReadNodesDataFile(string FullFilePath, out NodesDataFile Data)
        {
            var msg = new StatusMessage(true);
            msg.ObjectName = "ReadNodesDataFile";
            var fullFilename = FullFilePath; //DataPath() + FileName; 

            try
            {
                //Get saved data
                var json = _FileHelperService.GetTextFileContents(fullFilename);
                Data = JsonConvert.DeserializeObject<NodesDataFile>(json);

                msg.Success = true;
                msg.Message = $"Data read from '{fullFilename}'.";
            }
            catch (Exception e)
            {
                msg.Success = false;
                msg.Message = $"Unable to read data from '{fullFilename}'.";
                msg.RelatedException = e;
                Data = new NodesDataFile();
            }

            msg.TimestampEnd = DateTime.Now;
            return msg;
        }

        //private static FileInfo ParseFilePath(string FilePath)
        //{
        //    var pathData = new FileInfo(FilePath);

        //    //Filename & Folder
        //    //var pathDelim = FilePath.Contains("/") ? '/' : '\\';
        //    //var splitFilePath = FilePath.Split(pathDelim).ToList();

        //    //var filename = splitFilePath.Last();
        //    //var pathData = new FileInfo(filename);

        //    //var folderPath = FilePath.Replace(filename, "").Trim();
        //    //if (folderPath != "") { pathData.DirectoryName = folderPath; }

        //    if (filename.Contains("_"))
        //    {
        //        //Extension
        //        var splitExt = filename.Split('.').ToList();
        //        var ext = splitExt.Last();
        //        pathData.Extension = ext;

        //        var filenameStripped = filename.Replace("." + ext, "");
        //        var splitFileName = filenameStripped.Split('_');

        //        //Domain
        //        pathData.Domain = splitFileName[0];

        //        //Timestamp
        //        var filenameDate = splitFileName[1];
        //        var timestampVal = StringToTimestamp(filenameDate);
        //        pathData.Timestamp = timestampVal;

        //        //StartNode
        //        var startNode = splitFileName[2] == "ALL" ? 0 : Convert.ToInt32(splitFileName[2]);
        //        pathData.StartNode = startNode;

        //    }
        //    else
        //    {
        //        pathData = ParseArchiveFilename(FilePath);
        //    }

        //    return pathData;
        //}


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

        #endregion



        #endregion



    }
}
