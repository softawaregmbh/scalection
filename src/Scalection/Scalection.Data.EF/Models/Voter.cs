namespace Scalection.Data.EF.Models
{
    public class Voter
    {
        public long VoterId { get; set; }

        public long ElectionDistrictId { get; set; }

        public bool Voted { get; set; }
    }
}
