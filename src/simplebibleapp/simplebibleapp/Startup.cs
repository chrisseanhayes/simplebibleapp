using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lamar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using NLog.Web.AspNetCore;
using simplebibleapp.Data.Hearts;
using simplebibleapp.xmlbible;
using simplebibleapp.xmlbible.search;
using simplebibleapp.xmlbiblerepository;
using simplebibleapp.xmldatacore;
using simplebibleapp.xmldictionary;
using Microsoft.Extensions.Caching;
using NLog.Extensions.Logging;

namespace simplebibleapp
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddDistributedRedisCache(o =>
            {
                o.Configuration = Configuration.GetConnectionString("Redis");
                o.InstanceName = "sba_web_app_session";
            });

            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
            });
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
                loggingBuilder.AddNLog(Configuration);
            });
        }
 
        public void ConfigureContainer(ServiceRegistry registry)
        {
            registry.Scan(scan => {
                scan.Assembly("simplebibleapp.xmlbible");
                scan.WithDefaultConventions();
                scan.AddAllTypesOf<IState>();
                scan.Assembly("simplebibleapp.xmlbiblerepository");
                scan.WithDefaultConventions();
                scan.AddAllTypesOf<IXmlBibleSearchAction>();
                scan.Assembly("simplebibleapp.xmldictionary");
                scan.WithDefaultConventions();
                scan.AddAllTypesOf<IGreekDefinitionState>();
                scan.AddAllTypesOf<IHebrewDefinitionState>();
                scan.TheCallingAssembly();
                scan.WithDefaultConventions();
            });
            registry.For<IChapterBuilderFactory>().Add<ChapterBuilderFactory>();
            registry.For<IWordCountBuilderFactory>().Add<WordCountBuilderFactory>();
            registry.For<IHeartRepository>().Add<MongoHeartRepository>();
            registry.For<IMongoDbConfigSource>().Add<EnvironmentMongoDbConfigSource>();
            registry.For<IVerseSearch>().Use(s => SearchHelps.GetVerseSearch(Path.Combine(s.GetInstance<IXmlPathResolver>().GetPath(), "kjvfull.xml")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddNLog();

            var fordwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            fordwardedHeaderOptions.KnownNetworks.Clear();
            fordwardedHeaderOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(fordwardedHeaderOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }


            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
