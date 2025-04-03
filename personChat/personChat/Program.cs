using System.Diagnostics;
using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Chat API",
        Version = "v1",
        Description = "An API for interacting with Azure OpenAI models using custom prompt files",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton(sp =>
{
    var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ??
                   "https://faggruppeaihub9416157283.openai.azure.com/";

    var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
    if (string.IsNullOrEmpty(key))
    {
        throw new InvalidOperationException("Please set the AZURE_OPENAI_KEY environment variable.");
    }

    AzureKeyCredential credential = new AzureKeyCredential(key);
    return new AzureOpenAIClient(new Uri(endpoint), credential);
});

// Build the app
var app = builder.Build();

// Always enable Swagger for documentation
app.UseSwagger(options => 
{
    options.RouteTemplate = "api-docs/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api-docs/v1/swagger.json", "AI Chat API v1");
    options.RoutePrefix = "api-docs";
    options.DocumentTitle = "AI Chat API Documentation";
});

app.UseHttpsRedirection();

var mdFiles = Directory.EnumerateFiles(GetCurrentFolder())
    .Where(p => p.EndsWith(".md"))
    .OrderBy(p => p, StringComparer.Ordinal)
    .ToList();

// Configure POST endpoint for AI interaction
app.MapPost("/api/chat", async (AiRequest request, AzureOpenAIClient azureClient) =>
{
    if (string.IsNullOrEmpty(request.UserMessage))
    {
        return Results.BadRequest("User message is required");
    }
    
    // If FileIndex not provided or invalid, select random file
    int fileIndex = 0;
    if (request.FileIndex.HasValue && request.FileIndex.Value >= 0 && request.FileIndex.Value < mdFiles.Count)
    {
        fileIndex = request.FileIndex.Value;
    }
    else
    {
        var rand = new Random();
        fileIndex = rand.Next(0, mdFiles.Count);
    }
    
    try
    {
        // Get file content
        string fileContent = File.ReadAllText(mdFiles[fileIndex]);
        
        // Initialize the ChatClient with the specified deployment name
        ChatClient chatClient = azureClient.GetChatClient("gpt-4o-mini-roger");
        
        // Create chat completion options
        var options = new ChatCompletionOptions
        {
            Temperature = 0.7f,
            MaxOutputTokenCount = 800,
            TopP = 0.95f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };
        
        // Create messages
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(fileContent),
            new SystemChatMessage(request.UserMessage),
        };
        
        // Get completion
        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);
        
        // Return the AI response
        return Results.Ok(new AiResponse
        {
            Message = completion != null
                ? string.Join("\n", completion.Content.Select(s => s.Text))
                : "No response received.",
            FileIndex = fileIndex,
            FileName = Path.GetFileName(mdFiles[fileIndex])
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("ChatWithAi");

// Optional endpoint to get available files
app.MapGet("/api/files", () =>
{
    return Results.Ok(mdFiles.Select((file, index) => new
    {
        Index = index,
        FileName = Path.GetFileName(file)
    }));
})
.WithName("GetAvailableFiles");

// Run the app
app.Run();

string GetCurrentFolder()
{
    var stackTrace = new StackTrace(true);
    var frame = stackTrace.GetFrame(0); // 0 is the current method
    return Path.Combine(frame!.GetFileName()!, "..");
}
