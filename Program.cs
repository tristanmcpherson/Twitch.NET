using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using TwitchLib.Api.V5;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Specialized;
using MongoDB.Driver;

namespace DitsyTwitch
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var bot = new DitsyBot();
            await bot.Initialize();
            Console.ReadLine();
        }
    }

    public class DitsyBot
    {
        public const string ClientId = "qq4qpgbfgu518rnjh68yea6nu2mib5";
        public const string OAuth = "g0lgtp95ldotc12vleu6osdr2onn1g";
        public TwitchClient client;
        public List<BotModule> Modules = new List<BotModule>();

        public DitsyBot()
        {
            var credentials = new ConnectionCredentials("ditsyghostbot", OAuth);
            var api = new TwitchAPI();
            api.Settings.ClientId = ClientId;

            client = new TwitchClient();
            client.Initialize(credentials, "shredder89100");
            client.Connect();

            Modules.Add(new GamblingModule(client, api));

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
                await Task.WhenAll(Modules.Select(m => m.OnMessageReceived(s, e)));
            };
        }

        public async Task Initialize()
        {
            await Task.WhenAll(Modules.Select(m => m.Initialize()));
        }
    }
}
