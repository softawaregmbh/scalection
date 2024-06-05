using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using OpenTelemetry.Trace;
using Scalection.Data.EF;
using Scalection.Data.EF.Models;
using Scalection.ServiceDefaults;

namespace Scalection.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    private static readonly Guid ElectionId = Guid.Parse("AF555808-063A-4EEB-9EB2-77090A2BFF42");

    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ScalectionContext>();

            await EnsureDatabaseAsync(dbContext, cancellationToken);
            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task EnsureDatabaseAsync(ScalectionContext dbContext, CancellationToken cancellationToken)
    {
        var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
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

    private static async Task RunMigrationAsync(ScalectionContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.ExecuteSqlRawAsync($"ALTER DATABASE {ServiceDiscovery.SqlDB} SET COMPATIBILITY_LEVEL = 160", cancellationToken);

            // Run migration in a transaction to avoid partial migration if it fails.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static async Task SeedDataAsync(ScalectionContext dbContext, CancellationToken cancellationToken)
    {
        Election election = new()
        {
            ElectionId = ElectionId,
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

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Seed the database
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await AddOrUpdateAsync(election, e => e.ElectionId);
            await AddOrUpdateAsync(partyA, p => p.PartyId);
            await AddOrUpdateAsync(partyB, p => p.PartyId);
            await AddOrUpdateAsync(candidateA1, c => c.CandidateId);
            await AddOrUpdateAsync(candidateA2, c => c.CandidateId);
            await AddOrUpdateAsync(candidateB1, c => c.CandidateId);

            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync("""
                delete from Votes;
                delete from Voters;
                delete from ElectionDistricts;

                insert into ElectionDistricts(ElectionDistrictId, ElectionId, Name)
                select value, @p0, 'District ' + CONVERT(varchar(5), value)
                from GENERATE_SERIES(1, 10000);

                insert into Voters(VoterId, ElectionDistrictId, Voted)
                select value, (value % 10000) + 1, 0
                from GENERATE_SERIES(1, 10000000);
                """,
                [ElectionId],
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        async Task AddOrUpdateAsync<T, TId>(T entity, Func<T, TId> idSelector)
            where T : class
        {
            var dbSet = dbContext.Set<T>();
            var dbEntity = await dbSet.FindAsync(idSelector(entity));
            if (dbEntity == null)
            {
                dbSet.Add(entity);
            }
            else
            {
                dbSet.Update(entity);
            }
        }
    }
}