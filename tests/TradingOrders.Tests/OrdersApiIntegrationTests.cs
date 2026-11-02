using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TradingOrders.Application;
using Xunit;

namespace TradingOrders.Tests;

public sealed class OrdersApiIntegrationTests
{
    [Fact]
    public async Task Create_then_retry_and_conflict_follows_http_contract()
    {
        using var sourceFactory = new WebApplicationFactory<Program>();
        var path = Path.Combine(Path.GetTempPath(), $"trading-test-{Guid.NewGuid():N}.db");
        using var factory = MakeFactory(sourceFactory, path);
        using var client = factory.CreateClient();

        var request = new { clientOrderId = "request-001", symbol = "msft", side = "Buy", quantity = 2.5m, limitPrice = 350.50m };
        using var first = await client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);
        Assert.Equal("MSFT", created.Symbol);

        using var retry = await client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var replayed = await retry.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(replayed);
        Assert.Equal(created.Id, replayed.Id);

        using var conflict = await client.PostAsJsonAsync("/api/orders", new
        {
            clientOrderId = "request-001", symbol = "MSFT", side = "Buy", quantity = 3.5m, limitPrice = 350.50m
        });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Cancel_twice_keeps_same_timestamp()
    {
        using var sourceFactory = new WebApplicationFactory<Program>();
        var path = Path.Combine(Path.GetTempPath(), $"trading-test-{Guid.NewGuid():N}.db");
        using var factory = MakeFactory(sourceFactory, path);
        using var client = factory.CreateClient();

        using var createdHttp = await client.PostAsJsonAsync("/api/orders", new
        {
            clientOrderId = "request-002", symbol = "AAPL", side = "Sell", quantity = 1m, limitPrice = 160m
        });
        var created = await createdHttp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);
        using var firstHttp = await client.PostAsync($"/api/orders/{created.Id}/cancel", null);
        using var secondHttp = await client.PostAsync($"/api/orders/{created.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, firstHttp.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondHttp.StatusCode);
        var first = await firstHttp.Content.ReadFromJsonAsync<OrderDto>();
        var second = await secondHttp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("Cancelled", second.Status);
        Assert.Equal(first.CancelledAtUtc, second.CancelledAtUtc);
    }

    [Fact]
    public async Task Invalid_order_is_rejected()
    {
        using var sourceFactory = new WebApplicationFactory<Program>();
        var path = Path.Combine(Path.GetTempPath(), $"trading-test-{Guid.NewGuid():N}.db");
        using var factory = MakeFactory(sourceFactory, path);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/orders", new
        {
            clientOrderId = "bad-order", symbol = "AAPL", side = "Buy", quantity = -1m, limitPrice = 100m
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> MakeFactory(WebApplicationFactory<Program> source, string path) =>
        source.WithWebHostBuilder(webHostBuilder =>
            webHostBuilder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TradingOrders"] = $"Data Source={path}"
                })));
}
