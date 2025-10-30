using System.Text.Json;

namespace AGUI;

/// <summary>
/// Provides validation utilities for tool calls
/// </summary>
public static class ToolCallValidator
{
    /// <summary>
    /// Validates that a tool call ID is unique within the context
    /// </summary>
    /// <param name="toolCallId">The tool call ID to validate</param>
    /// <param name="existingToolCallIds">Collection of existing tool call IDs</param>
    /// <returns>True if unique, false otherwise</returns>
    public static bool IsToolCallIdUnique(string toolCallId, IEnumerable<string> existingToolCallIds)
    {
        return !existingToolCallIds.Contains(toolCallId);
    }

    /// <summary>
    /// Validates that a tool name exists in the available tools
    /// </summary>
    /// <param name="toolName">Name of the tool to validate</param>
    /// <param name="availableTools">Array of available tools</param>
    /// <returns>True if tool exists, false otherwise</returns>
    public static bool IsToolNameValid(string toolName, AGUITool[] availableTools)
    {
        return availableTools.Any(t => t.Name == toolName);
    }

    /// <summary>
    /// Gets the tool definition for a given tool name
    /// </summary>
    /// <param name="toolName">Name of the tool</param>
    /// <param name="availableTools">Array of available tools</param>
    /// <returns>Tool definition if found, null otherwise</returns>
    public static AGUITool? GetToolByName(string toolName, AGUITool[] availableTools)
    {
        return availableTools.FirstOrDefault(t => t.Name == toolName);
    }

    /// <summary>
    /// Validates tool call arguments against the tool's JSON Schema (basic validation)
    /// </summary>
    /// <param name="arguments">JSON string of arguments</param>
    /// <param name="tool">Tool definition with parameter schema</param>
    /// <returns>True if arguments are valid JSON, false otherwise</returns>
    /// <remarks>
    /// This performs basic JSON validation. For full JSON Schema validation,
    /// consider using a library like Json.Schema.Net
    /// </remarks>
    public static bool ValidateToolArguments(string arguments, AGUITool tool)
    {
        try
        {
            // Basic check: ensure it's valid JSON
            using var doc = JsonDocument.Parse(arguments);
            
            // Additional validation could check against tool.Parameters schema
            // This would require a JSON Schema validation library
            
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a unique tool call ID
    /// </summary>
    /// <returns>A unique identifier for a tool call</returns>
    public static string GenerateToolCallId()
    {
        return $"call_{Guid.NewGuid():N}";
    }
}
