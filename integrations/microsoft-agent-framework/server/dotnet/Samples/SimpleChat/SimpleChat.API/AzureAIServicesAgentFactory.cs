using AGUI;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleChat.API;

public class AzureAIServicesAgentFactory
{
    private readonly IConfiguration _configuration;
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;

    public AzureAIServicesAgentFactory(IConfiguration configuration)
    {
        _configuration = configuration;

        // Get the Azure AI Project endpoint and model from configuration
        var projectEndpoint = _configuration["AzureAI:ProjectEndpoint"]
            ?? throw new InvalidOperationException(
                "AzureAI:ProjectEndpoint not found in configuration. " +
                "Please set it using: dotnet user-secrets set AzureAI:ProjectEndpoint \"https://<your-project>.services.ai.azure.com/api/projects/<project-id>\"");

        _modelDeploymentName = _configuration["AzureAI:ModelDeploymentName"] ?? "gpt-4.1-mini";

        // Create the PersistentAgentsClient using DefaultAzureCredential
        _agentsClient = new PersistentAgentsClient(projectEndpoint, new DefaultAzureCredential());
    }

    public async Task<AGUIAgent> CreateAgenticChat()
    {
        // Create or get existing Azure AI agent using AIAgent
        var aiAgent = _agentsClient.Administration.CreateAgent(
            model: _modelDeploymentName,
            name: "SimpleChat",
            instructions: "You are a helpful assistant.");

        return new AzureAIAGUIAgent(aiAgent.Value, _agentsClient);
    }

    public AGUIAgent CreateBackendToolRendering()
    {
        // Define the function tool using FunctionToolDefinition for Azure AI Agents
        var getWeatherTool = new FunctionToolDefinition(
            name: "get_weather",
            description: "Get the weather for a given location.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "The location to get the weather for."
                    }
                },
                required = new[] { "location" }
            }));

        // Create agent with the function tool
        var agentMetadata = _agentsClient.Administration.CreateAgent(
            model: _modelDeploymentName,
            name: "BackendToolRenderer",
            instructions: "You are an agent that can render backend tools. Use the get_weather tool to fetch weather information.",
            tools: [getWeatherTool]);

        // Convert to AIAgent
        var aiAgent = _agentsClient.GetAIAgentAsync(agentMetadata.Value.Id).Result;

        return new AzureAIAGUIAgent(agentMetadata.Value, _agentsClient);
    }

    public AGUIAgent CreateHumanInTheLoop()
    {
        var aiAgent = _agentsClient.Administration.CreateAgent(
            model: _modelDeploymentName,
            name: "HumanInTheLoopAgent",
            instructions: "You are an agent that involves human feedback in your decision-making process.");

        return new AzureAIAGUIAgent(aiAgent, _agentsClient);
    }

    public AGUIAgent CreateToolBasedGenerativeUI()
    {
        var aiAgent = _agentsClient.Administration.CreateAgent(
            model: _modelDeploymentName,
            name: "ToolBasedGenerativeUIAgent",
            instructions: "You are an agent that uses tools to generate user interfaces.");

        return new AzureAIAGUIAgent(aiAgent.Value, _agentsClient);
    }

    internal async Task CleanAgentsAndThreads()
    {
        await foreach (var agent in _agentsClient.Administration.GetAgentsAsync())
        {
            var response = await _agentsClient.Administration.DeleteAgentAsync(agent.Id);
        }
        await foreach (var thread in _agentsClient.Threads.GetThreadsAsync())
        {
            var response = await _agentsClient.Threads.DeleteThreadAsync(thread.Id);
        }
    }

    private sealed class WeatherInfo
    {
        [JsonPropertyName("temperature")]
        public int Temperature { get; init; }

        [JsonPropertyName("conditions")]
        public string Conditions { get; init; } = string.Empty;

        [JsonPropertyName("humidity")]
        public int Humidity { get; init; }

        [JsonPropertyName("wind_speed")]
        public int WindSpeed { get; init; }

        [JsonPropertyName("feelsLike")]
        public int FeelsLike { get; init; }
    }
}
