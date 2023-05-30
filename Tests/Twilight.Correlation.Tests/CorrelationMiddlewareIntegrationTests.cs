using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RT.Comb;
using Xunit;

namespace Twilight.Correlation.Tests;

public sealed class CorrelationMiddlewareIntegrationTests : IAsyncLifetime
{
    private IHost _host;

    [Fact]
    public async Task CorrelationMiddleware_ReturnsSingleCorrelationHeader()
    {
        // Arrange
        using var client = _host.GetTestClient();

        var requestCorrelationId = new PostgreSqlCombProvider(new UnixDateTimeStrategy()).Create().ToString();
        var headerValues = new List<string> { requestCorrelationId };

        client.DefaultRequestHeaders.Add(RequestHeaderKey.CorrelationIdHeader, headerValues);

        // Act
        var response = await client.GetAsync("/10");

        // Assert
        response.Headers.TryGetValues(RequestHeaderKey.CorrelationIdHeader, out var headers).Should().BeTrue();

        var headerList = (headers ?? Enumerable.Empty<string>()).ToList();

        headerList.Should().NotBeEmpty();
        headerList.Count.Should().Be(1);
    }

    [Fact]
    public async Task CorrelationMiddleware_WithCorrelationId_ReturnsCorrelationId()
    {
        // Arrange
        using var client = _host.GetTestClient();

        var requestCorrelationId = new PostgreSqlCombProvider(new UnixDateTimeStrategy()).Create().ToString();
        var headerValues = new List<string> { requestCorrelationId };

        client.DefaultRequestHeaders.Add(RequestHeaderKey.CorrelationIdHeader, headerValues);

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.TryGetValues(RequestHeaderKey.CorrelationIdHeader, out var headers).Should().BeTrue();

        var headerList = (headers ?? Enumerable.Empty<string>()).ToList();
        var header = headerList.FirstOrDefault();

        Guid.TryParse(header, out var responseCorrelationId).Should().BeTrue();

        responseCorrelationId.Should().Be(requestCorrelationId);
    }

    [Fact]
    public async Task CorrelationMiddleware_WithoutCorrelationId_AddsCorrelationId()
    {
        // Arrange
        using var client = _host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.TryGetValues(RequestHeaderKey.CorrelationIdHeader, out var headers).Should().BeTrue();

        var headerList = (headers ?? Enumerable.Empty<string>()).ToList();
        var header = headerList.FirstOrDefault();

        Guid.TryParse(header, out var responseCorrelationId).Should().BeTrue();

        responseCorrelationId.Should().NotBeEmpty();
    }

    public async Task InitializeAsync()
        => _host = await new HostBuilder().ConfigureServices(services =>
        {
            services.AddSingleton<ICombDateTimeStrategy, UnixDateTimeStrategy>()
                    .AddSingleton<ICombProvider, PostgreSqlCombProvider>()
                    .AddSingleton<ICorrelationProvider, CorrelationProvider>()
                    .AddScoped<CorrelationMiddleware>()
                    .AddRouting();
        })
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseTestServer()
                      .Configure(app =>
                      {
                          app.UseMiddleware<CorrelationMiddleware>()
                             .UseRouting()
                             .UseEndpoints(endpoints => { endpoints.MapGet("/", () => string.Empty); });
                      });
        })
        .StartAsync();

    public async Task DisposeAsync()
    {
        _host.Dispose();

        await Task.CompletedTask;
    }
}
