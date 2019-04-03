using System;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using System.Threading.Tasks;
using TwitchLib.Api;
using System.Linq;
using DitsyTwitch.Facades;
using DitsyTwitch.Modules;

namespace DitsyTwitch
{

    public class DitsyBot
    {
        private TwitchClient client;
        private TwitchAPI api;

        //public List<BotModule> Modules { get; set; }

        public const string GlobalPrefix = "!";

        public static List<BotModule> Modules { get; set; }

        public ConfigFacade ConfigFacade { get; set; }

        public DitsyBot()
        {
            Modules = new List<BotModule>();
            ConfigFacade = new ConfigFacade();

            client = new TwitchClient();
            api = new TwitchAPI();
        }

        public async Task Initialize()
        {
            var botConfig = await ConfigFacade.GetConfig();
            var credentials = new ConnectionCredentials("ditsyghostbot", botConfig.OAuth);
            api.Settings.ClientId = botConfig.ClientId;

            client.Initialize(credentials, "shredder89100");

            // Use dependency injection to retrieve module parameters
            var modules = BotModule.GetModules(client, api);

            await Task.WhenAll(Modules.Select(m => m.Initialize()));

            client.OnConnected += (s, e) =>
            {
                Console.WriteLine($"Connected to {e.AutoJoinChannel}");
            };

            client.OnJoinedChannel += (s, e) =>
            {
                Console.WriteLine("Joined server.");
            };

            client.OnNewSubscriber += (s, e) =>
            {
                client.SendMessage(e.Channel, $"Welcome to the Ghouls {e.Subscriber}!");
            };

            client.OnReSubscriber += (s, e) =>
            {
                client.SendMessage(e.Channel, $"Thanks for resubbing {e.ReSubscriber}! You've been a Ghoul for {e.ReSubscriber.Months} months!");
            };

            client.OnMessageReceived += async (s, e) =>
            {
                var chatMessage = e.ChatMessage;
                var message = chatMessage.Message.ToLower();
                var parts = message.Split(' ');
                var commandName = parts[0];
                var arguments = parts.Skip(1).ToArray();

                foreach (var module in modules) {
                    var commands = module.Commands.Where(c => GlobalPrefix + c.Name.ToLower() == commandName);

                    foreach (var command in commands) {
                        await command.Execute.Invoke(chatMessage, arguments);
                    }
                }
            };

            client.Connect();
        }
    }
}
