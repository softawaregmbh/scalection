using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.Cosmos.Models
{
    public class Election : CosmosEntity
    {
        public static readonly Guid DemoElectionId = Guid.Parse("af555808-063a-4eeb-9eb2-77090a2bff42");

        public Guid ElectionId { get; set; }

        public override string Id => CreateId(ElectionId);

        [Required]
        public required string Name { get; set; }
    }
}
