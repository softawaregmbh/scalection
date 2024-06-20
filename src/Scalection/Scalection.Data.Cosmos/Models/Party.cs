using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.Cosmos.Models
{
    public class Party : CosmosEntity
    {
        public Guid PartyId { get; set; }

        public override string Id => CreateId(PartyId);

        public Guid ElectionId { get; set; }

        [Required]
        public required string Name { get; set; }

        public List<Candidate> Candidates { get; set; } = new();
    }
}
