using Scalection.ApiService;
using Scalection.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.AddSqlServerDbContext<ScalectionContext>("sqldb");

var host = builder.Build();
host.Run();
