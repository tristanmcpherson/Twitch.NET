using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwitchNET.Attributes;
using TwitchNET.Modules;
using ShredBot.Facades;
using Fortuna.Generator;
using Fortuna;

namespace ShredBot.Modules
{
    public class GamblingModule : ModuleBase
    {
        private readonly GambleConfigFacade mConfigFacade;
        private readonly GambleFacade mGambleFacade;
        private readonly IPRNGFortunaProvider mProvider;

        public GamblingModule(GambleConfigFacade configFacade, GambleFacade gambleFacade, IPRNGFortunaProvider provider)
        {
            mConfigFacade = configFacade;
            mGambleFacade = gambleFacade;
            mProvider = provider;
        }

        public async Task<IEnumerable<string>> GetChatters(string channel)
        {
            using var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync($"http://tmi.twitch.tv/group/user/{channel}/chatters");
            var userTypes = new string[] { "broadcaster", "vips", "moderators", "staff", "admins", "global_mods", "viewers" };
            var chatters = JObject.Parse(json)["chatters"];
            var users = userTypes.SelectMany(userType => chatters[userType].ToArray()).Select(token => token.ToString());
            return users;
        }

        public override async Task Initialize()
        {
            var configs = await mConfigFacade.GetConfigurations();
            configs.ForEach(config =>
            {
                var task = Task.Run(async () =>
                {
                    while (true)
                    {
                        var startTime = DateTime.Now;

                        var chatters = await GetChatters(config.Channel);
                        await mGambleFacade.SetDefaultPoints(config.Channel, chatters);
                        await mGambleFacade.AddPoints(config.Channel, config.PointAwardAmount);

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
            return await mGambleFacade.GetLeaderboardPosition(channel, username);
        }

        public async Task<long> CalculatePoints(string channel, string username, string verb)
        {
            var numberMatch = Regex.Match(verb, @"^\d+");
            var percentMatch = Regex.Match(verb, @"^(?<percent>\d+)%");
            var allMatch = Regex.Match(verb, @"all", RegexOptions.IgnoreCase);

            if (percentMatch.Success)
            {
                var points = (await mGambleFacade.GetInfo(channel, username)).Points;
                var percent = percentMatch.Groups["percent"].Value;
                return (long)(points * (double.Parse(percent) / 100.0));
            }
            else if (numberMatch.Success)
            {
                return long.Parse(numberMatch.Value);
            }
            else if (allMatch.Success)
            {
                return (await mGambleFacade.GetInfo(channel, username)).Points;
            }
            return 0;
        }

        [Command("leaderboard")]
        public async Task Leaderboard()
        {
            var leaderboard = await mGambleFacade.GetLeaderboard(Context.Channel);
            var messages = leaderboard.Select((item, index) => $"{index + 1}: {item.Username} - {item.Points}").ToList();
            messages.ForEach(m => Client.SendMessage(Context.Channel, m));
        }

        [Command("points")]
        public async Task Points()
        {
            var config = await mConfigFacade.GetConfiguration(Context.Channel);

            var position = await GetLeaderboardPosition(Context.Channel, Context.Username);
            var info = await mGambleFacade.GetInfo(Context.Channel, Context.Username);
            Client.SendMessage(Context.Channel, $"@{Context.Username} You have {info.Points} {config.Currency}! You are position {position} on the Leaderboard.");
        }

        [Command("gamble")]
        public async Task Gamble(string verb)
        {
            var channel = Context.Channel;
            var username = Context.Username;

            var config = await mConfigFacade.GetConfiguration(channel);

            if (string.IsNullOrWhiteSpace(verb))
            {
                return;
            }

            var info = await mGambleFacade.GetInfo(channel, username);
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

            var vals = new Dictionary<int, int>();
            for (int i = 0; i < 100000; i++)
            {
                var num = GetRandomNumber();
                vals[num]++;
            }

            Console.WriteLine(string.Join(',', vals.Keys));

            var roll = GetRandomNumber();
            var outcome = roll < 50;

            var newPoints = outcome ? (points + pointsToGamble) : (points - pointsToGamble);

            if (outcome)
            {
                await mGambleFacade.AddPoints(channel, username, pointsToGamble);
            }
            else
            {
                await mGambleFacade.RemovePoints(channel, username, pointsToGamble);
            }

            Client.SendMessage(channel, $"@{username} rolled a {roll}. You {(outcome ? "won" : "lost")} {pointsToGamble} {config.Currency}. Your total is now {newPoints} {config.Currency}");
        }

        private int GetRandomNumber()
        {


            var bytes = new byte[4];
            mProvider.GetBytes(bytes);
            return (int)((BitConverter.ToInt32(bytes, 0) + 1) / 255f) * 100;
        }
    }
}