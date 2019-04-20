using System;
using System.Threading.Tasks;

namespace TwitchBot
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var bot = new TwitchBot();
            await bot.Initialize();
            Console.ReadLine();
        }
    }
}
