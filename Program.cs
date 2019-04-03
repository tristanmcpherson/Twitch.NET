using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using TwitchLib.Api.V5;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
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
}
