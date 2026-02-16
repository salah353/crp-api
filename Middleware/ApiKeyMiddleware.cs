using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly IHostEnvironment _env;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, IHostEnvironment env)
    {
        _next = next;
        _env = env;

        _apiKey = configuration["ApiSecurity:ApiKey"]
                  ?? throw new InvalidOperationException("API key not configured");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // ✅ Allow Swagger UI + OpenAPI JSON in Development
        if (_env.IsDevelopment() && IsDevDocsRequest(path))
        {
            await _next(context);
            return;
        }

        // ✅ Allow root "/" (browser won't send Accept: application/json)
        if (path == "/")
        {
            await _next(context);
            return;
        }

        //// Enforce Accept header
        //if (!context.Request.Headers.TryGetValue("Accept", out var accept) ||
        //    !accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        //{
        //    await Reject(context, HttpStatusCode.NotAcceptable,
        //        "Accept header must be application/json");
        //    return;
        //}

        //// Enforce Content-Type (only if body exists)
        //if (context.Request.ContentLength > 0 &&
        //    !(context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
        //{
        //    await Reject(context, HttpStatusCode.UnsupportedMediaType,
        //        "Content-Type must be application/json");
        //    return;
        //}

        //// Enforce API Key
        //if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey) ||
        //    !string.Equals(providedKey.ToString(), _apiKey, StringComparison.Ordinal))
        //{
        //    await Reject(context, HttpStatusCode.Unauthorized,
        //        "Invalid or missing API key");
        //    return;
        //}

        await _next(context);
    }

    private static bool IsDevDocsRequest(PathString path) =>
        path.StartsWithSegments("/swagger") ||
        path.StartsWithSegments("/openapi") ||
        path.StartsWithSegments("/favicon.ico");

    private static async Task Reject(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
  