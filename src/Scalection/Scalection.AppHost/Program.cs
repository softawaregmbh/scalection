var builder = DistributedApplication.CreateBuilder(args);

var sqldb = builder.AddSqlServer("sqlserver")
                   .PublishAsAzureSqlDatabase()
                   .AddDatabase("sqldb");

var apiService = builder.AddProject<Projects.Scalection_ApiService>("apiservice")
                        .WithReference(sqldb);

builder.AddProject<Projects.Scalection_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
