using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.EF.Models
{
    public class Election
    {
        public Guid ElectionId { get; set; }

        [Required]
        public required string Name { get; set; }
    }
}
