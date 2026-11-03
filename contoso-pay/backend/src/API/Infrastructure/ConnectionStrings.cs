namespace API.Infrastructure;

public sealed class ConnectionStrings(IConfiguration configuration)
{
    public const string PostgresName = "Postgres";
    public const string MongoDbName = "MongoDB";

    public string Postgres { get { return GetRequired(PostgresName); } }
    public string MongoDb { get { return GetRequired(MongoDbName); } }

    public void Validate()
    {
        _ = Postgres;
        _ = MongoDb;
    }

    private string GetRequired(string name)
    {
        string? value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Connection string '{name}' is not configured.");
        }

        return value;
    }
}