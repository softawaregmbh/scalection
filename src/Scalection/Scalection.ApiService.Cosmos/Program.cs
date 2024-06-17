using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Scalection.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosClient(ServiceDiscovery.CosmosDB);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.MapGet("/election", async (CosmosClient client) =>
{    
    return Results.Ok();
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

app.MapPost("/vote", async (
    [FromHeader(Name = "x-voter-id")] long voterId,
    VoteDto dto,
    CosmosClient client,
    CancellationToken cancellationToken) =>
{
    var strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var voter = await context.Voters.FindAsync(voterId);

        if (voter == null)
        {
            return Results.Unauthorized();
        }

        if (voter.Voted)
        {
            return Results.Conflict();
        }

        var party = await context.Parties.SingleOrDefaultAsync(p => p.PartyId == dto.PartyId && p.ElectionId == voter.ElectionId);
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

app.Run();

record VoteDto(Guid PartyId, Guid? CandidateId);