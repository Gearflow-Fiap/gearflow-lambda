namespace GearFlow.Lambda.Shared.Db;

/// <summary>
/// Leitura de cliente no banco do Repo 3 (somente readonly).
/// </summary>
public interface IClientReadRepository
{
    Task<ClientLookupResult?> FindByCpfAsync(string cpf, CancellationToken cancellationToken = default);
}
