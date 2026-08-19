using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GearFlow.Lambda.GenerateToken;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JwtSettings _jwtSettings;

    public Function()
        : this(JwtSettings.FromEnvironment())
    {
    }

    public Function(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_jwtSettings.SigningKey))
            {
                context.Logger.LogError("JWT_SIGNING_KEY não configurada.");
                return Response(HttpStatusCode.InternalServerError, new
                {
                    message = "Configuração JWT ausente."
                });
            }

            var payload = DeserializeBody<GenerateTokenRequest>(request.Body);

            if (string.IsNullOrWhiteSpace(payload?.ClientId))
            {
                return Response(HttpStatusCode.BadRequest, new
                {
                    message = "clientId é obrigatório."
                });
            }

            var (accessToken, expiresAt) = JwtTokenGenerator.Generate(payload, _jwtSettings);

            return Response(HttpStatusCode.OK, new
            {
                accessToken,
                accessTokenExpiresAtUtc = expiresAt
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
