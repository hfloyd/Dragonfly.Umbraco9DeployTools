namespace Dragonfly.Umbraco9DeployTools.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using Dragonfly.NetModels;
    using Dragonfly.Umbraco9DeployTools.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Umbraco.Cms.Web.BackOffice.Controllers;
    using Umbraco.Cms.Web.Common.Attributes;


    //  /umbraco/backoffice/Dragonfly/ExceptionViewer/
    [PluginController("Dragonfly")]
    [IsBackOffice]
    public class ExceptionViewerController : UmbracoAuthorizedApiController
    {
        private readonly ILogger<DeployToolsController> _logger;
        private readonly IViewRenderService _viewRenderService;

        public ExceptionViewerController(
            ILogger<DeployToolsController> logger,
            IViewRenderService viewRenderService)
        {
            _logger = logger;
            _viewRenderService = viewRenderService;

        }

        private string RazorFilesPath()
        {
            return DeployToolsService.PluginPath() + "RazorViews/";
        }

        [HttpPost]
        public IActionResult View(string ExceptionJson)
        {
            //Setup
            var pvPath = RazorFilesPath() + "ExceptionViewer.cshtml";

            //GET DATA TO DISPLAY
            var status = new StatusMessage(true);
            Exception model = null;
            if (ExceptionJson != "")
            {
                model = JsonConvert.DeserializeObject<Exception>(ExceptionJson);
            }
            else
            {
                status.Success = false;
                status.Message = "Provided Exception data is NULL";
            }

            //VIEW DATA 
            var viewData = new Dictionary<string, object>();
            viewData.Add("Status", status);

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

        public IActionResult Index(string ExceptionId)
        {
            //Setup
            var pvPath = RazorFilesPath() + "ExceptionViewer.cshtml";

            //GET DATA TO DISPLAY
            var status = new StatusMessage(true);
            Exception model = null;
            if (!string.IsNullOrEmpty(Request.HttpContext.Session.GetString(ExceptionId)))
            {
                var json = Request.HttpContext.Session.GetString(ExceptionId);
                model = JsonConvert.DeserializeObject<Exception>(json);
            }
            else
            {
                status.Success = false;
                status.Message = $"Session is missing Exception data for {ExceptionId}";
            }

            //VIEW DATA 
            var viewData = new Dictionary<string, object>();
            viewData.Add("Status", status);

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

      

    }
}
