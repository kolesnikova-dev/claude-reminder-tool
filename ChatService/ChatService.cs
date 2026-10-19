using System.Text;
using Anthropic;
using Anthropic.Models.Messages;

using ClaudeReminder;

internal class ChatService
{
    internal static async Task Chat(AnthropicClient client, ChatConfig config)
    {
        var (messages, stopSequences, system) = config;
        while (true)
        {
            Console.Write("you> ");
            string? userMessage = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userMessage)) break; // Exit on empty input
            AddUserMessage(messages, userMessage);

            MessageCreateParams parameters = new()
            {
                MaxTokens = 1024,
                Messages = messages,
                Model = Model.ClaudeHaiku4_5,
                System = system,
                StopSequences = stopSequences,
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