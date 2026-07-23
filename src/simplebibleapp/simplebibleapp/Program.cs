using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using Orleans;
using Orleans.Hosting;

namespace simplebibleapp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseOrleansClient(clientBuilder =>
                {
                    var connectionString = Environment.GetEnvironmentVariable("ORLEANS_AZURE_STORAGE_CONNECTION_STRING") 
                                           ?? "UseDevelopmentStorage=true";
                    clientBuilder.UseAzureStorageClustering(options =>
                    {
                        options.ConfigureTableServiceClient(connectionString);
                    });
                    clientBuilder.Configure<global::Orleans.Configuration.ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "sba";
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    var logConfigPath = "nlog.config";
                    if (env.IsDevelopment())
                    {
                        logConfigPath = "nlog.development.config";
                    }
                    else if (env.IsProduction())
                    {
                        logConfigPath = "nlog.Production.config";
                    }
                    NLogBuilder.ConfigureNLog(logConfigPath);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UseNLog();
    }
}
