namespace LabApi.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {

        endpoints.MapGet("/", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Root endpoint called");
            return Results.Ok(new
            {
                app = "LabApi",
                message = "Lab API is running",
                endpoints = new[] { "/health", "/api/products" }
            });
        });

        endpoints.MapGet("/health", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Health check at {Time}", DateTimeOffset.UtcNow);
            return Results.Ok(new
            {
                status = "OK",
                time = DateTimeOffset.UtcNow
            });
        });

        return endpoints;
    }
}