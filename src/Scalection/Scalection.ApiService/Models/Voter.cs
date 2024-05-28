namespace Scalection.ApiService.Models
{
    public class Voter
    {
        public long VoterId { get; set; }

        public Guid ElectionDistrictId { get; set; }

        public bool Voted { get; set; }
    }
}
