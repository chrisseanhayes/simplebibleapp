using System;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.UseOrleans(siloBuilder =>
{
    var connectionString = Environment.GetEnvironmentVariable("ORLEANS_AZURE_STORAGE_CONNECTION_STRING") 
                           ?? "UseDevelopmentStorage=true";

    siloBuilder.UseAzureStorageClustering(options =>
    {
        options.ConfigureTableServiceClient(connectionString);
    });

    siloBuilder.Configure<global::Orleans.Configuration.ClusterOptions>(options =>
    {
        options.ClusterId = "dev";
        options.ServiceId = "sba";
    });

    siloBuilder.AddAzureBlobGrainStorage("blobStorage", options =>
    {
        options.ConfigureBlobServiceClient(connectionString);
    });

    // When running inside docker, the container needs to listen on these ports
    // and they should be mapped or exposed to the cluster network.
    // 11111 is for Silo-to-Silo, 30000 is for Client-to-Silo.
    siloBuilder.ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);
});

var host = builder.Build();
await host.RunAsync();
