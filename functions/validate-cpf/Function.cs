using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GearFlow.Lambda.ValidateCpf;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            var payload = DeserializeBody<ValidateCpfRequest>(request.Body);
            var normalized = CpfValidator.OnlyNumbers(payload?.Cpf);
            var valid = CpfValidator.IsValidCpf(normalized);

            if (!valid)
            {
                return Response(HttpStatusCode.BadRequest, new
                {
                    valid = false,
                    message = "CPF inválido."
                });
            }

            return Response(HttpStatusCode.OK, new
            {
                valid = true,
                cpf = normalized
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
