#pragma warning disable 1591

namespace Dragonfly.Umbraco9DeployTools.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using Dragonfly.NetModels;
    using Dragonfly.Umbraco9DeployTools.Models;
    using Dragonfly.Umbraco9DeployTools.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Umbraco.Cms.Web.BackOffice.Controllers;
    using Umbraco.Cms.Web.Common.Attributes;


    //  /umbraco/backoffice/Dragonfly/DeployTools/
    [PluginController("Dragonfly")]
    [IsBackOffice]
    public class DeployToolsController : UmbracoAuthorizedApiController
    {

        private readonly ILogger<DeployToolsController> _logger;
        private readonly DeployToolsService _deployToolsService;
        private readonly IViewRenderService _viewRenderService;

        public DeployToolsController(
            ILogger<DeployToolsController> logger,
            DeployToolsService deployToolsService,
            IViewRenderService viewRenderService)
        {
            _logger = logger;
            _deployToolsService = deployToolsService;
            _viewRenderService = viewRenderService;

        }

        private string RazorFilesPath()
        {
            return DeployToolsService.PluginPath() + "RazorViews/";
        }

        #region Actions (returns StatusMsg)

        //  /umbraco/backoffice/Dragonfly/DeployTools/FetchAllRemoteNodesData?UpdateRemoteFirst=true&EnvironmentType=live
        [HttpGet]
        public IActionResult FetchAllRemoteNodesData(bool UpdateRemoteFirst, string EnvironmentType)
        {
            var status = new StatusMessage(true);

            try
            {
                status.InnerStatuses.Add(_deployToolsService.FetchRemoteContentNodesData(EnvironmentType, UpdateRemoteFirst));
                status.InnerStatuses.Add(_deployToolsService.FetchRemoteMediaNodesData(EnvironmentType, UpdateRemoteFirst));
            }
            catch (Exception ex)
            {
                status.RelatedException = ex;
                status.Success = false;
                status.Message = $"Failure while running Dragonfly DeployTools: FetchAllRemoteNodesData('{EnvironmentType}',{UpdateRemoteFirst})";
                _logger.LogError(ex, status.Message);
            }

            //return status;

            //Return JSON
            string json = JsonConvert.SerializeObject(status);
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                )
            };

            return new HttpResponseMessageResult(result);
        }

        //  /umbraco/backoffice/Dragonfly/DeployTools/UpdateAllLocalNodesData
        [Microsoft.AspNetCore.Mvc.HttpGet]
        public IActionResult UpdateAllLocalNodesData()
        {
            var status = new StatusMessage(true);

            try
            {
                status.InnerStatuses.Add(_deployToolsService.SaveContentNodesData());
                status.InnerStatuses.Add(_deployToolsService.SaveMediaNodesData());
            }
            catch (Exception ex)
            {
                status.RelatedException = ex;
                status.Success = false;
                status.Message = "Failure while running Dragonfly DeployTools: UpdateAllLocalNodesData";
                _logger.LogError(ex, status.Message);
            }

            //return status;

            //Return JSON
            string json = JsonConvert.SerializeObject(status);
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                )
            };

            return new HttpResponseMessageResult(result);
        }

        //  /umbraco/backoffice/Dragonfly/DeployTools/UpdateLocalContentNodesData
        [Microsoft.AspNetCore.Mvc.HttpGet]
        public IActionResult UpdateLocalContentNodesData()
        {
            var status = new StatusMessage();

            try
            {
                status.InnerStatuses.Add(_deployToolsService.SaveContentNodesData());
            }
            catch (Exception ex)
            {
                status.RelatedException = ex;
                status.Success = false;
                status.Message = "Failure while running Dragonfly DeployTools: UpdateLocalContentNodesData";
                _logger.LogError(ex, status.Message);
            }

            // return status;

            //Return JSON
            string json = JsonConvert.SerializeObject(status);
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                )
            };

            return new HttpResponseMessageResult(result);
        }

        //  /umbraco/backoffice/Dragonfly/DeployTools/UpdateLocalMediaNodesData
        [Microsoft.AspNetCore.Mvc.HttpGet]
        public IActionResult UpdateLocalMediaNodesData()
        {
            var status = new StatusMessage();

            try
            {
                status.InnerStatuses.Add(_deployToolsService.SaveMediaNodesData());
            }
            catch (Exception ex)
            {
                status.RelatedException = ex;
                status.Success = false;
                status.Message = "Failure while running Dragonfly DeployTools: UpdateLocalMediaNodesData";
                _logger.LogError(ex, status.Message);
            }

            //  return status;

            //Return JSON
            string json = JsonConvert.SerializeObject(status);
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                )
            };

            return new HttpResponseMessageResult(result);

        }

        #endregion

        #region Views (returns HTML)

        //  /umbraco/backoffice/Dragonfly/DeployTools/Start
        [HttpGet]
        public IActionResult Start()
        {
            //Setup
            var pvPath = RazorFilesPath() + "Start.cshtml";

            //GET DATA TO DISPLAY
            var model = _deployToolsService.GetAllDeployEnvironments();
            var localFilesInfo = new List<INodesDataFile>();
            var localFilesStatus = _deployToolsService.GetLocalFilesInfo(out localFilesInfo);
       
            //VIEW DATA 
            var viewData = new Dictionary<string, object>();
            viewData.Add("StandardInfo", GetStandardViewInfo());
            viewData.Add("Status", localFilesStatus);
            viewData.Add("LocalFilesInfo", localFilesInfo);
   
            //RENDER
            var htmlTask = _viewRenderService.RenderToStringAsync(this.HttpContext, pvPath, model, viewData);
            var displayHtml = htmlTask.Result;

            //RETURN AS HTML
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(
                    displayHtml,
                    Encoding.UTF8,
                    "text/html"
                )
            };

            return new HttpResponseMessageResult(result);
        }


        //  /umbraco/backoffice/Dragonfly/DeployTools/CompareContent?EnvironmentType=live
        [HttpGet]
        public IActionResult CompareContent(string EnvironmentType)
        {
            //Setup
            var pvPath = RazorFilesPath() + "ContentCompare.cshtml";

            //GET DATA TO DISPLAY
            ComparisonResults model = null;
            var compareStatus = _deployToolsService.CompareContentNodes(EnvironmentType, out model);


            //VIEW DATA 
            var viewData = new Dictionary<string, object>();
            viewData.Add("StandardInfo", GetStandardViewInfo());
            viewData.Add("Status", compareStatus);

            //RENDER
            var htmlTask = _viewRenderService.RenderToStringAsync(this.HttpContext, pvPath, model, viewData);
            var displayHtml = htmlTask.Result;
     
            //RETURN AS HTML
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(
                    displayHtml,
                    Encoding.UTF8,
                    "text/html"
                )
            };

            return new HttpResponseMessageResult(result);
        }

    #endregion

    internal StandardViewInfo GetStandardViewInfo()
    {
        var info = new StandardViewInfo();

        info.CurrentEnvironment = _deployToolsService.GetCurrentEnvironment();
        info.CurrentToolVersion = DeployToolsPackage.Version;

        return info;
    }

    }

}