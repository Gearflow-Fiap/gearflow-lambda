namespace GearFlow.Lambda.Shared.Db;

/// <summary>
/// Factory de conexão readonly. A implementação concreta (Npgsql/EF/etc.)
/// será ligada quando o Repo 3 estiver disponível.
/// </summary>
public sealed class ReadonlyDbConnectionFactory
{
    public string ConnectionString { get; }

    public ReadonlyDbConnectionFactory(string? connectionString = null)
    {
        ConnectionString = connectionString
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
