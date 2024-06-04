using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using OpenTelemetry.Trace;
using Scalection.Data.EF;
using Scalection.Data.EF.Models;

namespace Scalection.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
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
            ElectionId = Guid.NewGuid(),
            Name = "EU Wahl 2024",
        };
        Party partyA = new()
        {
            PartyId = Guid.NewGuid(),
            Name = "Party A",
            ElectionId = election.ElectionId,
        };
        Party partyB = new()
        {
            PartyId = Guid.NewGuid(),
            Name = "Party B",
            ElectionId = election.ElectionId,
        };
        Candidate candidateA1 = new()
        {
            CandidateId = Guid.NewGuid(),
            Name = "Candidate A1",
            PartyId = partyA.PartyId,
        };
        Candidate candidateA2 = new()
        {
            CandidateId = Guid.NewGuid(),
            Name = "Candidate A2",
            PartyId = partyA.PartyId,
        };
        Candidate candidateB1 = new()
        {
            CandidateId = Guid.NewGuid(),
            Name = "Candidate B1",
            PartyId = partyB.PartyId,
        };

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Seed the database
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.Elections.AddAsync(election, cancellationToken);
            await dbContext.Parties.AddAsync(partyA, cancellationToken);
            await dbContext.Parties.AddAsync(partyB, cancellationToken);
            await dbContext.Candidates.AddAsync(candidateA1, cancellationToken);
            await dbContext.Candidates.AddAsync(candidateA2, cancellationToken);
            await dbContext.Candidates.AddAsync(candidateB1, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}