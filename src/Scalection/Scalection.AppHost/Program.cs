using Scalection.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

var sqldb = builder.AddSqlServer(ServiceDiscovery.SqlServer)
                   .PublishAsAzureSqlDatabase()
                   .AddDatabase(ServiceDiscovery.SqlDB);

builder.AddProject<Projects.Scalection_ApiService>(ServiceDiscovery.ApiService)
    .WithReference(sqldb)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Scalection_MigrationService>(ServiceDiscovery.MigrationService)
    .WithReference(sqldb);

builder.Build().Run();
