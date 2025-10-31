using AGUI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private RunAgentInput? _currentInput;

    public CanvasAgentFactory(IConfiguration configuration)
    {
        _configuration = configuration;

        // Get the GitHub token from configuration
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

    public AGUIAgent CreateCanvasAgent(RunAgentInput agentInput, HttpContext context)
    {
        _currentInput = agentInput;

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
                AIFunctionFactory.Create(CreateBoard, new AIFunctionFactoryOptions { Name = "create_board" }),
                AIFunctionFactory.Create(DeleteBoard, new AIFunctionFactoryOptions { Name = "delete_board" }),
                AIFunctionFactory.Create(RenameBoard, new AIFunctionFactoryOptions { Name = "rename_board" }),
                AIFunctionFactory.Create(CreateTask, new AIFunctionFactoryOptions { Name = "create_task" }),
                AIFunctionFactory.Create(UpdateTaskField, new AIFunctionFactoryOptions { Name = "update_task_field" }),
                AIFunctionFactory.Create(AddTaskTag, new AIFunctionFactoryOptions { Name = "add_task_tag" }),
                AIFunctionFactory.Create(RemoveTaskTag, new AIFunctionFactoryOptions { Name = "remove_task_tag" }),
                AIFunctionFactory.Create(MoveTaskToStatus, new AIFunctionFactoryOptions { Name = "move_task_to_status" }),
                AIFunctionFactory.Create(DeleteTask, new AIFunctionFactoryOptions { Name = "delete_task" })
            ]);

        return new ChatClientAGUIAgent(chatClientAgent);
    }

    // =================
    // Kanban Tools
    // =================

    [Description("Create a new board with the specified name. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string CreateBoard([Description("Name for the new board")] string name)
    {
        var state = DeserializeCurrentState();
        var boardId = GenerateId();

        var newBoard = new Board
        {
            Id = boardId,
            Name = name,
            Tasks = new List<KanbanTask>()
        };

        state.Boards.Add(newBoard);

        // If this is the first board, set it as active
        if (state.Boards.Count == 1)
        {
            state.ActiveBoardId = boardId;
        }

        state.LastAction = $"Created board '{name}'";

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"📋 Created board: {name} (ID: {boardId}) - Agent should call get_state then update_state");
        return $"Created board '{name}' with ID {boardId}. Current board count: {state.Boards.Count}";
    }

    [Description("Delete a board by its ID. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string DeleteBoard([Description("ID of the board to delete")] string boardId)
    {
        var state = DeserializeCurrentState();

        var boardToDelete = state.Boards.FirstOrDefault(b => b.Id == boardId);
        if (boardToDelete == null)
        {
            state.LastAction = $"Board '{boardId}' not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"🗑️ Board not found: {boardId}");
            return $"Board '{boardId}' not found";
        }

        var boardName = boardToDelete.Name;
        state.Boards.Remove(boardToDelete);

        // If deleting the active board, switch to another board
        if (state.ActiveBoardId == boardId)
        {
            state.ActiveBoardId = state.Boards.FirstOrDefault()?.Id ?? string.Empty;
        }

        state.LastAction = $"Deleted board '{boardName}'";

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"🗑️ Deleted board: {boardName} - Agent should call get_state then update_state");
        return $"Deleted board '{boardName}'. Remaining boards: {state.Boards.Count}";
    }

    [Description("Rename a board. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string RenameBoard(
        [Description("ID of the board to rename")] string boardId,
        [Description("New name for the board")] string name)
    {
        var state = DeserializeCurrentState();

        var board = state.Boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null)
        {
            state.LastAction = $"Board '{boardId}' not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"✏️ Board not found: {boardId}");
            return $"Board '{boardId}' not found";
        }

        var oldName = board.Name;
        board.Name = name;

        state.LastAction = $"Renamed board from '{oldName}' to '{name}'";

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"✏️ Renamed board from '{oldName}' to '{name}' - Agent should call get_state then update_state");
        return $"Renamed board from '{oldName}' to '{name}'";
    }

    [Description("Create a new task on the active board. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string CreateTask(
        [Description("Title of the task")] string title,
        [Description("Optional subtitle for the task")] string? subtitle = null,
        [Description("Optional description for the task")] string? description = null)
    {
        var state = DeserializeCurrentState();
        var taskId = GenerateId();

        if (string.IsNullOrEmpty(state.ActiveBoardId))
        {
            state.LastAction = "No active board. Create a board first.";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"➕ Cannot create task - no active board");
            return "No active board. Create a board first.";
        }

        var activeBoard = state.Boards.FirstOrDefault(b => b.Id == state.ActiveBoardId);
        if (activeBoard == null)
        {
            state.LastAction = "Active board not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"➕ Cannot create task - active board not found");
            return "Active board not found";
        }

        var newTask = new KanbanTask
        {
            Id = taskId,
            Title = title,
            Subtitle = subtitle ?? string.Empty,
            Description = description ?? string.Empty,
            Status = "new",
            Tags = new List<string>()
        };

        activeBoard.Tasks.Add(newTask);

        state.LastAction = $"Created task '{title}' on board '{activeBoard.Name}'";

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"➕ Created task: {title} (ID: {taskId}) - Agent should call get_state then update_state");
        return $"Created task '{title}' with ID {taskId} on board '{activeBoard.Name}'";
    }

    [Description("Update a field on a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string UpdateTaskField(
        [Description("ID of the task to update")] string taskId,
        [Description("Field to update (title, subtitle, description, status)")] string field,
        [Description("New value for the field")] string value)
    {
        var state = DeserializeCurrentState();

        KanbanTask? task = null;
        foreach (var board in state.Boards)
        {
            task = board.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null) break;
        }

        if (task == null)
        {
            state.LastAction = $"Task '{taskId}' not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"📝 Task not found: {taskId}");
            return $"Task '{taskId}' not found";
        }

        switch (field.ToLower())
        {
            case "title":
                task.Title = value;
                break;
            case "subtitle":
                task.Subtitle = value;
                break;
            case "description":
                task.Description = value;
                break;
            case "status":
                task.Status = value;
                break;
            default:
                state.LastAction = $"Unknown field '{field}'";
                _currentInput.State = JsonSerializer.SerializeToElement(state);
                Console.WriteLine($"📝 Unknown field: {field}");
                return $"Unknown field '{field}'. Valid fields: title, subtitle, description, status";
        }

        state.LastAction = $"Updated task {field} to '{value}'";

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"📝 Updated task {taskId}: {field} = {value} - Agent should call get_state then update_state");
        return $"Updated task {field} to '{value}'";
    }

    [Description("Add a tag to a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string AddTaskTag(
        [Description("ID of the task")] string taskId,
        [Description("Tag to add")] string tag)
    {
        var state = DeserializeCurrentState();

        KanbanTask? task = null;
        foreach (var board in state.Boards)
        {
            task = board.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null) break;
        }

        if (task == null)
        {
            state.LastAction = $"Task '{taskId}' not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"🏷️ Task not found: {taskId}");
            return $"Task '{taskId}' not found";
        }

        string message;
        if (!task.Tags.Contains(tag))
        {
            task.Tags.Add(tag);
            state.LastAction = $"Added tag '{tag}' to task '{task.Title}'";
            message = $"Added tag '{tag}' to task '{task.Title}'";
        }
        else
        {
            state.LastAction = $"Tag '{tag}' already exists on task '{task.Title}'";
            message = $"Tag '{tag}' already exists on task '{task.Title}'";
        }

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"🏷️ {message} - Agent should call get_state then update_state");
        return message;
    }

    [Description("Remove a tag from a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string RemoveTaskTag(
        [Description("ID of the task")] string taskId,
        [Description("Tag to remove")] string tag)
    {
        var state = DeserializeCurrentState();

        KanbanTask? task = null;
        foreach (var board in state.Boards)
        {
            task = board.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null) break;
        }

        if (task == null)
        {
            state.LastAction = $"Task '{taskId}' not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"🏷️ Task not found: {taskId}");
            return $"Task '{taskId}' not found";
        }

        string message;
        if (task.Tags.Remove(tag))
        {
            state.LastAction = $"Removed tag '{tag}' from task '{task.Title}'";
            message = $"Removed tag '{tag}' from task '{task.Title}'";
        }
        else
        {
            state.LastAction = $"Tag '{tag}' not found on task '{task.Title}'";
            message = $"Tag '{tag}' not found on task '{task.Title}'";
        }

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"🏷️ {message} - Agent should call get_state then update_state");
        return message;
    }

    [Description("Move a task to a different status. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string MoveTaskToStatus(
        [Description("ID of the task")] string taskId,
        [Description("New status (new, in_progress, review, completed)")] string status)
    {
        var state = DeserializeCurrentState();

        KanbanTask? task = null;
        foreach (var board in state.Boards)
        {
            task = board.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Status = status;
                state.LastAction = $"Moved task '{task.Title}' to '{status}'";
                break;
            }
        }

        if (task == null)
        {
            state.LastAction = $"Task '{taskId}' not found";
            _currentInput.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"➡️ Task not found: {taskId}");
            return $"Task '{taskId}' not found";
        }

        // Store modified state back to _currentInput.State
        _currentInput.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"➡️ Moved task '{task.Title}' to '{status}' - Agent should call get_state then update_state");
        return $"Moved task '{task.Title}' to '{status}'";
    }

    [Description("Delete a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    private string DeleteTask([Description("ID of the task to delete")] string taskId)
    {
        var state = DeserializeCurrentState();

        foreach (var board in state.Boards)
        {
            var task = board.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                var taskTitle = task.Title;
                board.Tasks.Remove(task);
                state.LastAction = $"Deleted task '{taskTitle}'";

                // Store modified state back to _currentInput.State
                _currentInput.State = JsonSerializer.SerializeToElement(state);

                Console.WriteLine($"❌ Deleted task: {taskTitle} - Agent should call get_state then update_state");
                return $"Deleted task '{taskTitle}'";
            }
        }

        state.LastAction = $"Task '{taskId}' not found";
        _currentInput.State = JsonSerializer.SerializeToElement(state);
        Console.WriteLine($"❌ Task not found: {taskId}");
        return $"Task '{taskId}' not found";
    }

    // =================
    // Helper Methods
    // =================

    private string GenerateId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private AgentState DeserializeCurrentState()
    {
        if (_currentInput?.State == null || !_currentInput.State.HasValue)
            return new AgentState();

        try
        {
            return JsonSerializer.Deserialize<AgentState>(
                _currentInput.State.Value.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new AgentState();
        }
        catch
        {
            return new AgentState();
        }
    }

}

// =================
// Kanban State Models
// =================

public class KanbanTask
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "new"; // new | in_progress | review | completed
}

public class Board
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tasks")]
    public List<KanbanTask> Tasks { get; set; } = new();
}

public class AgentState
{
    [JsonPropertyName("boards")]
    public List<Board> Boards { get; set; } = new();

    [JsonPropertyName("activeBoardId")]
    public string ActiveBoardId { get; set; } = string.Empty;

    [JsonPropertyName("lastAction")]
    public string? LastAction { get; set; }
}

public partial class Program { }
