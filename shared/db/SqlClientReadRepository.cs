using Microsoft.Data.SqlClient;

namespace GearFlow.Lambda.Shared.Db;

/// <summary>
/// Leitura readonly de cliente via SQL cru contra o schema do Customers (mesmo padrão de
/// SqlCustomerContactReader no monólito: leitura cross-BC sem referência de projeto).
/// </summary>
public sealed class SqlClientReadRepository : IClientReadRepository
{
    private readonly string _connectionString;

    public SqlClientReadRepository(string connectionString) => _connectionString = connectionString;

    public async Task<ClientLookupResult?> FindByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT id, cpf, email, name, status
                             FROM customers.clients
                             WHERE cpf = @cpf";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@cpf", cpf);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new ClientLookupResult
        {
            ClientId = reader.GetGuid(0).ToString(),
            Cpf = reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            UserName = reader.IsDBNull(3) ? null : reader.GetString(3),
            Status = reader.GetString(4)
        };
    }
}
