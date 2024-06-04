using Microsoft.EntityFrameworkCore;
using Scalection.Data.EF;
using Scalection.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB);

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/election", async (ScalectionContext context) =>
{
    return await context.Elections.ToListAsync();
});

app.MapGet("/parties", async (ScalectionContext context) =>
{
    return await context.Parties
        .Include(p => p.Candidates)
        .ToListAsync();
});

app.MapPost("/vote", async (ScalectionContext context) =>
{
    // TODO: Implement vote handling
});


app.MapDefaultEndpoints();

app.Run();
