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
    return await context.Elections
        .Select(e => new
        {
            e.ElectionId,
            e.Name,
        }).ToListAsync();
});

app.MapGet("election/{electionId:guid}/party", async (ScalectionContext context, Guid electionId) =>
{
    return await context.Parties
        .Where(p => p.ElectionId == electionId)
        .Include(p => p.Candidates)
        .Select(p => new
        {
            p.PartyId,
            p.Name,
            Candidates = p.Candidates.Select(c => new
            {
                c.CandidateId,
                c.Name
            })
        })
        .ToListAsync();
});

app.MapPost("/vote", async (ScalectionContext context) =>
{
    // TODO: Implement vote handling
});


app.MapDefaultEndpoints();

app.Run();
