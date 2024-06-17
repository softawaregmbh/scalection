using Scalection.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

var sqldb = builder.AddSqlServer(ServiceDiscovery.SqlServer)
                   .PublishAsAzureSqlDatabase()
                   .AddDatabase(ServiceDiscovery.SqlDB);

var appInsights = builder.AddAzureApplicationInsights(ServiceDiscovery.ApplicationInsights);

builder.AddProject<Projects.Scalection_ApiService>(ServiceDiscovery.ApiService)
    .WithReference(sqldb)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Scalection_ApiService_Caching>(ServiceDiscovery.ApiServiceWithCaching)
    .WithReference(sqldb)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Scalection_MigrationService>(ServiceDiscovery.MigrationService)
    .WithReference(sqldb)
    .WithReference(appInsights);

builder.Build().Run();
