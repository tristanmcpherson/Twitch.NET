using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DitsyTwitch;
using DitsyTwitch.Attributes;
using DitsyTwitch.Facades;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace DitsyTwitch.Modules
{
    [Module]
    public class GamblingModule : BotModule
    {
        private GambleFacade Facade = new GambleFacade();
        private GambleConfigFacade ConfigFacade = new GambleConfigFacade();
        private Random random = new Random();
        public async Task<IEnumerable<string>> GetChatters(string channel)
        {
            var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync($"http://tmi.twitch.tv/group/user/{channel}/chatters");
            var userTypes = new string[] { "broadcaster", "vips", "moderators", "staff", "admins", "global_mods", "viewers" };
            var chatters = JObject.Parse(json)["chatters"];
            var users = userTypes.SelectMany(userType => chatters[userType].ToArray()).Select(token => token.ToString());
            return users;
        }

        public GamblingModule(TwitchClient client, TwitchAPI api) : base(client, api)
        {
        }

        public override async Task Initialize()
        {
            var configs = await ConfigFacade.GetConfigurations();
            configs.ForEach(config =>
            {
                var task = Task.Run(async () =>
                {
                    while (true)
                    {
                        var startTime = DateTime.Now;

                        var chatters = await GetChatters(config.Channel);
                        await Facade.SetDefaultPoints(config.Channel, chatters);
                        await Facade.AddPoints(config.Channel, config.PointAwardAmount);

                        Debug($"Awarded points for {config.Channel}! Waiting again.");
                        var endTime = DateTime.Now;
                        var elapsed = endTime - startTime;
                        if (elapsed > TimeSpan.FromMinutes(config.PointAwardInterval))
                        {
                            Debug("Warning. Execution exceeding wait interval.");
                            continue;
                        }

                        await Task.Delay(TimeSpan.FromMinutes(config.PointAwardInterval) - elapsed);
                    }
                });
            });
        }

        public async Task<long> GetLeaderboardPosition(string channel, string username)
        {
            return await Facade.GetLeaderboardPosition(channel, username);
        }

        public async Task<long> CalculatePoints(string channel, string username, string verb)
        {
            var numberMatch = Regex.Match(verb, @"^\d+");
            var percentMatch = Regex.Match(verb, @"^(?<percent>\d+)%");
            var allMatch = Regex.Match(verb, @"all", RegexOptions.IgnoreCase);

            if (percentMatch.Success)
            {
                var points = (await Facade.GetInfo(channel, username)).Points;
                var percent = percentMatch.Groups["percent"].Value;
                return (long)(points * (double.Parse(percent) / 100.0));
            }
            else if (numberMatch.Success)
            {
                return long.Parse(numberMatch.Value);
            }
            else if (allMatch.Success)
            {
                return (await Facade.GetInfo(channel, username)).Points;
            }
            return 0;
        }

        [Command("leaderboard")]
        public async Task Leaderboard(ChatMessage chatMessage, string[] arguments)
        {
            var channel = chatMessage.Channel;
            var leaderboard = await Facade.GetLeaderboard(channel);
            var messages = leaderboard.Select((item, index) => $"{index + 1}: {item.Username} - {item.Points}").ToList();
            messages.ForEach(m => Client.SendMessage(channel, m));
        }

        [Command("points")]
        public async Task Points(ChatMessage chatMessage, string[] arguments)
        {
            var channel = chatMessage.Channel;
            var username = chatMessage.Username;
            var config = await ConfigFacade.GetConfiguration(channel);

            var position = await GetLeaderboardPosition(channel, username);
            var info = await Facade.GetInfo(channel, username);
            Client.SendMessage(channel, $"@{username} You have {info.Points} {config.Currency}! You are position {position} on the Leaderboard.");
        }

        [Command("gamble")]
        public async Task Gamble(ChatMessage chatMessage, string[] arguments)
        {
            var channel = chatMessage.Channel;
            var username = chatMessage.Username;
            var verb = arguments[0];

            var config = await ConfigFacade.GetConfiguration(channel);

            if (string.IsNullOrWhiteSpace(verb))
            {
                return;
            }

            var info = await Facade.GetInfo(channel, username);
            var points = info.Points;

            if (points == 0)
            {
                Client.SendMessage(channel, $"Sorry! You don't have any {config.Currency}. You are awarded {config.PointAwardAmount} every {config.PointAwardInterval}.");
                return;
            }

            var pointsToGamble = await CalculatePoints(channel, username, verb);

            if (pointsToGamble > points || pointsToGamble == 0)
            {
                return;
            }

            var roll = random.Next(1, 101);
            var outcome = roll < 50;

            var newPoints = outcome ? (points + pointsToGamble) : (points - pointsToGamble);

            if (outcome)
            {
                await Facade.AddPoints(channel, username, pointsToGamble);
            }
            else
            {
                await Facade.RemovePoints(channel, username, pointsToGamble);
            }

            Client.SendMessage(channel, $"@{username} rolled a {roll}. You {(outcome ? "won" : "lost")} {pointsToGamble} {config.Currency}. Your total is now {newPoints} {config.Currency}");
        }
    }
}