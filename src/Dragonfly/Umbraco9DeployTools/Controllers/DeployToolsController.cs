#pragma warning disable 1591

namespace Dragonfly.Umbraco9DeployTools.Controllers
{
    using System;
    using System.Net.Http;
    using System.Text;
    using Dragonfly.NetModels;
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
            _viewRenderService= viewRenderService;

        }

        private string RazorFilesPath()
        {
            return DeployToolsService.PluginPath() + "RazorViews/";
        }

        //[HttpGet]
        //public object GetStatus()
        //{

        //    try
        //    {

        //      //  return new StatusResult(_modelsBuilderSettings, _outOfDateModelsStatus, _sourceGenerator);

        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Failed getting status.");

        //        return new { success = false };

        //    }

        //}

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

        //  /umbraco/backoffice/Dragonfly/DeployTools/CompareContent?EnvironmentType=live
        [HttpGet]
        public IActionResult CompareContent(string EnvironmentType)
        {
            //Setup
            var pvPath = RazorFilesPath() + "ContentCompare.cshtml";
            
            //GET DATA TO DISPLAY
            var dataSet = _deployToolsService.CompareContentNodes(EnvironmentType);

            //VIEW DATA 
            //var viewData = new ViewDataDictionary(null);
            //viewData.Model = dataSet;
            //viewData.Add("TestResultSet", resultSet);
            //viewData.Add("FilesList", filesList);
            //viewData.Add("DisplayMode", displayMode);
            //viewData.Add("FullLinksSet", allLinksSet);

            //RENDER
           // var controllerContext = this.ControllerContext;

         //  var service =new  ViewRenderService(this.ra)
            var htmlTask=  _viewRenderService.RenderToStringAsync(this.HttpContext, pvPath, dataSet);
            var displayHtml = htmlTask.Result; 
            //  ApiControllerHtmlHelper.GetPartialViewHtml(controllerContext, pvPath, viewData, HttpContext.Current);


            //RETURN AS HTML
            var result =new HttpResponseMessage()
            {
                Content = new StringContent(
                    displayHtml,
                    Encoding.UTF8,
                    "text/html"
                )
            };

            return new HttpResponseMessageResult(result);
        }

        public IActionResult Start()
        {
            //Setup
            var pvPath = RazorFilesPath() + "Start.cshtml";

            //GET DATA TO DISPLAY
            var dataSet = _deployToolsService.GetAllDeployEnvironments();

            //VIEW DATA 
            //var viewData = new ViewDataDictionary(null);
            //viewData.Model = dataSet;
            //viewData.Add("TestResultSet", resultSet);
            //viewData.Add("FilesList", filesList);
            //viewData.Add("DisplayMode", displayMode);
            //viewData.Add("FullLinksSet", allLinksSet);

            //RENDER
            // var controllerContext = this.ControllerContext;

            //  var service =new  ViewRenderService(this.ra)
            var htmlTask = _viewRenderService.RenderToStringAsync(this.HttpContext, pvPath, dataSet);
            var displayHtml = htmlTask.Result;
            //  ApiControllerHtmlHelper.GetPartialViewHtml(controllerContext, pvPath, viewData, HttpContext.Current);


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

    }

}