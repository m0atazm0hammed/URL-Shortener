using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Services;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Concurrency;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Workers;
using UrlShortener.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "UrlShortener_";
});

builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<UrlShorteningService>();

builder.Services.AddSingleton<InMemoryKeyBuffer>();
builder.Services.AddSingleton<IKeyBuffer>(sp => sp.GetRequiredService<InMemoryKeyBuffer>());
builder.Services.AddHostedService<KeyGenerationWorker>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("5-per-10-seconds", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "URL Shortener API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "URL Shortener API v1"));
}

app.UseExceptionHandler();
app.UseRateLimiter();



app.MapPost("/api/v1/shorten", async (
    ShortenUrlRequest request,
    UrlShorteningService service,
    CancellationToken cancellationToken) =>
{
    if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
        return Results.BadRequest(new { Error = "Invalid URL format." });

    var shortCode = await service.ShortenAsync(request.OriginalUrl, cancellationToken);
    var host = $"{app.Configuration["BaseUrl"] ?? "http://localhost:5000"}/";

    return Results.Ok(new ShortenUrlResponse($"{host}{shortCode}"));
})
.WithName("CreateShortUrl")
.WithSummary("Creates a highly concurrent shortened URL")
.Produces<ShortenUrlResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status429TooManyRequests)
.RequireRateLimiting("5-per-10-seconds");

app.MapGet("/{shortCode}", async (
    string shortCode,
    UrlShorteningService service,
    CancellationToken cancellationToken) =>
{
    var originalUrl = await service.GetOriginalUrlAsync(shortCode, cancellationToken);

    if (string.IsNullOrEmpty(originalUrl))
        return Results.NotFound();

    return Results.Redirect(originalUrl, permanent: true);
})
.WithName("RedirectToOriginal")
.WithSummary("Redirects a short code to its original URL")
.Produces(StatusCodes.Status301MovedPermanently)
.Produces(StatusCodes.Status404NotFound)
.RequireRateLimiting("5-per-10-seconds");

app.Run();
