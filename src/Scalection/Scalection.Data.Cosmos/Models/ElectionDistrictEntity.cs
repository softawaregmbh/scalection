namespace Scalection.Data.Cosmos.Models;

public abstract class ElectionDistrictEntity : CosmosEntity
{
    public Guid ElectionId { get; set; }
    public long ElectionDistrictId { get; set; }
    public string PartitionKey => CreatePartitionKey(ElectionId, ElectionDistrictId);

    public static string CreatePartitionKey(Guid electionId, long electionDistrictId) => $"{electionId}/{electionDistrictId}";
}
