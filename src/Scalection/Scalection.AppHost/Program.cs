var builder = DistributedApplication.CreateBuilder(args);

var sqldb = builder.AddSqlServer("sqlserver")
                   .PublishAsAzureSqlDatabase()
                   .AddDatabase("sqldb");

builder.AddProject<Projects.Scalection_ApiService>("apiservice")
    .WithReference(sqldb);

builder.AddProject<Projects.Scalection_MigrationService>("scalection-migrationservice")
    .WithReference(sqldb);

builder.Build().Run();
