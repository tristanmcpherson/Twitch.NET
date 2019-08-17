using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Api;
using TwitchNET.Parsing;
using TwitchNET.Modules;
using TwitchLib.Client.Interfaces;
using TwitchLib.Api.Interfaces;
using Moq;

namespace TwitchNET
{

    public class TwitchBot
    {
        private readonly ITwitchClient _client;
        private readonly ITwitchAPI _api;

        public static char Prefix = '!';

        private List<Command> Commands;
        private IServiceProvider mServiceProvider;

        public TwitchBot(ConnectionCredentials credentials, string accessToken, string clientID, string channel)
        {
            _client = new TwitchClient();
            _client.Initialize(credentials, channel, Prefix, Prefix);
            _client.SetConnectionCredentials(credentials);

            _api = new TwitchAPI();
            _api.Settings.AccessToken = accessToken;
            _api.Settings.ClientId = clientID;
        }

        public TwitchBot(ITwitchClient client, ITwitchAPI api)
        {
            _client = client;
            _api = api;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            mServiceProvider = serviceProvider;

            Commands = Command.GetCommands(mServiceProvider);

            _client.OnConnected += (s, e) =>
            {
                Console.WriteLine($"Connected to {e.AutoJoinChannel}");
            };

            _client.OnJoinedChannel += (s, e) =>
            {
                Console.WriteLine("Joined server.");
            };

            _client.OnNewSubscriber += (s, e) =>
            {
            };

            _client.OnReSubscriber += (s, e) =>
            {
            };

            _client.OnMessageReceived += async (s, e) => await ProcessCommand(e.ChatMessage);

            _client.Connect();
        }

        public async Task ProcessCommand(ChatMessage chatMessage)
        {
            var message = chatMessage.Message.ToLower();
            var parts = message.Split(' ');
            var commandName = parts[0];
            var arguments = parts.Skip(1).ToArray();

            var potentialCommands = Commands.Where(c => Prefix + c.Name.ToLower() == commandName).ToList();
            if (potentialCommands.Count == 0)
            {
                return;
            }

            var (command, args) = GetMatchingCommandByArgs(potentialCommands, arguments);
            //if (args != null && arguments.Length > 0, && args.) 

            if (command == null)
            {
                return;
            }

            var instance = command.CreateModuleInstance();
            var context = new ChatContext
            {
                ChatMessage = chatMessage,
                Channel = chatMessage.Channel,
                Username = chatMessage.Username
            };

            instance.Initialize(_client, _api, context);

            var task = command.CreateTask(args);
            await task?.Invoke(instance, args?.ToArray());
        }

        public static (Command, object[]) GetMatchingCommandByArgs(IEnumerable<Command> commands, string[] args)
        {
            if (args.Length == 0)
            {
                return (commands.FirstOrDefault(c => c.Arguments.Count() == 0), null);
            }

            foreach (var command in commands)
            {
                var parses = command.Arguments.Select(a => Command.ParseArgument[a]).ToList();
                var outputs = args.Select((a, i) => parses[i](a));
                if (outputs.All((parseOutput) => parseOutput.Item1))
                {
                    return (command, outputs.Select(o => o.Item2).ToArray());
                }
            }

            return (null, null);
        }
    }
}
