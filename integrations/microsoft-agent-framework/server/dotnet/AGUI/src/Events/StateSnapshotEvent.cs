using System.Text.Json.Serialization;

namespace AGUI.Events;

/// <summary>
/// Provides a complete snapshot of an agent's state.
/// The StateSnapshot event delivers a comprehensive representation of the agent's current state.
/// This event is typically sent at the beginning of an interaction or when synchronization is needed.
/// </summary>
public class StateSnapshotEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.STATE_SNAPSHOT;

    /// <summary>
    /// Complete state snapshot containing all state variables relevant to the frontend
    /// </summary>
    [JsonPropertyName("snapshot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Snapshot { get; set; }
}
