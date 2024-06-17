using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OpenTelemetry.Trace;
using Scalection.Data.EF;
using Scalection.Data.EF.Models;
using Scalection.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<ScalectionContext>(ServiceDiscovery.SqlDB, s => s.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds);
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

app.MapPost("sql/reset/{voters:int}", async (ScalectionContext context, int voters, CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity("Resetting SQL database", ActivityKind.Client);

    try
    {
        await ResetDataAsync();
    }
    catch (Exception ex)
    {
        activity?.RecordException(ex);
        throw;
    }

    async Task ResetDataAsync()
    {
        Election election = new()
        {
            ElectionId = Election.DemoElectionId,
            Name = "EU Wahl 2024",
        };
        Party partyA = new()
        {
            PartyId = Guid.Parse("ba0161be-7ee6-4795-bb02-cb87a4103be1"),
            Name = "Party A",
            ElectionId = election.ElectionId,
        };
        Party partyB = new()
        {
            PartyId = Guid.Parse("8e1f28ef-f7a6-48a6-affc-84de83a60d88"),
            Name = "Party B",
            ElectionId = election.ElectionId,
        };
        Candidate candidateA1 = new()
        {
            CandidateId = Guid.Parse("7c16b002-6d9b-4867-943e-d7bf2b5d2b08"),
            Name = "Candidate A1",
            PartyId = partyA.PartyId,
        };
        Candidate candidateA2 = new()
        {
            CandidateId = Guid.Parse("501a1fcd-df4d-4fb9-828b-d3e0db31867e"),
            Name = "Candidate A2",
            PartyId = partyA.PartyId,
        };
        Candidate candidateB1 = new()
        {
            CandidateId = Guid.Parse("b4f6856c-d60d-44ff-802b-2d9efe3866a9"),
            Name = "Candidate B1",
            PartyId = partyB.PartyId,
        };

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Seed the database
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await AddOrUpdateAsync(election, e => e.ElectionId);
            await AddOrUpdateAsync(partyA, p => p.PartyId);
            await AddOrUpdateAsync(partyB, p => p.PartyId);
            await AddOrUpdateAsync(candidateA1, c => c.CandidateId);
            await AddOrUpdateAsync(candidateA2, c => c.CandidateId);
            await AddOrUpdateAsync(candidateB1, c => c.CandidateId);

            await context.SaveChangesAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                delete from Votes;
                delete from Voters;
                delete from ElectionDistricts;

                insert into ElectionDistricts(ElectionDistrictId, ElectionId, Name)
                select value, @p0, 'District ' + CONVERT(varchar(5), value)
                from GENERATE_SERIES(1, 10000);

                insert into Voters(VoterId, ElectionId, ElectionDistrictId, Voted)
                select value, @p0, (value % 10000) + 1, 0
                from GENERATE_SERIES(1, @p1);
                """,
                [election.ElectionId, voters],
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

app.MapDefaultEndpoints();

app.Run();