using Scalection.Data.EF;
using Scalection.MigrationService;
using Scalection.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB, s => s.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds);

var host = builder.Build();
host.Run();
