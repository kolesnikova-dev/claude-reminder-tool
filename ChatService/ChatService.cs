using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeReminder;

internal static class ChatService
{
    internal static async Task Chat(AnthropicClient client, ChatConfig config)
    {

        List<MessageParam> messages = new();
        while (true)
        {
            Console.Write("you> ");
            string? userMessage = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userMessage)) break;

            AddUserMessage(messages, userMessage);

            while (true) // inner loop
            {
                // The SDK serializes the typed request (messages, tools, system) to JSON
                // and deserializes the response, so no manual JSON handling is needed here.
                var response = await client.Messages.Create(BuildParameters(config, messages));

                if (response.StopReason == StopReason.ToolUse)
                {
                    HandleToolUse(messages, response);
                    continue; // adds assistant turn + tool_result turn
                }
                if (response.StopReason == StopReason.MaxTokens)
                {
                    Console.WriteLine($"claude> Oops! Out of tokens.");
                    break;
                }

                string assistantMessage;
                if (config.IsChatStream == true)
                {
                    assistantMessage = ChatStream(response);
                }
                else
                {
                    assistantMessage = TextFromMessage(response);
                }
                Console.WriteLine($"claude> {assistantMessage}");
                AddAssistantMessage(messages, assistantMessage);
                break; // back to asking the user
            }
        }
    }

    private static void HandleToolUse(List<MessageParam> messages, Message response)
    {
        // Collect any tool_use blocks in this response.
        var toolUses = new List<ToolUseBlock>();
        foreach (var block in response.Content)
            if (block.TryPickToolUse(out var tu))
                toolUses.Add(tu);
        // 1. Echo Claude's assistant turn, including every tool_use block.
        var assistantBlocks = new List<ContentBlockParam>();
        foreach (var block in response.Content)
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
        AddAssistantMessage(messages, assistantBlocks);

        // 2. Execute each tool, collect results.
        var resultBlocks = new List<ContentBlockParam>();
        foreach (var tu in toolUses)
        {
            var (content, isError) = ExecuteTool(tu.Name, tu.Input);
            // content is already a string, which is what tool_result expects, and the SDK
            // serializes this block to JSON. (A tool returning an object would need
            // JsonSerializer.Serialize first, the equivalent of Python's json.dumps.)
            resultBlocks.Add(new ToolResultBlockParam
            {
                ToolUseID = tu.ID,
                Content = content,
                IsError = isError,
            });
        }
        // 3. Add the tool results as a user turn, then loop to ask Claude again.
        AddUserMessage(messages, resultBlocks);
    }

    // input arrives already deserialized by the SDK (no json.loads equivalent needed);
    // the GetString/GetDouble helpers just read typed values out of it.
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
                    return (Tools.GetCurrentDatetime(GetString(input, "date_format")), false);

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

    internal static string ChatStream(Message message)
    {
        var reply = new StringBuilder();
        foreach (var block in message.Content)
            if (block.TryPickText(out var tb))
                reply.Append(tb.Text);

        return reply.ToString();
    }

    private static MessageCreateParams BuildParameters(ChatConfig config, List<MessageParam> messages)
    {
        return new MessageCreateParams
        {
            MaxTokens = config.MaxTokens,
            Messages = messages,
            Model = config.Model,
            System = config.System,
            StopSequences = config.StopSequences,
            Tools = ToolSchemas.All,
        };
    }
}