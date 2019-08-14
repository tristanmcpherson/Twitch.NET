using System;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using System.Threading.Tasks;
using TwitchLib.Api;
using System.Linq;
using TwitchNET.Modules;
using TwitchNET.Parsing;
using System.Reflection.Emit;
using System.Reflection;

namespace TwitchNET
{

    public class TwitchBot
    {
        private readonly TwitchClient _client;
        private readonly TwitchAPI _api;

        public static string Prefix = "!";

        private List<Command> Commands;
        private IServiceProvider mServiceProvider;

        public TwitchBot(ConnectionCredentials credentials, string accessToken, string clientID)
        {
            _client = new TwitchClient();
            _client.SetConnectionCredentials(credentials);

            _api = new TwitchAPI();
            _api.Settings.AccessToken = accessToken;
            _api.Settings.ClientId = clientID;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            mServiceProvider = serviceProvider;

	        Commands = Command.GetCommands();

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

            if (command == null)
            {
                return;
            }

            var instance = command.CreateModuleInstance();
            var task = command.CreateTask(args);
            await task?.Invoke(instance, args?.ToArray());
        }

        

        public static (Command, object[]) GetMatchingCommandByArgs(IEnumerable<Command> commands, string[] args)
        {
            if (args.Length == 0)
            {
                return (commands.First(), null);
            }

            foreach (var command in commands)
            {
                var parses = command.Arguments.Select(a => Command.ParseArgument[a]).ToList();
                var outputs = args.Select((a,i) => parses[i](a));
                if (outputs.All((parseOutput) => parseOutput.Item1))
                {
                    return (command, outputs.Select(o => o.Item2).ToArray());
                }
            }

            return (null, null);
        }
    }
}
