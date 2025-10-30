using AGUI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SimpleChat.API;

public class ChatClientAgentFactory
{
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient _openAiClient;

    public ChatClientAgentFactory(IConfiguration configuration)
    {
        _configuration = configuration;

        // Get the GitHub token from configuration (moved from method)
        var githubToken = _configuration["GitHubToken"]
            ?? throw new InvalidOperationException(
                "GitHubToken not found in configuration. " +
                "Please set it using: dotnet user-secrets set GitHubToken \"<your-token>\" " +
                "or get it using: gh auth token");

        _openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(githubToken),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            });
    }

    public AGUIAgent CreateAgenticChat()
    {
        var chatClient = _openAiClient.GetChatClient("gpt-4.1-nano").AsIChatClient();

        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "SimpleChat",
            description: "A simple chat agent using GPT-4o via GitHub Models");

        return new ChatClientAGUIAgent(chatClientAgent);
    }

    public AGUIAgent CreateBackendToolRendering()
    {
        var chatClient = _openAiClient.GetChatClient("gpt-4.1")
            .AsIChatClient();

        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "BackendToolRenderer",
            description: "An agent that can render backend tools using GPT-4o via GitHub Models",
            tools: [AIFunctionFactory.Create(GetWeather, new AIFunctionFactoryOptions {
                Name = "get_weather",
            })]);
        return new ChatClientAGUIAgent(chatClientAgent);

        [Description("Get the weather for a given location.")]
        WeatherInfo GetWeather([Description("The location to get the weather for.")] string location) => new WeatherInfo
        {
            Temperature = 20,
            Conditions = "sunny",
            Humidity = 50,
            WindSpeed = 10,
            FeelsLike = 25
        };
    }

    public AGUIAgent CreateHumanInTheLoop()
    {
        var chatClient = _openAiClient.GetChatClient("gpt-4.1-nano").AsIChatClient();
        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "HumanInTheLoopAgent",
            description: "An agent that involves human feedback in its decision-making process using GPT-4o via GitHub Models");
        return new ChatClientAGUIAgent(chatClientAgent);
    }
    
    public AGUIAgent CreateToolBasedGenerativeUI()
    {
        var chatClient = _openAiClient.GetChatClient("gpt-4.1-nano").AsIChatClient();
        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "ToolBasedGenerativeUIAgent",
            description: "An agent that uses tools to generate user interfaces using GPT-4o via GitHub Models");

        return new ChatClientAGUIAgent(chatClientAgent);
    }

    internal AGUIAgent CreateAgenticUI()
    {
        var chatClient = _openAiClient.GetChatClient("gpt-4.1-nano").AsIChatClient();
        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "ToolBasedGenerativeUIAgent",
            description: "An agent that uses tools to generate user interfaces using GPT-4o via GitHub Models");

        return new ChatClientAGUIAgent(chatClientAgent);
    }
    internal AGUIAgent CreateSharedState()
    {
        var chatClient = _openAiClient.GetChatClient("gpt-4.1-nano").AsIChatClient();
        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "ToolBasedGenerativeUIAgent",
            description: "An agent that uses tools to generate user interfaces using GPT-4o via GitHub Models");

        return new ChatClientAGUIAgent(chatClientAgent);
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
