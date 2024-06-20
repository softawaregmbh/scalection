using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Scalection.Data.Cosmos.Models;
using Scalection.ServiceDefaults;

namespace Scalection.Data.Cosmos;

public static class CosmosDB
{
    public const string ElectionContainerName = "elections";
    public const string ElectionDistrictContainerName = "election-districts";

    public static Container ElectionContainer(this CosmosClient client) =>
        client.GetContainer(ServiceDiscovery.CosmosDB, ElectionContainerName);

    public static Container ElectionDistrictContainer(this CosmosClient client) =>
        client.GetContainer(ServiceDiscovery.CosmosDB, ElectionDistrictContainerName);

    public static IQueryable<Election> Elections(this CosmosClient client) =>
        client.ElectionContainer().TypedQuery<Election>();

    public static IQueryable<Party> Parties(this CosmosClient client) =>
        client.ElectionContainer().TypedQuery<Party>();

    public static IQueryable<ElectionDistrict> ElectionDistricts(this CosmosClient client) =>
        client.ElectionDistrictContainer().TypedQuery<ElectionDistrict>();

    public static IQueryable<Voter> Voters(this CosmosClient client) =>
        client.ElectionDistrictContainer().TypedQuery<Voter>();

    private static IQueryable<T> TypedQuery<T>(this Container container)
        where T : CosmosEntity =>
        container.GetItemLinqQueryable<T>()
                 .Where(e => e.Type == CosmosEntity.GetName<T>());

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> queryable, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var iterator = queryable.ToFeedIterator<T>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in page)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }

    public static async Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        var iterator = queryable.ToFeedIterator();
        if (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            return page.FirstOrDefault();
        }

        return default;
    }
}
