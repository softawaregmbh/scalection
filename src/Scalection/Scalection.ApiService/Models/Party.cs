using System.ComponentModel.DataAnnotations;

namespace Scalection.ApiService.Models
{
    public class Party
    {
        public Guid PartyId { get; set; }

        [Required]
        public required string Name { get; set; }

        public Guid ElectionId { get; set; }

        public Election? Election { get; set; }

        public ICollection<Candidate> Candidates { get; } = new List<Candidate>();
    }
}
