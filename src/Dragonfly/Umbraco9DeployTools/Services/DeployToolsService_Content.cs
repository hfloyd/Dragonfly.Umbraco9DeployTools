namespace Dragonfly.Umbraco9DeployTools.Services
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Dragonfly.NetModels;
    using Dragonfly.Umbraco9DeployTools.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Umbraco.Cms.Core;
    using Umbraco.Cms.Core.Models;
    using Umbraco.Extensions;

    public partial class DeployToolsService
    {
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
            status.RunningFunctionName = "FetchRemoteContentNodesData";
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

        public StatusMessage CompareContentNodes(string EnvironmentType, out ComparisonResults CompareModel)
        {
            CompareModel = new ComparisonResults();
            var status = new StatusMessage(true);
            status.ObjectName = "CompareContentNodes";

            //Get environments
            var remoteEnvironment = LookupEnvironmentByType(EnvironmentType);

            if (remoteEnvironment == null)
            {
                //throw new Exception($"No Deploy Workspace found to match type '{EnvironmentType}'.");
                status.Success = false;
                status.Message = $"No Deploy Workspace found to match type '{EnvironmentType}'.";
                //compareModel.Status = status;
                return status;
            }

            var localEnvironment = GetCurrentEnvironment();

            CompareModel.LocalEnvironment = localEnvironment;
            CompareModel.RemoteEnvironment = remoteEnvironment;

            //Get local data set
            ContentNodesDataFile localData;
            var statusReadLocal = ReadContentNodesDataFile(localEnvironment, out localData);
            status.InnerStatuses.Add(statusReadLocal);
            //....If no file, try saving.
            var localUdis = localData.ContentNodes.Select(n => n.NodeUdi).ToList();
            CompareModel.LocalDataFileInfo = new NodesDataFile(localData);


            //Get remote data set
            ContentNodesDataFile remoteData;
            var statusReadRemote = ReadContentNodesDataFile(remoteEnvironment, out remoteData);
            CompareModel.RemoteDataFileInfo = new NodesDataFile(remoteData);
            status.InnerStatuses.Add(statusReadRemote);
            //...If no file, try downloading. If no file on remote, trigger remote Save
            //...  /umbraco/Dragonfly/RemoteDeployTools/TriggerMediaDataSave?Key=xxx
            var remoteUdis = remoteData.ContentNodes.Select(n => n.NodeUdi).ToList();

            //Find Nodes which are local and NOT remote
            var localNotRemote = localUdis.Except(remoteUdis);
            CompareModel.ContentLocalNotRemote = localData.ContentNodes.Where(n => localNotRemote.Contains(n.NodeUdi));

            //Find Nodes which are remote and NOT local
            var remoteNotLocal = remoteUdis.Except(localUdis);
            CompareModel.ContentRemoteNotLocal = remoteData.ContentNodes.Where(n => remoteNotLocal.Contains(n.NodeUdi));

            //Compare nodes which are on both environments
            var matchingNodes = new List<NodeItemMatches>();
            foreach (var localNode in localData.ContentNodes)
            {
                var remoteMatches = remoteData.ContentNodes.Where(n => n.NodeUdi == localNode.NodeUdi).ToList();

                if (remoteMatches.Any())
                {
                    var remoteNode = remoteMatches.First();

                    var match = new NodeItemMatches();
                    match.LocalNode = localNode;
                    match.RemoteNode = remoteNode;
                    match.Comparisons.AddRange(CompareINodeDataItems(localNode, remoteNode));

                    var compIsPublished = new LocalRemoteComparison();
                    compIsPublished.PropertyName = "IsPublished";
                    compIsPublished.LocalValue = localNode.IsPublished;
                    compIsPublished.RemoteValue = remoteNode.IsPublished;
                    compIsPublished.Result = localNode.IsPublished == remoteNode.IsPublished
                        ? ComparisonResult.Same
                        : ComparisonResult.Different;
                    match.Comparisons.Add(compIsPublished);

                    match.NumberOfDifferences = match.CountDifferences(true);
                    matchingNodes.Add(match);

                }
            }

            CompareModel.ContentMatchingNodes = matchingNodes;

            //CompareModel.Status = status;
            return status;
        }



        private StatusMessage ReadContentNodesDataFile(Workspace Environment, out ContentNodesDataFile Data)
        {
            var msg = new StatusMessage(true);
            msg.RunningFunctionName = "ReadContentNodesDataFile";

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
            status.RunningFunctionName = "SaveContentNodesData()";

            //Setup
            var requestUri = Dragonfly.NetHelpers.Urls.CurrentRequestUri(_Context.Request);
            var hostname = requestUri != null ? requestUri.Host : "UNKNOWN";
            var environment = LookupEnvironment(requestUri);

            status.Message = $"Running {status.RunningFunctionName} on '{hostname}' [{environment.Name}] environment";

            var results = GetContentNodesData(environment);

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
            status.RunningFunctionName = "SaveContentData";
            status.Message = $"Running {status.RunningFunctionName} [{Environment.Name}] environment.";
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

        public ContentNodesDataFile GetContentNodesData(Workspace Environment)
        {
            var functionName = "Dragonfly.DeployToolsService GetContentNodesData";
            _logger.LogInformation($"{functionName} Started ...");

            var results = new ContentNodesDataFile();
            results.GeneratorVersion = DeployToolsPackage.Version;
            results.TimestampUtc = DateTime.UtcNow;
            results.Environment = Environment;
            results.Type = NodesType.Content;

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
            results.TotalNodes = _ContentResultsList.Count;

            //Finish up
            var duration = DateTime.UtcNow - results.TimestampUtc;
            results.TimeToGenerate = duration;

            _logger.LogInformation($"... {functionName}  Completed in {results.GetTimeToGenerateDisplay() }");
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
                var result =new ContentNodeDataItem(GetBasicContentNodeData(Content)) ;
                result.ContentTypeAlias = Content.ContentType.Alias;
                result.IsPublished = Content.Published;
                result.UniversalSortInt = _ContentResultsCounter;

                //Get Parent Node Info
                if (Content.ParentId == -1)
                {
                    //Root
                    var parentInfo = new NodeDataItem();
                    parentInfo.LevelNum = 0;
                    parentInfo.OrderNum = 0;
                    parentInfo.NodeId = Content.ParentId;
                    parentInfo.NodeName = "[CONTENT ROOT]";

                    result.ParentNodeInfo = parentInfo;
                }
                else
                {
                    var parent = _services.ContentService.GetById(Content.ParentId);
                if (parent != null)
                {
                    var parentInfo =new NodeDataItem(GetBasicContentNodeData(parent));
                    result.ParentNodeInfo = parentInfo;
                }
}
                _ContentResultsList.Add(result);
            }
        }

        private INodeDataItem GetBasicContentNodeData(IContent Content)
        {
            var result = new NodeDataItem();
            result.NodeId = Content.Id;
            result.NodeUdi = Content.GetUdi();
            result.NodeName = Content.Name;
            result.LastEditedDate = Content.UpdateDate;
            result.LastEditedByUser = GetUserName(Content.WriterId);
            result.LevelNum = Content.Level;
            result.OrderNum = Content.SortOrder;

            return result;
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


    }
}
