namespace GearFlow.Lambda.Shared.Db;

/// <summary>
/// Stub temporário até a consulta real ao Repo 3.
/// </summary>
public sealed class StubClientReadRepository : IClientReadRepository
{
    public Task<ClientLookupResult?> FindByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        // Placeholder: troca pela query readonly quando o banco estiver integrado.
        return Task.FromResult<ClientLookupResult?>(null);
    }
}
