using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Scalection.Data.Cosmos;
using Scalection.Data.Cosmos.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddCosmosClient();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.MapGet("/election", (CosmosClient client, CancellationToken cancellationToken) =>
{
    return client.Elections()
        .Select(e => new
        {
            e.ElectionId,
            e.Name
        }).ToAsyncEnumerable(cancellationToken);
});

app.MapGet("election/{electionId:guid}/party", (CosmosClient context, Guid electionId, CancellationToken cancellationToken) =>
{
    return context.Parties()
        .Where(p => p.ElectionId == electionId)
        .Select(p => new
        {
            p.PartyId,
            p.Name,
            Candidates = p.Candidates.Select(c => new
            {
                c.CandidateId,
                c.Name
            })
        }).ToAsyncEnumerable(cancellationToken);
});

app.MapPost("election/{electionId:guid}/vote", async (
    [FromHeader(Name = "x-election-district-id")] long electionDistrictId,
    [FromHeader(Name = "x-voter-id")] long voterId,
    Guid electionId,
    VoteDto dto,
    CosmosClient client,
    CancellationToken cancellationToken) =>
{
    var cosmosVoterId = CosmosEntity.CreateId<Voter>(voterId);
    var partitionKey = new PartitionKey(ElectionDistrictEntity.CreatePartitionKey(electionId, electionDistrictId));

    var electionDistrictContainer = client.ElectionDistrictContainer();

    ItemResponse<Voter> voterResponse;
    try
    {
        voterResponse = await electionDistrictContainer.ReadItemAsync<Voter>(
            cosmosVoterId,
            partitionKey,
            cancellationToken: cancellationToken);
    }
    catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
    {
        return Results.Unauthorized();
    }

    if (voterResponse.Resource.Voted)
    {
        return Results.Conflict();
    }

    ItemResponse<Party> partyResponse;
    try
    {
        partyResponse = await client.ElectionContainer().ReadItemAsync<Party>(
            CosmosEntity.CreateId<Party>(dto.PartyId),
            new PartitionKey(electionId.ToString()),
            cancellationToken: cancellationToken);
    }
    catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }

    if (dto.CandidateId.HasValue)
    {
        if (!partyResponse.Resource.Candidates.Any(c => c.CandidateId == dto.CandidateId))
        {
            return Results.NotFound();
        }
    }

    var transaction = electionDistrictContainer.CreateTransactionalBatch(partitionKey);

    transaction.CreateItem(new Vote()
    {
        VoteId = Guid.NewGuid(),
        ElectionId = electionId,
        PartyId = dto.PartyId,
        CandidateId = dto.CandidateId,
        ElectionDistrictId = electionDistrictId,
        Timestamp = DateTime.UtcNow
    });

    transaction.PatchItem(
        cosmosVoterId,
        [PatchOperation.Set("/voted", true)],
        new()
        {
            IfMatchEtag = voterResponse.ETag
        });

    var response = await transaction.ExecuteAsync(cancellationToken);

    return response.IsSuccessStatusCode
        ? Results.NoContent()
        : Results.Conflict();
});

app.Run();

record VoteDto(Guid PartyId, Guid? CandidateId);