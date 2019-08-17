using System;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;


namespace TwitchNET.Modules
{
    public class ChatContext
    {
        public string Username { get; set; }

        public string Channel { get; set; }

        public ChatMessage ChatMessage { get; set; }
    }

    public abstract class ModuleBase
    {
        protected ITwitchClient Client { get; set; }

        protected ITwitchAPI Api { get; set; }

        public ChatContext Context { get; set; }

        public const string GlobalPrefix = "!";

        public void Initialize(ITwitchClient client, ITwitchAPI api, ChatContext context)
        {
            Client = client;
            Api = api;
            Context = context;
        }

        public virtual Task Initialize() => Task.CompletedTask;

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}: {message}");
        }
    }
}
