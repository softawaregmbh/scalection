using System.ComponentModel.DataAnnotations;

namespace Scalection.ApiService.Models
{
    public class Candidate
    {
        public Guid CandidateId { get; set; }

        [Required]
        public required string Name { get; set; }

        public Guid PartyId { get; set; }
    }
}
