using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ReactCore.Backend.Exceptions;
using ReactCore.Backend.Options;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Tests.Unit;

public sealed class WeatherApiServiceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private static WeatherApiService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder, ExternalApiOptions? options = null)
    {
        var handler = new StubHttpMessageHandler(responder);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("OpenWeatherMap")).Returns(client);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<WeatherApiService>>();

        return new WeatherApiService(
            factory.Object,
            memoryCache,
            Options.Create(options ?? new ExternalApiOptions
            {
                ApiKey = "test-key",
                CircuitBreakerThreshold = 2,
                CircuitBreakerTimeoutSeconds = 60
            }),
            logger.Object);
    }

    [Fact]
    public async Task FetchWeatherAsync_Throws_WhenApiKeyMissing()
    {
        var svc = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            options: new ExternalApiOptions { ApiKey = "${OPENWEATHERMAP_API_KEY}" });

        var act = async () => await svc.FetchWeatherAsync("Santo Domingo");
        await act.Should().ThrowAsync<ApiUnavailableException>();
    }

    [Fact]
    public async Task FetchWeatherAsync_ThrowsArgumentException_WhenNotFound()
    {
        var svc = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var act = async () => await svc.FetchWeatherAsync("Nowhere");
        var ex = await act.Should().ThrowAsync<ArgumentException>();
        ex.Which.ParamName.Should().Be("location");
    }

    [Fact]
    public async Task FetchWeatherAsync_RetriesAndSucceeds_AfterNonTransientHttpError()
    {
        var callCount = 0;
        var svc = CreateService(_ =>
        {
            callCount++;

            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad-request")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"main\":{\"temp\":20,\"humidity\":55},\"weather\":[{\"description\":\"light rain\"}],\"wind\":{\"speed\":2.2},\"name\":\"Santo Domingo\"}")
            };
        });

        var dto = await svc.FetchWeatherAsync("Santo Domingo");

        dto.Temperature.Should().Be(20);
        dto.Condition.Should().Be("light rain");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task FetchWeatherAsync_RetriesOnce_ForTransientHttpError()
    {
        var callCount = 0;
        var svc = CreateService(_ =>
        {
            callCount++;

            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"main\":{\"temp\":21,\"humidity\":60},\"weather\":[{\"main\":\"Clouds\"}],\"wind\":{\"speed\":3.2},\"name\":\"Santo Domingo\"}")
            };
        });

        var dto = await svc.FetchWeatherAsync("Santo Domingo");

        dto.Temperature.Should().Be(21);
        dto.Condition.Should().Be("Clouds");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task FetchWeatherAsync_OpensCircuitBreaker_AfterRepeatedFailures_AndBlocksNextCall()
    {
        // Keep retries minimal for this test: threshold 1 means a single terminal failure opens the circuit.
        var callCount = 0;
        var svc = CreateService(
            _ =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("server-error")
                };
            },
            options: new ExternalApiOptions
            {
                ApiKey = "test-key",
                CircuitBreakerThreshold = 1,
                CircuitBreakerTimeoutSeconds = 60
            });

        var first = async () => await svc.FetchWeatherAsync("Santo Domingo");
        await first.Should().ThrowAsync<ApiUnavailableException>();

        var second = async () => await svc.FetchWeatherAsync("Santo Domingo");
        await second.Should().ThrowAsync<ApiUnavailableException>().WithMessage("*circuit breaker is open*");

        callCount.Should().BeGreaterThan(1);
    }
}
