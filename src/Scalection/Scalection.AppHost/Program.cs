var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Scalection_ApiService>("apiservice");

builder.AddProject<Projects.Scalection_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
