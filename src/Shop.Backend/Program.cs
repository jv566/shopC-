using Shop.Contracts.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "Shop.Backend",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/bootstrap/products", () =>
{
    var data = new[]
    {
        new ProductDto(Guid.NewGuid(), "Keyboard", "KB-001", 199, "CNY"),
        new ProductDto(Guid.NewGuid(), "Mouse", "MS-001", 99, "CNY")
    };

    return Results.Ok(data);
});

app.Run();

