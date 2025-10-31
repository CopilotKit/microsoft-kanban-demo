using AGUI;
using ProverbsAgent.Models;
using System.ComponentModel;
using System.Text.Json;

namespace ProverbsAgent.Services;

/// <summary>
/// Service containing all Kanban board management tools
/// </summary>
public class KanbanService
{
    private readonly RunAgentInput _input;

    public KanbanService(RunAgentInput input)
    {
        _input = input;
    }

    // =================
    // Board Management Tools
    // =================

    [Description("Create a new board with the specified name. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string CreateBoard([Description("Name for the new board")] string name)
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

        // Store modified state back to _input.State
        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"📋 Created board: {name} (ID: {boardId}) - Agent should call get_state then update_state");
        return $"Created board '{name}' with ID {boardId}. Current board count: {state.Boards.Count}";
    }

    [Description("Delete a board by its ID. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string DeleteBoard([Description("ID of the board to delete")] string boardId)
    {
        var state = DeserializeCurrentState();

        var boardToDelete = state.Boards.FirstOrDefault(b => b.Id == boardId);
        if (boardToDelete == null)
        {
            state.LastAction = $"Board '{boardId}' not found";
            _input.State = JsonSerializer.SerializeToElement(state);
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
        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"🗑️ Deleted board: {boardName} - Agent should call get_state then update_state");
        return $"Deleted board '{boardName}'. Remaining boards: {state.Boards.Count}";
    }

    [Description("Rename a board. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string RenameBoard(
        [Description("ID of the board to rename")] string boardId,
        [Description("New name for the board")] string name)
    {
        var state = DeserializeCurrentState();

        var board = state.Boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null)
        {
            state.LastAction = $"Board '{boardId}' not found";
            _input.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"✏️ Board not found: {boardId}");
            return $"Board '{boardId}' not found";
        }

        var oldName = board.Name;
        board.Name = name;

        state.LastAction = $"Renamed board from '{oldName}' to '{name}'";
        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"✏️ Renamed board from '{oldName}' to '{name}' - Agent should call get_state then update_state");
        return $"Renamed board from '{oldName}' to '{name}'";
    }

    // =================
    // Task Management Tools
    // =================

    [Description("Create a new task on the active board. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string CreateTask(
        [Description("Title of the task")] string title,
        [Description("Optional subtitle for the task")] string? subtitle = null,
        [Description("Optional description for the task")] string? description = null)
    {
        var state = DeserializeCurrentState();
        var taskId = GenerateId();

        if (string.IsNullOrEmpty(state.ActiveBoardId))
        {
            state.LastAction = "No active board. Create a board first.";
            _input.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"➕ Cannot create task - no active board");
            return "No active board. Create a board first.";
        }

        var activeBoard = state.Boards.FirstOrDefault(b => b.Id == state.ActiveBoardId);
        if (activeBoard == null)
        {
            state.LastAction = "Active board not found";
            _input.State = JsonSerializer.SerializeToElement(state);
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
        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"➕ Created task: {title} (ID: {taskId}) - Agent should call get_state then update_state");
        return $"Created task '{title}' with ID {taskId} on board '{activeBoard.Name}'";
    }

    [Description("Update a field on a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string UpdateTaskField(
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
            _input.State = JsonSerializer.SerializeToElement(state);
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
                _input.State = JsonSerializer.SerializeToElement(state);
                Console.WriteLine($"📝 Unknown field: {field}");
                return $"Unknown field '{field}'. Valid fields: title, subtitle, description, status";
        }

        state.LastAction = $"Updated task {field} to '{value}'";
        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"📝 Updated task {taskId}: {field} = {value} - Agent should call get_state then update_state");
        return $"Updated task {field} to '{value}'";
    }

    [Description("Add a tag to a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string AddTaskTag(
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
            _input.State = JsonSerializer.SerializeToElement(state);
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

        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"🏷️ {message} - Agent should call get_state then update_state");
        return message;
    }

    [Description("Remove a tag from a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string RemoveTaskTag(
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
            _input.State = JsonSerializer.SerializeToElement(state);
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

        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"🏷️ {message} - Agent should call get_state then update_state");
        return message;
    }

    [Description("Move a task to a different status. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string MoveTaskToStatus(
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
            _input.State = JsonSerializer.SerializeToElement(state);
            Console.WriteLine($"➡️ Task not found: {taskId}");
            return $"Task '{taskId}' not found";
        }

        _input.State = JsonSerializer.SerializeToElement(state);

        Console.WriteLine($"➡️ Moved task '{task.Title}' to '{status}' - Agent should call get_state then update_state");
        return $"Moved task '{task.Title}' to '{status}'";
    }

    [Description("Delete a task. CRITICAL: Agent must call get_state then update_state after this to sync frontend.")]
    public string DeleteTask([Description("ID of the task to delete")] string taskId)
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

                _input.State = JsonSerializer.SerializeToElement(state);

                Console.WriteLine($"❌ Deleted task: {taskTitle} - Agent should call get_state then update_state");
                return $"Deleted task '{taskTitle}'";
            }
        }

        state.LastAction = $"Task '{taskId}' not found";
        _input.State = JsonSerializer.SerializeToElement(state);
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
        if (_input?.State == null || !_input.State.HasValue)
            return new AgentState();

        try
        {
            return JsonSerializer.Deserialize<AgentState>(
                _input.State.Value.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new AgentState();
        }
        catch
        {
            return new AgentState();
        }
    }
}
