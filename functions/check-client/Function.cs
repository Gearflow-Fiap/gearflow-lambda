using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GearFlow.Lambda.Shared.Db;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GearFlow.Lambda.CheckClient;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IClientReadRepository _clientReadRepository;

    public Function()
        : this(CreateDefaultRepository())
    {
    }

    public Function(IClientReadRepository clientReadRepository)
    {
        _clientReadRepository = clientReadRepository;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            var payload = DeserializeBody<CheckClientRequest>(request.Body);
            var cpf = OnlyNumbers(payload?.Cpf);

            if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
            {
                return Response(HttpStatusCode.BadRequest, new
                {
                    found = false,
                    message = "CPF é obrigatório e deve conter 11 dígitos."
                });
            }

            var client = await _clientReadRepository.FindByCpfAsync(cpf);

            if (client is null)
            {
                return Response(HttpStatusCode.NotFound, new
                {
                    found = false,
                    message = "Cliente não encontrado."
                });
            }

            return Response(HttpStatusCode.OK, new
            {
                found = true,
                clientId = client.ClientId,
                cpf = client.Cpf,
                email = client.Email,
                userName = client.UserName,
                status = client.Status
            });
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"JSON inválido: {ex.Message}");
            return Response(HttpStatusCode.BadRequest, new { message = "Body JSON inválido." });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Erro inesperado: {ex}");
            return Response(HttpStatusCode.InternalServerError, new { message = "Erro interno." });
        }
    }

    private static IClientReadRepository CreateDefaultRepository()
    {
        var connectionFactory = new ReadonlyDbConnectionFactory();
        return connectionFactory.IsConfigured
            ? new SqlClientReadRepository(connectionFactory.ConnectionString)
            : new StubClientReadRepository();
    }

    private static string OnlyNumbers(string? src)
    {
        if (string.IsNullOrEmpty(src))
            return string.Empty;

        return new string(src.Where(char.IsDigit).ToArray());
    }

    private static T? DeserializeBody<T>(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return default;

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static APIGatewayHttpApiV2ProxyResponse Response(HttpStatusCode status, object body) => new()
    {
        StatusCode = (int)status,
        Headers = new Dictionary<string, string> { ["content-type"] = "application/json" },
        Body = JsonSerializer.Serialize(body, JsonOptions)
    };
}
