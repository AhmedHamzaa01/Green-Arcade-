using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RowCycle.Api.Conventions;
using RowCycle.Api.Middleware;
using RowCycle.Application;
using RowCycle.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options => options.Conventions.Add(new RoutePrefixConvention("api/v1")));
builder.Services.AddOpenApi();

// RFC 7807 bodies for exceptions and bare status codes (404, 401, ...).
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier);

builder.Services.AddHealthChecks()
    .AddNpgSql(RowCycle.Infrastructure.DependencyInjection.GetConnectionString(builder.Configuration), name: "postgres");

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Green Arcade API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    // In containers TLS ends at the reverse proxy; locally the API runs on plain HTTP.
    app.UseHttpsRedirection();
}

app.UseCors("web");
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthResponse });
app.MapControllers();

await app.RunAsync();

static Task WriteHealthResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    var body = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonSerializerOptions.Web));
}

public partial class Program;
