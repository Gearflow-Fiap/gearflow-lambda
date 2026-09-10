using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using GearFlow.Lambda.CheckClient;
using GearFlow.Lambda.Shared.Db;
using NSubstitute;
using Xunit;

namespace GearFlow.Lambda.Tests;

public sealed class CheckClientFunctionTests
{
    private const string ValidCpf = "52998224725";

    private static APIGatewayHttpApiV2ProxyRequest RequestFor(string cpf) => new()
    {
        Body = JsonSerializer.Serialize(new { cpf })
    };

    [Fact]
    public async Task FunctionHandler_returns_ok_with_client_data_when_cpf_is_found()
    {
        var repository = Substitute.For<IClientReadRepository>();
        repository.FindByCpfAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns(new ClientLookupResult
            {
                ClientId = "11111111-1111-1111-1111-111111111111",
                Cpf = ValidCpf,
                Email = "cliente@example.com",
                UserName = "Cliente Exemplo",
                Status = "active"
            });
        var function = new Function(repository);

        var response = await function.FunctionHandler(RequestFor(ValidCpf), new TestLambdaContext());

        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.GetProperty("found").GetBoolean().Should().BeTrue();
        body.RootElement.GetProperty("clientId").GetString().Should().Be("11111111-1111-1111-1111-111111111111");
        body.RootElement.GetProperty("cpf").GetString().Should().Be(ValidCpf);
        body.RootElement.GetProperty("email").GetString().Should().Be("cliente@example.com");
        body.RootElement.GetProperty("status").GetString().Should().Be("active");
    }

    [Fact]
    public async Task FunctionHandler_returns_not_found_when_cpf_does_not_exist()
    {
        var repository = Substitute.For<IClientReadRepository>();
        repository.FindByCpfAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns((ClientLookupResult?)null);
        var function = new Function(repository);

        var response = await function.FunctionHandler(RequestFor(ValidCpf), new TestLambdaContext());

        response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.GetProperty("found").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task FunctionHandler_returns_internal_server_error_without_leaking_exception_when_repository_fails()
    {
        var repository = Substitute.For<IClientReadRepository>();
        repository.FindByCpfAsync(ValidCpf, Arg.Any<CancellationToken>())
            .Returns<Task<ClientLookupResult?>>(_ => throw new InvalidOperationException("connection refused: 10.0.0.5:1433"));
        var function = new Function(repository);

        var response = await function.FunctionHandler(RequestFor(ValidCpf), new TestLambdaContext());

        response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        response.Body.Should().NotContain("connection refused");
        response.Body.Should().NotContain("InvalidOperationException");
    }
}
