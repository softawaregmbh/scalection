using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.Cosmos.Models
{
    public class Candidate
    {
        public Guid CandidateId { get; set; }

        [Required]
        public required string Name { get; set; }
    }
}
