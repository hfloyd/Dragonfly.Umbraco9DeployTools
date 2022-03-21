#pragma warning disable 1591

namespace Dragonfly.Umbraco9DeployTools.Composers
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Umbraco.Cms.Core.Composing;
    using Umbraco.Cms.Core.DependencyInjection;

    public class SetupComposer : IComposer
    {

        public void Compose(IUmbracoBuilder builder)
        {

            // builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            builder.Services.AddMvcCore().AddRazorViewEngine();
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            //builder.Services.AddSingleton<IRazorViewEngine>();
            //  builder.Services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            // builder.Services.AddScoped<IServiceProvider, ServiceProvider>();


            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<Dragonfly.Umbraco9DeployTools.Services.DependencyLoader>();
            builder.Services.AddScoped<Dragonfly.UmbracoServices.FileHelperService>();
            builder.Services.AddScoped<Dragonfly.Umbraco9DeployTools.Services.DeployToolsService>();
            builder.Services.AddScoped<IViewRenderService, ViewRenderService>();

            //builder.AddUmbracoOptions<Settings>();

        }

    }

}