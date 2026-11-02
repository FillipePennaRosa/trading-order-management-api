using Microsoft.EntityFrameworkCore;
using TradingOrders.Application;
using TradingOrders.Domain;
using TradingOrders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TradingOrders")
        ?? "Data Source=trading-orders.db"));
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Demo-only bootstrapping. Real services should use versioned EF migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/orders", async (CreateOrderRequest request, OrderService service, CancellationToken ct) =>
{
    if (!Enum.TryParse<OrderSide>(request.Side, true, out var side) || !Enum.IsDefined(side))
        return Results.BadRequest(new { error = "Side must be Buy or Sell." });

    try
    {
        var result = await service.CreateAsync(new CreateOrderCommand(
            request.ClientOrderId ?? string.Empty,
            request.Symbol ?? string.Empty,
            side, request.Quantity, request.LimitPrice), ct);

        return result.Replayed
            ? Results.Ok(result.Order)
            : Results.Created($"/api/orders/{result.Order.Id}", result.Order);
    }
    catch (OrderValidationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (OrderConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
});

app.MapGet("/api/orders/{id:guid}", async (Guid id, OrderService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.GetAsync(id, ct)); }
    catch (OrderNotFoundException) { return Results.NotFound(); }
});

app.MapGet("/api/orders", async (int? limit, OrderService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.ListAsync(limit ?? 20, ct)); }
    catch (OrderValidationException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/orders/{id:guid}/cancel", async (Guid id, OrderService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.CancelAsync(id, ct)); }
    catch (OrderNotFoundException) { return Results.NotFound(); }
});

app.Run();

public sealed record CreateOrderRequest(
    string? ClientOrderId, string? Symbol, string? Side, decimal Quantity, decimal LimitPrice);

// Enables API integration tests using WebApplicationFactory<Program>.
public partial class Program { }
