namespace AGUI.Events;

public static class EventType
{
    public const string RUN_STARTED = "RUN_STARTED";
    public const string RUN_FINISHED = "RUN_FINISHED";
    public const string RUN_ERROR = "RUN_ERROR";
    public const string TEXT_MESSAGE_START = "TEXT_MESSAGE_START";
    public const string TEXT_MESSAGE_CONTENT = "TEXT_MESSAGE_CONTENT";
    public const string TEXT_MESSAGE_END = "TEXT_MESSAGE_END";
    public const string STEP_STARTED = "STEP_STARTED";
    public const string STEP_FINISHED = "STEP_FINISHED";
    public const string TOOL_CALL_START = "TOOL_CALL_START";
    public const string TOOL_CALL_ARGS = "TOOL_CALL_ARGS";
    public const string TOOL_CALL_END = "TOOL_CALL_END";
    public const string TOOL_CALL_RESULT = "TOOL_CALL_RESULT";
    public const string STATE_SNAPSHOT = "STATE_SNAPSHOT";
}
