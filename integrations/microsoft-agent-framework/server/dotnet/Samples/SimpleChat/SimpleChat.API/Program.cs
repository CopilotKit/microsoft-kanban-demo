using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using AGUI;
using Microsoft.AspNetCore.HttpLogging;
using SimpleChat.API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.RequestBody 
        | HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.ResponseBody;
    logging.RequestBodyLogLimit = int.MaxValue;
    logging.ResponseBodyLogLimit = int.MaxValue;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AGUIJsonSerializerContext.Default);
});

// Register AgentFactory
builder.Services.AddSingleton<ChatClientAgentFactory>();
builder.Services.AddSingleton<AzureAIServicesAgentFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpLogging();

// Map the AG-UI agent endpoint using the factory
var chatClientAgentFactory = app.Services.GetRequiredService<ChatClientAgentFactory>();
var azureAiServicesAgentFactory = app.Services.GetRequiredService<AzureAIServicesAgentFactory>();

await azureAiServicesAgentFactory.CleanAgentsAndThreads();

app.MapAGUIAgent("/chat-client/agentic_chat", chatClientAgentFactory.CreateAgenticChat());

app.MapAGUIAgent("/chat-client/tool_call", (agentInput, context) =>
{
    return chatClientAgentFactory.CreateBackendToolRendering();
});

app.MapAGUIAgent("/chat-client/human_in_the_loop", (agentInput, context) =>
{
    return chatClientAgentFactory.CreateHumanInTheLoop();
});

app.MapAGUIAgent("/chat-client/agentic_generative_ui", (agentInput, context) =>
{
    return chatClientAgentFactory.CreateAgenticUI();
});

app.MapAGUIAgent("/chat-client/tool_based_generative_ui", (agentInput, context) =>
{
    return chatClientAgentFactory.CreateToolBasedGenerativeUI();
});

app.MapAGUIAgent("/chat-client/shared_state", (agentInput, context) =>
{
    return chatClientAgentFactory.CreateSharedState();
});

app.MapAGUIAgent("/azure-ai-agents/agentic_chat", await azureAiServicesAgentFactory.CreateAgenticChat());

app.MapAGUIAgent("/azure-ai-agents/tool_call", (agentInput, context) =>
{
    return azureAiServicesAgentFactory.CreateBackendToolRendering();
});

app.MapAGUIAgent("/azure-ai-agents/human_in_the_loop", (agentInput, context) =>
{
    return azureAiServicesAgentFactory.CreateHumanInTheLoop();
});

app.MapAGUIAgent("/azure-ai-agents/tool_based_generative_ui", (agentInput, context) =>
{
    return azureAiServicesAgentFactory.CreateToolBasedGenerativeUI();
});

app.Run();

public partial class Program { }
