using System.Data;
//using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalection.Data.EF;
using Scalection.Data.EF.Models;
using Scalection.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddHttpLogging(logging =>
//{
//    logging.LoggingFields = HttpLoggingFields.All;
//    logging.RequestHeaders.Add("x-voter-id");
//    logging.RequestHeaders.Add("x-election-district-id");
//    logging.RequestBodyLogLimit = 4096;
//    logging.ResponseBodyLogLimit = 4096;
//    logging.CombineLogs = true;
//});

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB);

builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

//app.UseHttpLogging();

app.MapGet("/election", async (ScalectionContext context) =>
{
    var strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        return await context.Elections
       .Select(e => new
       {
           e.ElectionId,
           e.Name,
       }).ToListAsync();
    });
});

app.MapGet("election/{electionId:guid}/party", async (ScalectionContext context, Guid electionId) =>
{
    var strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
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
});

app.MapPost("election/{electionId:guid}/vote", async (
    [FromHeader(Name = "x-election-district-id")] long electionDistrictId,
    [FromHeader(Name = "x-voter-id")] long voterId,
    Guid electionId,
    VoteDto dto,
    ScalectionContext context,
    CancellationToken cancellationToken) =>
{
    var strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var voter = await context.Voters.FindAsync(voterId);

        if (voter == null || voter.ElectionId != electionId || voter.ElectionDistrictId != electionDistrictId)
        {
            return Results.Unauthorized();
        }

        if (voter.Voted)
        {
            return Results.Conflict();
        }

        var party = await context.Parties.SingleOrDefaultAsync(p => p.PartyId == dto.PartyId && p.ElectionId == electionId);
        if (party == null)
        {
            return Results.NotFound();
        }

        if (dto.CandidateId.HasValue)
        {
            var candidate = await context.Candidates.SingleOrDefaultAsync(c => c.CandidateId == dto.CandidateId && c.PartyId == dto.PartyId);
            if (candidate == null)
            {
                return Results.NotFound();
            }
        }

        await context.Votes.AddAsync(new Vote()
        {
            VoteId = Guid.NewGuid(),
            PartyId = dto.PartyId,
            CandidateId = dto.CandidateId,
            ElectionDistrictId = voter.ElectionDistrictId,
            Timestamp = DateTime.UtcNow
        });

        voter.Voted = true;

        await context.SaveChangesAsync();

        await transaction.CommitAsync(cancellationToken);

        return Results.NoContent();
    });
});

app.MapDefaultEndpoints();

app.Run();

record VoteDto(Guid PartyId, Guid? CandidateId);
