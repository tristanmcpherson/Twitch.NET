using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
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

namespace DitsyTwitch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var bot = new DitsyBot();
            Console.ReadLine();
        }
    }

    public class DitsyBot
    {
        public const string ClientId = "qq4qpgbfgu518rnjh68yea6nu2mib5";
        //public const string ClientSecret = "q62w3almg0bmnu1ls4ecdmjyyn9532";

        public const string OAuth = "t5soqxx11oe23qqg2s77m3vzmr3eej";
        public TwitchClient client;
        public List<BotModule> modules = new List<BotModule>();

        public DitsyBot()
        {
            var credentials = new ConnectionCredentials("ditsyghostbot", OAuth);
            var api = new TwitchAPI();
            api.Settings.ClientId = ClientId;

            client = new TwitchClient();
            client.Initialize(credentials, "ditsyghost");
            client.Connect();

            modules.Add(new GamblingModule(client, api, "ditsyghost"));

            client.OnConnected += (s, e) =>
            {
                Console.WriteLine($"Connected to {e.AutoJoinChannel}");
            };
            client.OnJoinedChannel += (s, e) =>
            {
                Console.WriteLine("Sending joined message.");
                //client.SendMessage(e.Channel, "Hi! This is a test.");
            };
            client.OnNewSubscriber += (s, e) =>
            {
                client.SendMessage(e.Channel, $"Welcome to the Ghouls {e.Subscriber}!");
            };
            client.OnReSubscriber += (s, e) =>
            {
                client.SendMessage(e.Channel, $"Thanks for resubbing {e.ReSubscriber}! You've been a Ghoul for {e.ReSubscriber.Months} months!");
            };
            client.OnMessageReceived += (s, e) =>
            {
                foreach (var module in modules)
                    module.OnMessageReceived(s, e);
            };
        }

    }

    public abstract class BotModule
    {
        public TwitchClient Client { get; set; }
        public const string GlobalPrefix = "!";
        public string[] Prefix { get; set; }
        public BotModule(TwitchClient client, string[] prefix)
        {
            Client = client;
            Prefix = prefix;
        }

        public abstract void HandleMessage(ChatMessage chatMessage);

        public void OnMessageReceived(object sender, OnMessageReceivedArgs args)
        {
            foreach (var prefix in Prefix)
            {
                var totalPrefix = GlobalPrefix + prefix;
                if (args.ChatMessage.Message.ToLower().StartsWith(totalPrefix))
                {
                    this.HandleMessage(args.ChatMessage);
                }
            }
        }

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}: {message}");
        }
    }

    public class GamblingModule : BotModule
    {
        private static TimeSpan PointInterval = TimeSpan.FromMinutes(2);
        private static ulong PointsPerTime = 5;
        private static string Currency = "DitsCoins";
        private string Channel { get; set; }
        // UserId, points
        private ConcurrentDictionary<string, ulong> Points = new ConcurrentDictionary<string, ulong>();

        private Random random = new Random();

        public GamblingModule(TwitchClient client, TwitchAPI api, string channel) : base(client, new string[] { "gamble", "points" })
        {
            Channel = channel;

            if (File.Exists("points.json"))
            {
                var pointsJson = File.ReadAllText("points.json");
                Points = JsonConvert.DeserializeObject<ConcurrentDictionary<string, ulong>>(pointsJson);
            }

            Task task = Task.Run(async () =>
            {
                while (true)
                {
                    var chatters = await api.Undocumented.GetChattersAsync(Channel);
                    chatters.ForEach(chatter =>
                    {
                        if (Points.ContainsKey(chatter.Username))
                            Points[chatter.Username] += PointsPerTime;
                        else
                            Points[chatter.Username] = 50;
                    });

                    Debug("Awarded points! Waiting again. Also Saving.");
                    File.WriteAllText("points.json", JsonConvert.SerializeObject(Points));
                    await Task.Delay(PointInterval);
                }
            });
        }

// add mongo makes this stuff ez
        public int GetLeaderboardPosition(string username) {
            var leaderboard = Points.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
            return leaderboard.IndexOf(username) + 1;
        }

        public ulong CalculatePoints(string username, string verb)
        {
            var numberMatch = Regex.Match(verb, @"^\d+");
            var percentMatch = Regex.Match(verb, @"^(?<percent>\d+)%");
            var allMatch = Regex.Match(verb, @"all", RegexOptions.IgnoreCase);

            if (percentMatch.Success)
            {
                var percent = percentMatch.Groups["percent"].Value;
                return (ulong)(Points[username] * (double.Parse(percent) / 100.0));
            }
            else if (numberMatch.Success)
            {
                return ulong.Parse(numberMatch.Value);
            }
            else if (allMatch.Success)
            {
                return Points[username];
            }
            return 0;
        }

        public override void HandleMessage(ChatMessage chatMessage)
        {
            var username = chatMessage.Username;
            var parts = chatMessage.Message.ToLower().Split(' ').ToArray();
            if (parts[0] == "!gamble")
            {
                Gamble(username, parts[1]);
            }
            else if (parts[0] == "!points")
            {
                var position = GetLeaderboardPosition(username);
                Client.SendMessage(Channel, $"@{username} You have {Points[username]} {Currency}! You are position {position} on the Leaderboard.");
            }
        }

        public void Gamble(string username, string verb)
        {
            if (string.IsNullOrWhiteSpace(verb))
            {
                return;
            }
            var points = Points[username];

            if (points == 0)
            {
                Client.SendMessage(Channel, $"Sorry! You don't have any {Currency}. You are awarded {PointsPerTime} every {PointInterval}.");
                return;
            }

            var pointsToGamble = CalculatePoints(username, verb);

            if (pointsToGamble > points || pointsToGamble == 0)
            {
                return;
            }

            Console.WriteLine($"Gambling {verb}={pointsToGamble}");
            var roll = random.Next(1, 101);
            var outcome = roll < 50;

            var newPoints = outcome ? (points + pointsToGamble) : (points - pointsToGamble);

            if (outcome)
            {
                Points[username] += pointsToGamble;
            }
            else
            {
                Points[username] -= pointsToGamble;
            }

            Client.SendMessage(Channel, $"@{username} rolled a {roll}. You {(outcome ? "won" : "lost")} {pointsToGamble} {Currency}. Your total is now {newPoints} {Currency}");
        }
    }
}
