using System;
using DotNetEnv;
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
                await InitializeClient(anthropicApiKey);
                Console.WriteLine("Hello World!");
            }
        }
        internal static async Task InitializeClient(string apiKey)
        {
            AnthropicClient client = new();
            Console.WriteLine("Anthropic client initialized successfully.");
            string system = "You are patient and understanding of a developer trying to build with you. " +
            "The dev configured a continuous message loop.";

            MessageCreateParams parameters = new()
            {
                MaxTokens = 1024,
                Messages =
                            [
                                new()
                                    {
                                        Role = Role.User,
                                        Content = Console.ReadLine() ?? throw new Exception("Input is required."),
                                    },
                                ],
                Model = Model.ClaudeHaiku4_5,
                System = system,
            };

            var message = await client.Messages.Create(parameters);

            foreach (var block in message.Content)
            {
                if (block.TryPickText(out var textBlock))
                {
                    Console.WriteLine(textBlock.Text);
                }
            }
        }
    }
}


