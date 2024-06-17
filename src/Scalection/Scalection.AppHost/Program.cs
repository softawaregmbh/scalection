using Scalection.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

var sqlDB = builder.AddSqlServer(ServiceDiscovery.SqlServer)
                   .WithDataVolume()
                   .PublishAsAzureSqlDatabase()
                   .AddDatabase(ServiceDiscovery.SqlDB);

var cosmosDB = builder.AddAzureCosmosDB(ServiceDiscovery.CosmosAccount)
                      .AddDatabase(ServiceDiscovery.CosmosDB)
                      .RunAsEmulator();

var appInsights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights(ServiceDiscovery.ApplicationInsights)
    : builder.AddConnectionString(ServiceDiscovery.ApplicationInsights, "APPLICATIONINSIGHTS_CONNECTION_STRING");

builder.AddProject<Projects.Scalection_ApiService>(ServiceDiscovery.ApiService)
    .WithReference(sqlDB)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Scalection_ApiService_Cosmos>(ServiceDiscovery.ApiServiceCosmosDB)
    .WithReference(cosmosDB)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Scalection_MigrationService>(ServiceDiscovery.MigrationService)
    .WithReference(sqlDB)
    .WithReference(appInsights)
    .WithExternalHttpEndpoints();


builder.Build().Run();
