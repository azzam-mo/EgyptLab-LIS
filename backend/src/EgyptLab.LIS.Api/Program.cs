var builder = WebApplication.CreateBuilder(args);

// OpenAPI is development-only; it is not part of the production surface.
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health")
   .WithTags("Operations");

app.Run();

// Exposed for integration tests using WebApplicationFactory.
public partial class Program;
