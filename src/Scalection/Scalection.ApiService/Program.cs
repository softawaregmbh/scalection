using Microsoft.EntityFrameworkCore;
using Scalection.ApiService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.AddSqlServerDbContext<ScalectionContext>("sqldb");

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/election", async (ScalectionContext context) =>
{
    return await context.Elections.ToListAsync();
});

app.MapDefaultEndpoints();

app.Run();
