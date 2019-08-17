using System.Threading.Tasks;
using TwitchNET.Utilities;
using TwitchNET.Attributes;
using TwitchNET.Modules;

namespace ShredBot.Modules
{
    public class InfoModule : ModuleBase {

        [Command("uptime")]
        public async Task Uptime() {
            var streamOnline = await Api.V5.Streams.BroadcasterOnlineAsync(Context.Channel);
            if (streamOnline) {
                var info = await Api.V5.Streams.GetUptimeAsync(Context.Channel);
                Client.SendMessage(Context.Channel, $"{Context.Channel} has been live for {info.Value.ToUserFriendlyString()}");
            }
        }
    }
}