namespace Scalection.Data.Cosmos.Models
{
    public abstract class CosmosEntity
    {
        public abstract string Id { get; }

        public string Type => GetName(GetType());

        public string CreateId(object id) => CreateId(GetType(), id);

        public static string CreateId<TEntity>(object id)
            where TEntity : CosmosEntity => CreateId(typeof(TEntity), id);

        public static string GetName<TEntity>()
            where TEntity : CosmosEntity => GetName(typeof(TEntity));

        private static string CreateId(Type t, object id) =>
            $"{GetName(t)}-{id}";

        private static string GetName(Type t) => t.Name;
    }
}
