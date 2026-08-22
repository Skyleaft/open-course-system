var builder = DistributedApplication.CreateBuilder(args);


var web = builder.AddProject<Projects.Web>("web")
    .WithEndpoint("https", endpoint => {
        endpoint.IsProxied = false;
    });

builder.Build().Run();
