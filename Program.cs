using System;
using System.Text.Json;
using DotNetEnv;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;

namespace ClaudeReminder
{

    internal sealed class ChatConfig
    {
        public List<MessageParam> Messages { get; init; } = new();
        public List<string> StopSequences { get; init; } = new();
        public string System { get; init; } =
            "You are patient and understanding of a developer trying to build with you. " +
            "The dev configured a continuous message loop.";
        public void Deconstruct(out List<MessageParam> messages,
                      out List<string> stopSequences,
                      out string system)
        {
            messages = Messages;
            stopSequences = StopSequences;
            system = System;
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            DotNetEnv.Env.Load();

            AnthropicClient client;
            try
            {
                client = new() { ResponseValidation = true };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Anthropic client: {ex.Message}");
                return;
            }
            Console.WriteLine("Anthropic client initialized successfully.");

            ChatConfig config = new ChatConfig
            {
                Messages = { new MessageParam { Role = Role.User, Content = "What is the exact time, formatted as HH:MM:SS?" } }
            };
            try
            {
                // await ChatService.Chat(client, config);
                var response = await client.Messages.Create(new MessageCreateParams
                {
                    MaxTokens = 1024,
                    Messages = config.Messages,
                    Model = Model.ClaudeHaiku4_5,
                    System = config.System,
                    StopSequences = config.StopSequences,
                    Tools = ToolSchemas.All
                });
                // var reply = new StringBuilder();
                // foreach (var block in response.Content)
                // {
                //     if (block.TryPickText(out var textBlock))
                //     {
                //         reply.Append(textBlock.Text);
                //     }
                // }
                // Console.WriteLine($"claude> {reply}");
                Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during chat: {ex.Message}");
            }
        }
    }
}


