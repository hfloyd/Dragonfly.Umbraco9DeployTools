#pragma warning disable 1591

namespace Dragonfly.Umbraco9DeployTools.Controllers
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using Dragonfly.NetModels;
    using Dragonfly.Umbraco9DeployTools.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Umbraco.Cms.Web.BackOffice.Controllers;
    using Umbraco.Cms.Web.Common.Attributes;
    using Umbraco.Cms.Web.Common.Controllers;


    //  /umbraco/Dragonfly/RemoteDeployTools/
    [PluginController("Dragonfly")]
    public class RemoteDeployToolsController : UmbracoApiController
    {

        private readonly ILogger<DeployToolsController> _logger;
        private readonly DeployToolsService _deployToolsService;

        public RemoteDeployToolsController(ILogger<DeployToolsController> logger, DeployToolsService deployToolsService)
        {
            _logger = logger;
            _deployToolsService = deployToolsService;

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

        //[Microsoft.AspNetCore.Mvc.HttpGet]
        //public IActionResult UpdateAllLocalNodesData()
        //{
        //    var status = new StatusMessage(true);

        //    try
        //    {
        //        status.InnerStatuses.Add(_deployToolsService.SaveContentNodesData());
        //        status.InnerStatuses.Add(_deployToolsService.SaveMediaNodesData());
        //    }
        //    catch (Exception ex)
        //    {
        //        status.RelatedException = ex;
        //        status.Success = false;
        //        status.Message = "Failure while running Dragonfly DeployTools: UpdateAllLocalNodesData";
        //        _logger.LogError(ex, status.Message);
        //    }

        //    //return status;

        //    //Return JSON
        //    string json = JsonConvert.SerializeObject(status);
        //    var result = new HttpResponseMessage()
        //    {
        //        Content = new StringContent(
        //            json,
        //            Encoding.UTF8,
        //            "application/json"
        //        )
        //    };

        //    return new HttpResponseMessageResult(result);
        //}


        //  /umbraco/Dragonfly/RemoteDeployTools/Test
        //  /umbraco/Dragonfly/RemoteDeployTools/Test?Key=xxx
        [HttpGet]
        public IActionResult Test(string Key)
        {
            var validAccess = true; //For Test we allow a blank Key
            if (Key != "")
            {
                validAccess = _deployToolsService.ValidateApiKey(Key);
            }

            if (validAccess)
            {
                var byteArray = Encoding.UTF8.GetBytes("{Hello World!}");
                var stream = new MemoryStream(byteArray);
                return File(stream, "text/json", "test.json");
            }
            else
            {
                //Return Error
                //string message = JsonConvert.SerializeObject(status);
                var result = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    //Content = new StringContent(
                    //    json,
                    //    Encoding.UTF8,
                    //    "application/json"
                    //)
                };
                return new HttpResponseMessageResult(result);

            }
        }

        //  /umbraco/Dragonfly/RemoteDeployTools/ExportContentData?UpdateNow=true&ReturnType=Json&Key=xxx
        //  /umbraco/Dragonfly/RemoteDeployTools/ExportContentData?UpdateNow=true&ReturnType=File&Key=xxx
        [HttpGet]
        public IActionResult ExportContentData(bool UpdateNow, string ReturnType,string Key)
        {
            var validAccess = false; //blank Keys are not allowed
            if (Key != "")
            {
                validAccess = _deployToolsService.ValidateApiKey(Key);
            }

            if (validAccess)
            {
                if (UpdateNow)
                {
                    var status = new StatusMessage(true);

                    try
                    {
                        status.InnerStatuses.Add(_deployToolsService.SaveContentNodesData());
                    }
                    catch (Exception ex)
                    {
                        status.RelatedException = ex;
                        status.Success = false;
                        status.Message = "Failure while running Dragonfly DeployTools: ExportContentData";
                        _logger.LogError(ex, status.Message);
                    }

                }

                //Export
                var thisEnvironment = _deployToolsService.GetCurrentEnvironment();
                var saveFileName = _deployToolsService.EnvironmentFilePath(DeployToolsService.NodesType.Content, thisEnvironment, true);
                var fileContents = _deployToolsService.RetrieveFileContents(DeployToolsService.NodesType.Content, thisEnvironment);

                if (ReturnType == "File")
                {
                    //Return File
                    var byteArray = Encoding.UTF8.GetBytes(fileContents);
                    var stream = new MemoryStream(byteArray);
                    return File(stream, "text/json", saveFileName);
                }
                else  //if (ReturnType == "Json")
                {
                    //Return JSON
                    string json = fileContents; //JsonConvert.SerializeObject(fileContents);
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

            }
            else
            {
                //Return Error
                //string message = JsonConvert.SerializeObject(status);
                var result = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    //Content = new StringContent(
                    //    json,
                    //    Encoding.UTF8,
                    //    "application/json"
                    //)
                };
                return new HttpResponseMessageResult(result);

            }
        }

        //  /umbraco/Dragonfly/RemoteDeployTools/ExportMediaData?UpdateNow=true&ReturnType=Json&Key=xxx
        //  /umbraco/Dragonfly/RemoteDeployTools/ExportMediaData?UpdateNow=true&ReturnType=File&Key=xxx
        [HttpGet]
        public IActionResult ExportMediaData(bool UpdateNow, string ReturnType, string Key)
        {
            var validAccess = false; //blank Keys are not allowed
            if (Key != "")
            {
                validAccess = _deployToolsService.ValidateApiKey(Key);
            }

            if (validAccess)
            {
                if (UpdateNow)
                {
                    var status = new StatusMessage(true);

                    try
                    {
                        status.InnerStatuses.Add(_deployToolsService.SaveMediaNodesData());
                    }
                    catch (Exception ex)
                    {
                        status.RelatedException = ex;
                        status.Success = false;
                        status.Message = "Failure while running Dragonfly DeployTools: ExportContentData";
                        _logger.LogError(ex, status.Message);
                    }

                }

                //Export
                var thisEnvironment = _deployToolsService.GetCurrentEnvironment();
                var saveFileName = _deployToolsService.EnvironmentFilePath(DeployToolsService.NodesType.Media, thisEnvironment, true);
                var fileContents = _deployToolsService.RetrieveFileContents(DeployToolsService.NodesType.Media, thisEnvironment);

                if (ReturnType == "File")
                {
                    //Return File
                    var byteArray = Encoding.UTF8.GetBytes(fileContents);
                    var stream = new MemoryStream(byteArray);
                    return File(stream, "text/json", saveFileName);
                }
                else  //if (ReturnType == "Json")
                {
                    //Return JSON
                    string json = fileContents; //JsonConvert.SerializeObject(fileContents);
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

            }
            else
            {
                //Return Error
                //string message = JsonConvert.SerializeObject(status);
                var result = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    //Content = new StringContent(
                    //    json,
                    //    Encoding.UTF8,
                    //    "application/json"
                    //)
                };
                return new HttpResponseMessageResult(result);

            }
        }


        //  /umbraco/Dragonfly/RemoteDeployTools/TriggerContentDataSave?Key=xxx
        [HttpGet]
        public IActionResult TriggerContentDataSave(string Key)
        {
            var validAccess = false; //blank Keys are not allowed
            if (Key != "")
            {
                validAccess = _deployToolsService.ValidateApiKey(Key);
            }

            if (validAccess)
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
                    status.Message = "Failure while running Dragonfly DeployTools: TriggerContentDataSave";
                    _logger.LogError(ex, status.Message);
                }

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
            else
            {
                //Return Error
                //string message = JsonConvert.SerializeObject(status);
                var result = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    //Content = new StringContent(
                    //    json,
                    //    Encoding.UTF8,
                    //    "application/json"
                    //)
                };
                return new HttpResponseMessageResult(result);

            }
        }


        //  /umbraco/Dragonfly/RemoteDeployTools/TriggerMediaDataSave?Key=xxx
        [HttpGet]
        public IActionResult TriggerMediaDataSave(string Key)
        {
            var validAccess = false; //blank Keys are not allowed
            if (Key != "")
            {
                validAccess = _deployToolsService.ValidateApiKey(Key);
            }

            if (validAccess)
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
                    status.Message = "Failure while running Dragonfly DeployTools: TriggerMediaDataSave";
                    _logger.LogError(ex, status.Message);
                }

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
            else
            {
                //Return Error
                //string message = JsonConvert.SerializeObject(status);
                var result = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    //Content = new StringContent(
                    //    json,
                    //    Encoding.UTF8,
                    //    "application/json"
                    //)
                };
                return new HttpResponseMessageResult(result);

            }
        }


        //[Microsoft.AspNetCore.Mvc.HttpGet]
        //public IActionResult UpdateLocalMediaNodesData()
        //{
        //    var status = new StatusMessage();

        //    try
        //    {
        //        status.InnerStatuses.Add(_deployToolsService.SaveMediaNodesData());
        //    }
        //    catch (Exception ex)
        //    {
        //        status.RelatedException = ex;
        //        status.Success = false;
        //        status.Message = "Failure while running Dragonfly DeployTools: UpdateLocalMediaNodesData";
        //        _logger.LogError(ex, status.Message);
        //    }

        //    //  return status;

        //    //Return JSON
        //    string json = JsonConvert.SerializeObject(status);
        //    var result = new HttpResponseMessage()
        //    {
        //        Content = new StringContent(
        //            json,
        //            Encoding.UTF8,
        //            "application/json"
        //        )
        //    };

        //    return new HttpResponseMessageResult(result);

        //}

    }

}