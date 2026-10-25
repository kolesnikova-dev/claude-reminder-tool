using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeReminder;

internal sealed record ToolResult
{
    public const string BlockType = "tool_result";

    [JsonPropertyName("type")]
    public string Type => BlockType;

    [JsonPropertyName("tool_use_id")]
    public required string ToolUseId { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("is_error")]
    public bool IsError { get; init; }

    public static ToolResult Success(string toolUseId, string content) =>
        new() { ToolUseId = toolUseId, Content = content };

    public static ToolResult Failure(string toolUseId, string error) =>
        new() { ToolUseId = toolUseId, Content = error, IsError = true };
}

internal class ChatService
{
    internal static async Task Chat(AnthropicClient client, ChatConfig config)
    {
        var (messages, stopSequences, system) = config;

        while (true)
        {
            Console.Write("you> ");
            string? userMessage = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userMessage)) break;

            AddUserMessage(messages, userMessage);

            // Inner loop: keep talking to Claude until it stops asking for tools.
            while (true)
            {
                var parameters = new MessageCreateParams
                {
                    MaxTokens = 1024,
                    Messages = messages,
                    Model = Model.ClaudeHaiku4_5,
                    System = system,
                    StopSequences = stopSequences,
                    Tools = ToolSchemas.All,
                };

                var message = await client.Messages.Create(parameters);

                // Collect any tool_use blocks in this response.
                var toolUses = new List<ToolUseBlock>();
                foreach (var block in message.Content)
                    if (block.TryPickToolUse(out var tu))
                        toolUses.Add(tu);

                if (toolUses.Count == 0)
                {
                    // No tools requested — print the text and wait for the next user turn.
                    var reply = TextFromMessage(message);
                    Console.WriteLine($"claude> {reply}");
                    AddAssistantMessage(messages, reply);
                    break; // back to outer loop: read next user input
                }

                // 1. Echo Claude's assistant turn, including every tool_use block.
                var assistantBlocks = new List<ContentBlockParam>();
                foreach (var block in message.Content)
                {
                    if (block.TryPickText(out var tb))
                        assistantBlocks.Add(new TextBlockParam { Text = tb.Text });
                    else if (block.TryPickToolUse(out var tu))
                        assistantBlocks.Add(new ToolUseBlockParam
                        {
                            ID = tu.ID,
                            Name = tu.Name,
                            Input = tu.Input,
                        });
                }
                messages.Add(new MessageParam { Role = Role.Assistant, Content = assistantBlocks });

                // 2. Execute each tool, collect results.
                var resultBlocks = new List<ContentBlockParam>();
                foreach (var tu in toolUses)
                {
                    var (content, isError) = ExecuteTool(tu.Name, tu.Input);
                    resultBlocks.Add(new ToolResultBlockParam
                    {
                        ToolUseID = tu.ID,
                        Content = content,
                        IsError = isError,
                    });
                }

                // 3. Add the tool results as a user turn, then loop to ask Claude again.
                messages.Add(new MessageParam { Role = Role.User, Content = resultBlocks });
            }
        }
    }

    private static (string content, bool isError) ExecuteTool(
        string toolName, IReadOnlyDictionary<string, JsonElement> input)
    {
        try
        {
            switch (toolName)
            {
                case ToolNames.SetReminder:
                    Tools.SetReminder(
                        GetString(input, "content") ?? "",
                        GetString(input, "timestamp") ?? "");
                    return ("Reminder set.", false);

                case ToolNames.AddDurationToDatetime:
                    var result = Tools.AddDurationToDatetime(
                        GetString(input, "datetime_str"),
                        GetDouble(input, "duration"),
                        GetString(input, "unit") ?? "days");
                    return result.Success
                        ? (Tools.FormatForDisplay(result), false)
                        : (result.Error!, true);

                case ToolNames.GetCurrentDatetime:
                    return (Tools.GetCurrentDatetime(
                        GetString(input, "date_format") ?? Tools.OutputFormat), false);

                default:
                    return ($"Error: Unknown tool '{toolName}'.", true);
            }
        }
        catch (Exception ex)
        {
            return ($"Error: {ex.Message}", true);
        }
    }

    private static string? GetString(IReadOnlyDictionary<string, JsonElement> input, string key) =>
        input.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double GetDouble(IReadOnlyDictionary<string, JsonElement> input, string key, double fallback = 0) =>
        input.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;

    internal static void AddUserMessage(List<MessageParam> messages, MessageParamContent message) =>
        messages.Add(new MessageParam { Role = Role.User, Content = message });

    internal static void AddAssistantMessage(List<MessageParam> messages, MessageParamContent message) =>
        messages.Add(new MessageParam { Role = Role.Assistant, Content = message });

    internal static string TextFromMessage(Message message)
    {
        var reply = new StringBuilder();
        foreach (var block in message.Content)
            if (block.TryPickText(out var tb))
                reply.Append(tb.Text);

        return reply.ToString();
    }
}