using TwitchLib.Client.Models;
using System.Threading.Tasks;

namespace DitsyTwitch
{
    public class Command
    {
        public delegate Task CommandDelegate(ChatMessage chatMessage, string[] arguments);
        public string Name { get; set; }
        public CommandDelegate Execute { get; set; }
    }
}
