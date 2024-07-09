using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.EF.Models
{
    public class ElectionDistrict
    {
        public long ElectionDistrictId { get; set; }

        public Guid ElectionId { get; set; }

        [Required]
        public required string Name { get; set; }
    }
}
