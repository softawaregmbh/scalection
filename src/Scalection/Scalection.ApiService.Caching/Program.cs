using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Scalection.Data.EF;
using Scalection.Data.EF.Models;
using Scalection.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB);

builder.Services.AddProblemDetails();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromHours(1)));
});

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseExceptionHandler();

app.UseOutputCache();

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
}).CacheOutput();

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
}).CacheOutput();

app.MapPost("election/{electionId:guid}/vote", async (
    [FromHeader(Name = "x-election-district-id")] long electionDistrictId,
    [FromHeader(Name = "x-voter-id")] long voterId,
    Guid electionId,
    VoteDto dto,
    ScalectionContext context,
    IMemoryCache cache,
    CancellationToken cancellationToken) =>
{
    var strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        var party = await cache.GetOrCreateAsync(
            $"Party_{dto.PartyId}",
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await context.Parties.FindAsync(dto.PartyId);
            });

        if (party == null || party.ElectionId != electionId)
        {
            return Results.NotFound();
        }

        if (dto.CandidateId.HasValue)
        {
            var candidate = await cache.GetOrCreateAsync(
                $"Candidate_{dto.CandidateId}_{dto.PartyId}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return await context.Candidates.SingleOrDefaultAsync(c => c.CandidateId == dto.CandidateId && c.PartyId == dto.PartyId);
                });

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
