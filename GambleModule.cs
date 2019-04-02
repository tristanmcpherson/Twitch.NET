using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DitsyTwitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;

public class GamblingModule : BotModule
{
    private TwitchClient _twitchClient;
    private TwitchAPI _twitchApi;
    private GambleAccess Access = new GambleAccess();
    private GambleConfigAccess ConfigAccess = new GambleConfigAccess();
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

    public GamblingModule(TwitchClient client, TwitchAPI api) : base(client, new string[] { "gamble", "points", "leaderboard" })
    {
        _twitchClient = client;
        _twitchApi = api;
    }

    public override async Task Initialize() {
        var configs = await ConfigAccess.GetConfigurations();
        configs.ForEach(config => {
            var task = Task.Run(async () =>
            {
                while (true)
                {
                    var startTime = DateTime.Now;

                    var chatters = await GetChatters(config.Channel);
                    await Access.SetDefaultPoints(config.Channel, chatters);
                    await Access.AddPoints(config.Channel, config.PointAwardAmount);

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

    public override async Task HandleMessage(ChatMessage chatMessage)
    {
        var channel = chatMessage.Channel;
        var config = await ConfigAccess.GetConfiguration(channel);

        var username = chatMessage.Username;
        var parts = chatMessage.Message.ToLower().Split(' ').ToArray();
        if (parts[0] == "!gamble")
        {
            await Gamble(channel, username, parts[1]);
        }
        else if (parts[0] == "!points")
        {
            var position = await GetLeaderboardPosition(channel, username);
            var info = await Access.GetInfo(channel, username);
            Client.SendMessage(channel, $"@{username} You have {info.Points} {config.Currency}! You are position {position} on the Leaderboard.");
        }
        else if (parts[0] == "!leaderboard")
        {
            var leaderboard = await Access.GetLeaderboard(channel);
            var messages = leaderboard.Select((item, index) => $"{index + 1}: {item.Username} - {item.Points}").ToList();
            messages.ForEach(m => Client.SendMessage(channel, m));
        }
    }

    public async Task<long> GetLeaderboardPosition(string channel, string username)
    {
        return await Access.GetLeaderboardPosition(channel, username);
    }

    public async Task<long> CalculatePoints(string channel, string username, string verb)
    {
        var numberMatch = Regex.Match(verb, @"^\d+");
        var percentMatch = Regex.Match(verb, @"^(?<percent>\d+)%");
        var allMatch = Regex.Match(verb, @"all", RegexOptions.IgnoreCase);

        if (percentMatch.Success)
        {
            var points = (await Access.GetInfo(channel, username)).Points;
            var percent = percentMatch.Groups["percent"].Value;
            return (long)(points * (double.Parse(percent) / 100.0));
        }
        else if (numberMatch.Success)
        {
            return long.Parse(numberMatch.Value);
        }
        else if (allMatch.Success)
        {
            return (await Access.GetInfo(channel, username)).Points;
        }
        return 0;
    }


    public async Task Gamble(string channel, string username, string verb)
    {
        var config = await ConfigAccess.GetConfiguration(channel);

        if (string.IsNullOrWhiteSpace(verb))
        {
            return;
        }

        var info = await Access.GetInfo(channel, username);
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
            await Access.AddPoints(channel, username, pointsToGamble);
        }
        else
        {
            await Access.RemovePoints(channel, username, pointsToGamble);
        }

        Client.SendMessage(channel, $"@{username} rolled a {roll}. You {(outcome ? "won" : "lost")} {pointsToGamble} {config.Currency}. Your total is now {newPoints} {config.Currency}");
    }
}