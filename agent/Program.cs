using AGUI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using ProverbsAgent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AGUIJsonSerializerContext.Default);
});

// Register the agent factory
builder.Services.AddSingleton<CanvasAgentFactory>();

var app = builder.Build();

// Map the AG-UI agent endpoint
var agentFactory = app.Services.GetRequiredService<CanvasAgentFactory>();
app.MapAGUIAgent("/", agentFactory.CreateCanvasAgent);

app.Run();

// =================
// Agent Factory
// =================
public class CanvasAgentFactory
{
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient _openAiClient;

    public CanvasAgentFactory(IConfiguration configuration)
    {
        _configuration = configuration;

        var githubToken = _configuration["GitHubToken"]
            ?? throw new InvalidOperationException("GitHubToken not found in configuration. Run: dotnet user-secrets set GitHubToken \"YOUR_TOKEN\"");

        _openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(githubToken),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            });
    }

    public AGUIAgent CreateCanvasAgent(RunAgentInput agentInput, HttpContext context)
    {
        // Create Kanban service with current request input
        var kanbanService = new KanbanService(agentInput);

        var chatClient = _openAiClient.GetChatClient("gpt-4o-mini").AsIChatClient();

        var chatClientAgent = new ChatClientAgent(
            chatClient,
            name: "my_agent",
            description: @"A helpful assistant managing Kanban boards and tasks.

CRITICAL STATE SYNCHRONIZATION RULES:
1. Before answering questions about state, ALWAYS call get_state first to read current state
2. After ANY tool that modifies state (create/delete/rename board or task), you MUST:
   a) Call get_state to retrieve the updated state
   b) Call update_state with the complete state object to sync frontend
   c) Only then respond to the user

Example workflow:
User: 'Create a new board called Project Alpha'
1. Call create_board(name: 'Project Alpha')
2. Call get_state (returns: {boards: [...], activeBoardId: '...', lastAction: '...'})
3. Call update_state(state: {boards: [...], activeBoardId: '...', lastAction: '...'})
4. Respond: 'Created board Project Alpha!'

Each task has title, subtitle, description, tags[], and status.
Tasks flow through 4 statuses: new → in_progress → review → completed.
ALWAYS prefer shared state over chat history.",
            tools: [
                AIFunctionFactory.Create(kanbanService.CreateBoard, new AIFunctionFactoryOptions { Name = "create_board" }),
                AIFunctionFactory.Create(kanbanService.DeleteBoard, new AIFunctionFactoryOptions { Name = "delete_board" }),
                AIFunctionFactory.Create(kanbanService.RenameBoard, new AIFunctionFactoryOptions { Name = "rename_board" }),
                AIFunctionFactory.Create(kanbanService.CreateTask, new AIFunctionFactoryOptions { Name = "create_task" }),
                AIFunctionFactory.Create(kanbanService.UpdateTaskField, new AIFunctionFactoryOptions { Name = "update_task_field" }),
                AIFunctionFactory.Create(kanbanService.AddTaskTag, new AIFunctionFactoryOptions { Name = "add_task_tag" }),
                AIFunctionFactory.Create(kanbanService.RemoveTaskTag, new AIFunctionFactoryOptions { Name = "remove_task_tag" }),
                AIFunctionFactory.Create(kanbanService.MoveTaskToStatus, new AIFunctionFactoryOptions { Name = "move_task_to_status" }),
                AIFunctionFactory.Create(kanbanService.DeleteTask, new AIFunctionFactoryOptions { Name = "delete_task" })
            ]);

        return new ChatClientAGUIAgent(chatClientAgent);
    }
}

public partial class Program { }
