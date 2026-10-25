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

                var reply = new StringBuilder();
                foreach (var block in message.Content)
                {
                    if (block.TryPickText(out var textBlock))
                    {
                        reply.Append(textBlock.Text);
                    }
                }
                Console.WriteLine($"claude> {reply}");
                AddAssistantMessage(messages, reply.ToString());
            }
        }
    }
    internal static void AddUserMessage(List<MessageParam> messages, string text)
    {
        messages.Add(new MessageParam
        {
            Role = Role.User,
            Content = text,
        });
    }
    internal static void AddAssistantMessage(List<MessageParam> messages, string text)
    {
        messages.Add(new MessageParam
        {
            Role = Role.Assistant,
            Content = text,
        });
    }
}