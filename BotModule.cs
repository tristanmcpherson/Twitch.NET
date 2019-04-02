using System;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace DitsyTwitch
{
    public abstract class BotModule
    {
        public static MongoClient MongoClient = new MongoClient();
        public TwitchClient Client { get; set; }
        public const string GlobalPrefix = "!";
        public string[] Prefix { get; set; }
        public BotModule(TwitchClient client, string[] prefix)
        {
            Client = client;
            Prefix = prefix;
        }

        public abstract Task Initialize();

        public abstract Task HandleMessage(ChatMessage chatMessage);

        public async Task OnMessageReceived(object sender, OnMessageReceivedArgs args)
        {
            foreach (var prefix in Prefix)
            {
                var totalPrefix = GlobalPrefix + prefix;
                if (args.ChatMessage.Message.ToLower().StartsWith(totalPrefix))
                {
                    await this.HandleMessage(args.ChatMessage);
                }
            }
        }

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}: {message}");
        }
    }
}
