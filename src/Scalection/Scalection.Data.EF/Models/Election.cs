using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.EF.Models
{
    public class Election
    {
        public static readonly Guid DemoElectionId = Guid.Parse("af555808-063a-4eeb-9eb2-77090a2bff42");

        public Guid ElectionId { get; set; }

        [Required]
        public required string Name { get; set; }
    }
}
