using System.Threading.Tasks;
using DitsyTwitch.Attributes;
using DitsyTwitch.Utilities;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace DitsyTwitch.Modules {
    [Module]
    public class InfoModule : BotModule {
        public InfoModule(TwitchClient client, TwitchAPI api) : base(client, api)
        {
        }

        [Command("uptime")]
        public async Task Uptime(ChatMessage chatMessage, string[] arguments) {
            var streamOnline = await Api.V5.Streams.BroadcasterOnlineAsync(chatMessage.Channel);
            if (streamOnline) {
                var info = await Api.V5.Streams.GetUptimeAsync(chatMessage.Channel);
                Client.SendMessage(chatMessage.Channel, $"{chatMessage.Channel} has been live for {info.Value.ToUserFriendlyString()}");
            }
        }
    }
}