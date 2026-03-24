using ChatBot_BE.Services;
using ChatBot_BE.Data;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;
using ChatBot_BE.Shared;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.SemanticKernel", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

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
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(60);
            npgsqlOptions.EnableRetryOnFailure(3);
            // Explicitly set assembly to ensure migrations are found on Render
            npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
        }));

// Configure rate limiting
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Global limiter for all endpoints
    rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Stricter limiter for AI endpoints
    rateLimiterOptions.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiterOptions.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.ContentType = "application/json";
        var response = new { error = "Too many requests. Please try again later." };
        await context.HttpContext.Response.WriteAsJsonAsync(response, token);
    };
});

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

// EF persistence
builder.Services.AddScoped<IUserStore, EfUserStore>();
builder.Services.AddScoped<IPolicyStore, EfPolicyStore>(); // New store
builder.Services.AddScoped<IChatSessionStore, DbChatSessionStore>();

// Scoped conversation context for plugin access
builder.Services.AddScoped<IConversationContext, ConversationContext>();

// Input validation services
builder.Services.AddSingleton<IProfanityFilter, ProfanityFilter>();
builder.Services.AddSingleton<ISpellChecker, SpellChecker>();
builder.Services.AddScoped<IInputValidator, InputValidator>();

builder.Services.AddScoped<IPolicyService, PolicyService>();

// Knowledge Base (RAG) setup
builder.Services.AddSingleton<IKnowledgeBaseService, InMemoryKnowledgeBaseService>();
builder.Services.AddScoped<KnowledgeBasePlugin>();

// Semantic Kernel setup with OpenAI (Kimi K2 via NVIDIA)
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("Missing OpenAI:ApiKey in configuration.");
var openAiModelId = builder.Configuration["OpenAI:ModelId"]
    ?? throw new InvalidOperationException("Missing OpenAI:ModelId in configuration.");
var openAiEndpoint = builder.Configuration["OpenAI:Endpoint"]
    ?? throw new InvalidOperationException("Missing OpenAI:Endpoint in configuration.");

builder.Services.AddScoped<UserManagementPlugin>();
builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    var httpClient = new HttpClient { BaseAddress = new Uri(openAiEndpoint) };
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: openAiModelId,
        apiKey: openAiApiKey,
        httpClient: httpClient);

    var userPlugin = sp.GetRequiredService<UserManagementPlugin>();
    kernelBuilder.Plugins.AddFromObject(userPlugin, "UserManagement");

    var knowledgePlugin = sp.GetRequiredService<KnowledgeBasePlugin>();
    kernelBuilder.Plugins.AddFromObject(knowledgePlugin, "KnowledgeBase");

    return kernelBuilder.Build();
});
builder.Services.AddScoped(sp => sp.GetRequiredService<Kernel>()
    .GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>());
builder.Services.AddScoped<IAIService, AIService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("LocalFrontend");

app.UseMiddleware<ExceptionMiddleware>();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// Initialize and seed the database
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
    var policyStore = scope.ServiceProvider.GetRequiredService<IPolicyStore>();
    var knowledgeBase = scope.ServiceProvider.GetRequiredService<IKnowledgeBaseService>();

    await DatabaseInitializer.InitializeAsync(context, userStore, policyStore, knowledgeBase, app.Environment);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "FATAL: Database initialization or seeding failed. the application will not start.");
    // Force exit so Render knows the deploy failed
    Environment.Exit(1);
}

app.Run();

// Make Program visible for testing
public partial class Program { }
