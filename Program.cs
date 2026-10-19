using System;
using DotNetEnv;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeReminder
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            DotNetEnv.Env.Load();
            string anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
            throw new Exception("ANTHROPIC_API_KEY environment variable is not set.");

            if (anthropicApiKey != "")
            {
                AnthropicClient client = new();
                Console.WriteLine("Anthropic client initialized successfully.");
                List<string> stopSequences = new() { };
                List<MessageParam> messages = new() { };
                await Chat(client, messages, stopSequences);
            }
        }
        internal static async Task Chat(AnthropicClient client, List<MessageParam> messages, List<string> stopSequences)
        {
            string system = "You are patient and understanding of a developer trying to build with you. " +
           "The dev configured a continuous message loop.";
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
}


