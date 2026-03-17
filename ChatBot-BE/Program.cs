using ChatBot_BE.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

static string[] GetValidatedCorsOrigins(IConfiguration configuration, string configKey)
{
    var rawOrigins = configuration.GetSection(configKey).Get<string[]>() ?? Array.Empty<string>();

    var normalized = new List<string>();
    var invalid = new List<string>();

    foreach (var raw in rawOrigins)
    {
        var s = (raw ?? string.Empty).Trim();
        if (s.Length == 0) continue;

        if (s.Contains('*', StringComparison.Ordinal))
        {
            invalid.Add(raw ?? string.Empty);
            continue;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            invalid.Add(raw ?? string.Empty);
            continue;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            invalid.Add(raw ?? string.Empty);
            continue;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            invalid.Add(raw ?? string.Empty);
            continue;
        }

        if (uri.AbsolutePath != "/" && uri.AbsolutePath.Length != 0)
        {
            invalid.Add(raw ?? string.Empty);
            continue;
        }

        var origin = uri.GetLeftPart(UriPartial.Authority);
        if (origin.Length == 0)
        {
            invalid.Add(raw ?? string.Empty);
            continue;
        }

        if (!normalized.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            normalized.Add(origin);
        }
    }

    if (invalid.Count > 0)
    {
        var formatted = string.Join(", ", invalid.Select(v => $"'{v}'"));
        throw new InvalidOperationException(
            $"Invalid CORS origin(s) in '{configKey}': {formatted}. " +
            "Each origin must be an absolute http/https URL like 'https://localhost:4200' with no path/query/fragment.");
    }

    if (normalized.Count == 0)
    {
        throw new InvalidOperationException(
            $"Missing CORS origins in '{configKey}'. Add at least one allowed origin (example: 'http://localhost:4200').");
    }

    return normalized.ToArray();
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var localFrontendOrigins = GetValidatedCorsOrigins(builder.Configuration, "Cors:LocalFrontend:Origins");

// Allow configured frontends to call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
        policy.WithOrigins(localFrontendOrigins)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// In-memory persistence (no database)
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();

// Semantic Kernel setup with Gemini
var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
    ?? throw new InvalidOperationException("Missing Gemini:ApiKey in configuration.");
var geminiModel = builder.Configuration["Gemini:Model"]
    ?? throw new InvalidOperationException("Missing Gemini:Model in configuration.");

builder.Services.AddScoped<UserManagementPlugin>();
builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddGoogleAIGeminiChatCompletion(
        modelId: geminiModel,
        apiKey: geminiApiKey);

    var plugin = sp.GetRequiredService<UserManagementPlugin>();
    kernelBuilder.Plugins.AddFromObject(plugin, "UserManagement");

    return kernelBuilder.Build();
});
builder.Services.AddScoped(sp => sp.GetRequiredService<Kernel>()
    .GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>());
builder.Services.AddScoped<IAIService, AIService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

