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
            status.ObjectName = "FetchRemoteMediaNodesData";
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

        public StatusMessage CompareMediaNodes(string EnvironmentType, out ComparisonResults CompareModel)
        {
            CompareModel = new ComparisonResults();

            throw new NotImplementedException();
        }


        private StatusMessage ReadMediaNodesDataFile(Workspace Environment, out MediaNodesDataFile Data)
        {
            var msg = new StatusMessage(true);
            msg.ObjectName = "ReadMediaNodesDataFile";

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
            status.RunningFunctionName = "SaveMediaNodesData()";

            //Setup
            var requestUri = Dragonfly.NetHelpers.Urls.CurrentRequestUri(_Context.Request);
            var hostname = requestUri != null ? requestUri.Host : "UNKNOWN";
            var environment = LookupEnvironment(requestUri);

            status.Message = $"Running {status.RunningFunctionName} on '{hostname}' [{environment.Name}] environment";

            var results = GetMediaNodesData(environment);

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
            status.RunningFunctionName = "SaveMediaData";
            status.Message = $"Running {status.RunningFunctionName} [{Environment.Name}] environment.";
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

        public MediaNodesDataFile GetMediaNodesData(Workspace Environment)
        {
            var functionName = "Dragonfly.DeployToolsService GetMediaNodesData";
            _logger.LogInformation($"{functionName} Started ...");

            var results = new MediaNodesDataFile();
            results.GeneratorVersion = DeployToolsPackage.Version;
            results.TimestampUtc = DateTime.UtcNow;
            results.Environment = Environment;
            results.Type = NodesType.Media;

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
            results.TotalNodes = _MediaResultsList.Count;

            //Finish up
            var duration = DateTime.UtcNow - results.TimestampUtc;
            results.TimeToGenerate = duration;

            _logger.LogInformation($"... {functionName}  Completed in {results.GetTimeToGenerateDisplay() }");
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
                var result = new MediaNodeDataItem(GetBasicMediaNodeData(Media));
                result.MediaTypeAlias = Media.ContentType.Alias;
                result.FilePath = Media.HasProperty("umbracoFile") ? (Media.GetValue("umbracoFile") != null ? Media.GetValue("umbracoFile").ToString() : "MISSING") : "N/A";
                result.UniversalSortInt = _MediaResultsCounter;

                //Get Parent Node Info
                var parent = _services.MediaService.GetById(Media.ParentId);
                if (parent != null)
                {
                    var parentInfo =new NodeDataItem( GetBasicMediaNodeData(parent));
                    result.ParentNodeInfo = parentInfo;
                }


                _MediaResultsList.Add(result);
            }
        }

        private INodeDataItem GetBasicMediaNodeData(IMedia Media)
        {
            var result = new NodeDataItem();
            result.NodeId = Media.Id;
            result.NodeUdi = Media.GetUdi();
            result.NodeName = Media.Name;
            result.LastEditedDate = Media.UpdateDate;
            result.LastEditedByUser = GetUserName(Media.WriterId);
            result.LevelNum = Media.Level;
            result.OrderNum = Media.SortOrder;

            return result;
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


    }
}
