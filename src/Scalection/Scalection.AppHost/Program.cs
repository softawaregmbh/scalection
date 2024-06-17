using Scalection.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

var sqldb = builder.AddSqlServer(ServiceDiscovery.SqlServer)
                   .WithDataVolume()
                   .PublishAsAzureSqlDatabase()
                   .AddDatabase(ServiceDiscovery.SqlDB);

var appInsights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights(ServiceDiscovery.ApplicationInsights)
    : builder.AddConnectionString(ServiceDiscovery.ApplicationInsights, "APPLICATIONINSIGHTS_CONNECTION_STRING");

builder.AddProject<Projects.Scalection_ApiService>(ServiceDiscovery.ApiService)
    .WithReference(sqldb)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Scalection_MigrationService>(ServiceDiscovery.MigrationService)
    .WithReference(sqldb)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.Build().Run();
