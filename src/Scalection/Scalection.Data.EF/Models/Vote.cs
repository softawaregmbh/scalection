namespace Scalection.Data.EF.Models
{
    public class Vote
    {
        public Guid VoteId { get; set; }

        public Guid ElectionDistrictId { get; set; }

        public Guid PartyId { get; set; }

        public Guid? CandidateId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
