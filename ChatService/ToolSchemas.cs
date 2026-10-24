using Anthropic.Models.Messages;
using System.Text.Json;

namespace ClaudeReminder;

internal static class ToolSchemas
{
    internal static List<ToolUnion> All => new()
    {
        new Tool
        {
            Name = "add_duration_to_datetime",
            Description = "Adds a duration to a datetime and returns the resulting datetime formatted as 'Thursday, April 03, 2025 10:30:00 AM'. Supports seconds, minutes, hours, days, weeks, months, and years. Month and year arithmetic handles variable month lengths and leap years. Negative durations move into the past. Defaults: duration=0, unit='days'.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["datetime_str"] = JsonSerializer.SerializeToElement(
                        new { type = "string", description = "The datetime to add to, in ISO 8601. Accepted shapes: 'YYYY-MM-DD' (e.g. '2025-04-03'), 'YYYY-MM-DDTHH:MM:SS' (e.g. '2025-04-03T10:30:00'), or 'YYYY-MM-DD HH:MM:SS'. If no timezone is given, UTC is assumed." }),
                    ["duration"] = JsonSerializer.SerializeToElement(
                        new { type = "number", description = "Amount to add. Positive for future, negative for past. Fractional values are allowed for seconds/minutes/hours/days/weeks; months and years require whole numbers. Defaults to 0.", @default = 0 }),
                    ["unit"] = JsonSerializer.SerializeToElement(
                        new { type = "string", description = "Time unit of the duration. Defaults to 'days'.", @enum = new[] { "seconds", "minutes", "hours", "days", "weeks", "months", "years" }, @default = "days" })
                },
                Required = ["datetime_str"]
            }
        },
        new Tool
        {
            Name = "set_reminder",
            Description = "Creates a timed reminder that notifies the user at the given timestamp with the given content. Use when the user wants to be reminded about something at a specific future time. The reminder is persisted and triggered through the user's notification channels. The system handles timezone internally.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["content"] = JsonSerializer.SerializeToElement(
                        new { type = "string", description = "Message shown in the notification, e.g. 'Take medication' or 'Join video call with team'." }),
                    ["timestamp"] = JsonSerializer.SerializeToElement(
                        new { type = "string", description = "When to trigger the reminder, as ISO 8601 (YYYY-MM-DDTHH:MM:SS) or a Unix timestamp. Timezone is handled internally." })
                },
                Required = ["content", "timestamp"]
            }
        },
        new Tool
        {
            Name = "batch_tool",
            Description = "Invoke multiple other tool calls in a single step. Use when several independent tool calls are needed at once. Each invocation names a tool and provides its arguments as a JSON-encoded string.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["invocations"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "array",
                            description = "The tool calls to invoke.",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string", description = "Name of the tool to invoke." },
                                    arguments = new { type = "string", description = "Arguments for that tool, encoded as a JSON string. Must conform to that tool's input_schema." }
                                },
                                required = new[] { "name", "arguments" }
                            }
                        })
                },
                Required = ["invocations"]
            }
    },
      new Tool
        {
            Name = "get_current_time",
            Description = "Returns the current time in ISO 8601 format (YYYY-MM-DDTHH:MM:SS) in UTC. Use when the user asks for the current time or when a tool needs to know the current time.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>(),
                Required = Array.Empty<string>()
            }
        }
};
}