using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.CustomerService.Tests.Services;

public class IAMClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<IAMClient>> _loggerMock;
    private readonly IAMClient _iamClient;

    public IAMClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://test-iam")
        };
        _loggerMock = new Mock<ILogger<IAMClient>>();
        _iamClient = new IAMClient(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePrincipalAsync_Success_ReturnsResponse()
    {
        // Arrange
        var request = new CreatePrincipalRequest
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        var expectedResponse = new CreatePrincipalResponse
        {
            PrincipalId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().EndsWith("/iam/v1/service-accounts")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse)
            });

        // Act
        var result = await _iamClient.CreatePrincipalAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.PrincipalId, result.PrincipalId);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CreatePrincipalAsync_Error_ThrowsHttpRequestException()
    {
        // Arrange
        var request = new CreatePrincipalRequest { Email = "error@example.com" };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _iamClient.CreatePrincipalAsync(request));
    }

    [Fact]
    public async Task CreatePrincipalAsync_EmptyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreatePrincipalRequest { Email = "empty@example.com" };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _iamClient.CreatePrincipalAsync(request));
    }
}
