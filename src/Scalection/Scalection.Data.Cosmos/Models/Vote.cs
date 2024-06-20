namespace Scalection.Data.Cosmos.Models
{
    public class Vote : ElectionDistrictEntity
    {
        public Guid VoteId { get; set; }

        public override string Id => CreateId(VoteId);

        public Guid PartyId { get; set; }

        public Guid? CandidateId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
