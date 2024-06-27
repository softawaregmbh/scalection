using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NGuid;
using OpenTelemetry.Trace;
using Scalection.Data.Cosmos;
using Scalection.Data.Cosmos.Models;
using Scalection.Data.EF;
using Scalection.ServiceDefaults;
using CosmosModels = Scalection.Data.Cosmos.Models;
using EFModels = Scalection.Data.EF.Models;

const int NumberOfElectionDistricts = 10_000;
const int NumberOfParties = 10;
const int NumberOfCandidatesPerParty = 10;
const int BatchSize = 10_000;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB, s => s.CommandTimeout = (int)TimeSpan.FromMinutes(10).TotalSeconds);
builder.AddCosmosClient(c =>
{
    c.AllowBulkExecution = true;
    c.MaxRetryAttemptsOnRateLimitedRequests = null;
    c.MaxRetryWaitTimeOnRateLimitedRequests = null;
    c.GatewayModeMaxConnectionLimit = 1000;
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var activitySource = new ActivitySource("Migrations");

app.MapPost("sql/migrate", async (ScalectionContext context, CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity("Migrating SQL database", ActivityKind.Client);

    try
    {
        await EnsureDatabaseAsync();
        await RunMigrationAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task EnsureDatabaseAsync()
    {
        var dbCreator = context.GetService<IRelationalDatabaseCreator>();

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Create the database if it does not exist.
            // Do this first so there is then a database to start a transaction against.
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    async Task RunMigrationAsync()
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await context.Database.ExecuteSqlRawAsync($"ALTER DATABASE {ServiceDiscovery.SqlDB} SET COMPATIBILITY_LEVEL = 160", cancellationToken);

            // Run migration in a transaction to avoid partial migration if it fails.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
});

app.MapPost("sql/regenerate/{voters:int}", async (ScalectionContext context, int voters) =>
{
    // For now, don't use the injected token to prevent gateway timeouts
    CancellationToken cancellationToken = default;

    using var activity = activitySource.StartActivity("Regenerating data in SQL database", ActivityKind.Client);

    try
    {
        await RegenerateDataAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task RegenerateDataAsync()
    {
        EFModels.Election election = new()
        {
            ElectionId = DemoData.ElectionId,
            Name = "Scalection Demo Election",
        };

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await AddOrUpdateAsync(election, e => e.ElectionId);

            await context.Database.ExecuteSqlRawAsync("""
                delete from Votes;
                delete from Voters;
                delete from ElectionDistricts;
                delete from Candidates;
                delete from Parties;
                """);

            foreach (var party in GenerateItems("Party", NumberOfParties, election.ElectionId))
            {
                await AddOrUpdateAsync(new EFModels.Party
                {
                    ElectionId = election.ElectionId,
                    PartyId = party.Id,
                    Name = party.Name
                }, p => p.PartyId);

                foreach (var candidate in GenerateItems("Candidate", NumberOfCandidatesPerParty, party.Id))
                {
                    await AddOrUpdateAsync(new EFModels.Candidate
                    {
                        PartyId = party.Id,
                        CandidateId = candidate.Id,
                        Name = candidate.Name
                    }, c => c.CandidateId);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                insert into ElectionDistricts(ElectionDistrictId, ElectionId, Name)
                select value, @p0, 'District ' + CONVERT(varchar(5), value)
                from GENERATE_SERIES(1, @p1);
                """,
                [election.ElectionId, NumberOfElectionDistricts],
                cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                insert into Voters(VoterId, ElectionId, ElectionDistrictId, Voted)
                select value, @p0, (value % @p1) + 1, 0
                from GENERATE_SERIES(1, @p2);
                """,
                [election.ElectionId, NumberOfElectionDistricts, voters],
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        async Task AddOrUpdateAsync<T, TId>(T entity, Func<T, TId> idSelector)
            where T : class
        {
            var dbSet = context.Set<T>();
            var dbEntity = await dbSet.FindAsync(idSelector(entity));
            if (dbEntity == null)
            {
                dbSet.Add(entity);
            }
            else
            {
                dbSet.Entry(dbEntity).CurrentValues.SetValues(entity);
            }
        }
    }
});

app.MapPost("sql/reset-votes", async (ScalectionContext context, CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity("Resetting votes in SQL database", ActivityKind.Client);

    try
    {
        await ResetVotesAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task ResetVotesAsync()
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                delete from Votes;
                update Voters set Voted = 0;
                """,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
});

app.MapPost("cosmos/migrate", async (CosmosClient client, CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity("Migrating Cosmos database", ActivityKind.Client);

    try
    {
        await EnsureContainersAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task EnsureContainersAsync()
    {
        var database = client.GetDatabase(ServiceDiscovery.CosmosDB);
        await database.CreateContainerIfNotExistsAsync(CosmosDB.ElectionContainerName, "/electionId", cancellationToken: cancellationToken);
        await database.CreateContainerIfNotExistsAsync(new ContainerProperties(CosmosDB.ElectionDistrictContainerName, "/partitionKey") { DefaultTimeToLive = -1 }, cancellationToken: cancellationToken);
    }
});

app.MapPost("cosmos/regenerate/{voters:int}", async (CosmosClient client, int voters, CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity("Regenerating data in Cosmos database", ActivityKind.Client);

    try
    {
        return await RegenerateDataAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task<double> RegenerateDataAsync()
    {
        var electionContainer = client.ElectionContainer();
        var electionDistrictContainer = client.ElectionDistrictContainer();

        var totalRequestUnits = 0.0;

        CosmosModels.Election election = new()
        {
            ElectionId = DemoData.ElectionId,
            Name = "Scalection Demo Election",
        };

        totalRequestUnits += (await electionContainer.UpsertItemAsync(election)).RequestCharge;

        totalRequestUnits += await BulkUpsertAsync(
            electionContainer,
            GenerateItems("Party", NumberOfParties, election.ElectionId).Select(p =>
                new CosmosModels.Party
                {
                    ElectionId = election.ElectionId,
                    PartyId = p.Id,
                    Name = p.Name,
                    Candidates = GenerateItems("Candidate", NumberOfCandidatesPerParty, p.Id).Select(c =>
                    new CosmosModels.Candidate
                    {
                        CandidateId = c.Id,
                        Name = c.Name
                    }).ToList()
                }));


        totalRequestUnits += await BulkUpsertAsync(
            electionDistrictContainer,
            GenerateItems("District", NumberOfElectionDistricts, election.ElectionId).Select(d =>
            new CosmosModels.ElectionDistrict
            {
                ElectionId = election.ElectionId,
                ElectionDistrictId = d.Number,
                Name = d.Name
            }));

        totalRequestUnits += await BulkUpsertAsync(
            electionDistrictContainer,
            Enumerable.Range(1, voters).Select(i =>
            new CosmosModels.Voter
            {
                ElectionId = election.ElectionId,
                ElectionDistrictId = GetElectionDistrictId(i),
                VoterId = i,
                Voted = false,
            }));

        return totalRequestUnits;

        async Task<double> BulkUpsertAsync<T>(Container container, IEnumerable<T> items)
        {
            var sum = 0.0;
            int c = 0;
            foreach (var chunk in items.Chunk(BatchSize))
            {
                var tasks = chunk.Select(i => container.UpsertItemAsync(
                    i,
                    null,
                    new ItemRequestOptions() { EnableContentResponseOnWrite = false },
                    cancellationToken))
                .ToList();

                await Task.WhenAll(tasks);

                c++;
                Debug.WriteLine(c * BatchSize);

                sum += tasks.Sum(t => t.Result.RequestCharge);
            }

            return sum;
        }
    }
});

app.MapPost("cosmos/reset-votes/{voters:int}", async (CosmosClient client, int voters, CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity("Resetting votes in Cosmos database", ActivityKind.Client);

    try
    {
        return await ResetVotesAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task<double> ResetVotesAsync()
    {
        var electionDistrictContainer = client.ElectionDistrictContainer();

        var totalRequestUnits = 0.0;

        foreach (var chunk in Enumerable.Range(1, voters).Chunk(BatchSize))
        {
            var tasks = chunk.Select(i => electionDistrictContainer.PatchItemAsync<Voter>(
                CosmosEntity.CreateId<Voter>(i),
                new PartitionKey(ElectionDistrictEntity.CreatePartitionKey(DemoData.ElectionId, GetElectionDistrictId(i))),
                [PatchOperation.Set("/voted", false)],
                new PatchItemRequestOptions() { EnableContentResponseOnWrite = false },
                cancellationToken))
            .ToList();

            await Task.WhenAll(tasks);
            totalRequestUnits += tasks.Sum(t => t.Result.RequestCharge);
        }


        return totalRequestUnits;
    }
});

app.MapDefaultEndpoints();

app.Run();

static IEnumerable<Item> GenerateItems(string namePrefix, int count, Guid namespaceGuid)
{
    var format = new string('0', count.ToString().Length);

    for (int i = 1; i <= count; i++)
    {
        var name = $"{namePrefix} {i.ToString(format)}";
        var id = GuidHelpers.CreateFromName(namespaceGuid, i.ToString());
        yield return new Item(i, id, name);
    }
}

static int GetElectionDistrictId(int voterId) => (voterId % NumberOfElectionDistricts) + 1;

record Item(int Number, Guid Id, string Name);