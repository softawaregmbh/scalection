namespace Scalection.Data.Cosmos.Models
{
    public class Voter : ElectionDistrictEntity
    {
        public long VoterId { get; set; }

        public override string Id => CreateId(VoterId);

        public bool Voted { get; set; }
    }
}
