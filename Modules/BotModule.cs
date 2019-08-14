using System;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;


namespace TwitchNET.Modules
{
    public abstract class ChatContext
    {
        public string Username { get; set; }

        public string Channel { get; set; }

        public ChatMessage ChatMessage { get; set; }
    }

    public abstract class BotModule
    {
        protected TwitchClient Client { get; set; }

        protected TwitchAPI Api { get; set; }

        public ChatContext Context { get; set; }

        public const string GlobalPrefix = "!";

        public void Initialize(TwitchClient client, TwitchAPI api)
        {
            Client = client;
            Api = api;
        }

        public virtual Task Initialize() => Task.CompletedTask;

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}: {message}");
        }
    }
}
