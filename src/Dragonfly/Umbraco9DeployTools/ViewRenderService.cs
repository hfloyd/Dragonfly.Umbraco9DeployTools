namespace Dragonfly.Umbraco9DeployTools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;


    //From https://stackoverflow.com/a/57888901/3841490

    //DI Setup: services.AddScoped<IViewRenderService, ViewRenderService>();
    //Usage (in a Controller): string html = await m_RenderService.RenderToStringAsync("<NameOfPartial>", new Model());
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(HttpContext HttpContext, string ViewName, object Model, Dictionary<string, object> ViewDataDictionary = null);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        //     private readonly IServiceProvider _serviceProvider;
        //private readonly IHttpContextAccessor _contextAccessor;

        public ViewRenderService(IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider)    //,IHttpContextAccessor contextAccessor IServiceProvider serviceProvider
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
          //  _contextAccessor = contextAccessor;

            //  _serviceProvider = serviceProvider;
        }
        public async Task<string> RenderToStringAsync(HttpContext HttpContext, string ViewName, object Model, Dictionary<string, object> ViewDataDictionary = null)
        {
            //  var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
           // var httpContext = _contextAccessor.HttpContext;
            var actionContext = new ActionContext(HttpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _razorViewEngine.GetView(ViewName, ViewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{ViewName} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = Model
                };

                if (ViewDataDictionary != null)
                {
                    foreach (var item in ViewDataDictionary)
                    {
                        viewDictionary.Add(item.Key,item.Value);
                    }
                }

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();

            }
        }
    }
}
