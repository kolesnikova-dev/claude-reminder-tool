using System;
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
                client = new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Anthropic client: {ex.Message}");
                return;
            }
            Console.WriteLine("Anthropic client initialized successfully.");

            ChatConfig config = new();
            try
            {
                await ChatService.Chat(client, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during chat: {ex.Message}");
            }
        }
    }
}


