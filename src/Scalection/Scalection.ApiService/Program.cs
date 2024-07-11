using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalection.Data.EF;
using Scalection.Data.EF.Models;
using Scalection.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/election", async (ScalectionContext context, CancellationToken cancellationToken) =>
{
    var strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        return await context.Elections
       .Select(e => new
       {
           e.ElectionId,
           e.Name,
       }).ToListAsync(cancellationToken);
    });
});

app.MapGet("election/{electionId:guid}/party", async (ScalectionContext context, Guid electionId, CancellationToken cancellationToken) =>
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
            .ToListAsync(cancellationToken);
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
        var party = await context.Parties.SingleOrDefaultAsync(p => p.PartyId == dto.PartyId && p.ElectionId == electionId, cancellationToken);
        if (party == null)
        {
            return Results.NotFound();
        }

        if (dto.CandidateId.HasValue)
        {
            var candidate = await context.Candidates.SingleOrDefaultAsync(c => c.CandidateId == dto.CandidateId && c.PartyId == dto.PartyId, cancellationToken);
            if (candidate == null)
            {
                return Results.NotFound();
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        var voter = await context.Voters.FindAsync([voterId], cancellationToken);

        if (voter == null || voter.ElectionId != electionId || voter.ElectionDistrictId != electionDistrictId)
        {
            return Results.Unauthorized();
        }

        if (voter.Voted)
        {
            return Results.Conflict();
        }

        await context.Votes.AddAsync(new Vote()
        {
            VoteId = Guid.NewGuid(),
            PartyId = dto.PartyId,
            CandidateId = dto.CandidateId,
            ElectionDistrictId = voter.ElectionDistrictId,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        voter.Voted = true;

        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Results.NoContent();
    });
});

app.MapDefaultEndpoints();

app.Run();

record VoteDto(Guid PartyId, Guid? CandidateId);
