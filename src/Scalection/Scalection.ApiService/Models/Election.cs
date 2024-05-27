using System.ComponentModel.DataAnnotations;

namespace Scalection.ApiService.Models
{
    public class Election
    {
        public Guid ElectionId { get; set; }

        [Required]
        public required string Name { get; set; }
    }
}
