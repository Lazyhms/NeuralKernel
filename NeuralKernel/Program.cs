using NeuralKernel;
using NeuralKernel.Apis;
using NeuralKernel.Core.DataFormats;
using NeuralKernel.Core.Pipeline;
using NeuralKernel.Plugins;
using NeuralKernel.Plugins.Core;
using NeuralKernel.Plugins.Document;
using NeuralKernel.Plugins.SpeechToText;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using OllamaSharp;
using Qdrant.Client;
using Serilog;
using ModelContextProtocol.Client;

Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Information()
            .CreateBootstrapLogger();

int MaxSize = 100 * 1024 * 1024;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger, true);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = MaxSize;
    });

    builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    {
#if DEBUG
        policy.AllowAnyOrigin();
#else
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? []);
#endif
        policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(NeuralKernel.Apis.Chat.X_CHAT_SESSION_ID).WithExposedHeaders(RagChat.X_CHAT_SESSION_ID);
    }));

    builder.Services.AddHttpClient();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.WriteIndented = true;
    });

    builder.Services.AddSwaggerGen();
    builder.Services.AddExceptionHandler(handle =>
    {
        handle.ExceptionHandler = async (context) =>
        {
            var feature = context.Features.GetRequiredFeature<IExceptionHandlerFeature>();

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>();
            logger.CreateLogger("NeuralKernel").LogError(feature.Error, "NeuralKernel");

            await context.Response.WriteAsJsonAsync(new { Message = feature.Error });
            await ValueTask.CompletedTask;
        };
    });
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "192.168.40.138:6379,defaultDatabase=1,abortConnect=false,connectTimeout=5000,ssl=false";
    });

    var kernelBuilder = builder.Services.AddKernel()
        .AddCorePlugin()
        .AddDocumentPlugin();

    foreach (var item in Directory.EnumerateDirectories("Skills"))
    {
        kernelBuilder.Plugins.AddFromPromptDirectory(item);
    }

    kernelBuilder.Plugins.AddFromPluginDirectory();

    builder.Services.Configure<ModelOptions>(builder.Configuration.GetSection("Ollama"));

    builder.Services.AddSingleton<IOllamaApiClient>(new OllamaApiClient(new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(5),
        BaseAddress = new Uri(builder.Configuration.GetConnectionString("Ollama")!),
    }));
    builder.Services.AddSingleton(new QdrantClient(new Uri(builder.Configuration.GetConnectionString("Qdrant")!)));

    builder.Services.AddDefaultWebScraper();
    builder.Services.AddDefaultContentDecoders();
    builder.Services.AddDefaultMimeTypeDetection();

    builder.Services.AddOllamaChatCompletion();
    builder.Services.AddOllamaEmbeddingGenerator();

    builder.Services.AddMemoryCache();
    builder.Services.AddQdrantVectorStore();

    var app = builder.Build();

    app.UseExceptionHandler();

    app.UseCors();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGroup("api/v0")
        .MapChat();

    app.MapGroup("api/v0/rag")
        .MapRagChat();
    //.MapRagIngest();

    app.MapGroup("api/v0/Speech")
        .MapSpeechToText()
        .WithMetadata(new RequestSizeLimitAttribute(MaxSize))
        .WithFormOptions(multipartBodyLengthLimit: MaxSize, valueLengthLimit: int.MaxValue);

    app.MapGroup("api/v0/document")
        .MapDocumentAgent();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "");
    await Log.CloseAndFlushAsync();
}