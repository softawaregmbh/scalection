var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Scalection_ApiService>("apiservice");

var sqlServer = builder.AddSqlServer("sqlserver")
                       .PublishAsAzureSqlDatabase()
                       .AddDatabase("sqldb");

builder.AddProject<Projects.Scalection_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
