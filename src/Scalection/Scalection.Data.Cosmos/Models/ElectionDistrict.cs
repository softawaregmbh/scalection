using System.ComponentModel.DataAnnotations;

namespace Scalection.Data.Cosmos.Models
{
    public class ElectionDistrict : ElectionDistrictEntity
    {
        public override string Id => CreateId(ElectionDistrictId);

        [Required]
        public required string Name { get; set; }
    }
}
