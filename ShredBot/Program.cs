using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchLib.Client.Models;
using TwitchNET;
using Fortuna;
using ShredBot.Facades;
using TwitchMoq;
using Grpc.Core;

namespace ShredBot
{
    public static class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((services) =>
                    services
                        .AddSingleton(_ => PRNGFortunaProviderFactory.Create())
                        .AddSingleton<ConfigFacade>()
                        .AddSingleton<GambleConfigFacade>()
                        .AddSingleton<GambleFacade>()
                        .AddHostedService<Bot>());

            await builder.RunConsoleAsync();
        }
    }

    public class Bot : IHostedService
    {
        private readonly IServiceProvider mServiceProvider;
        private readonly ConfigFacade mConfigFacade;

        public Bot(IServiceProvider serviceProvider, ConfigFacade configFacade)
        {
            mServiceProvider = serviceProvider;
            mConfigFacade = configFacade;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var port = 50052;
            var twitchMock = new TwitchMock();

            Task.Factory.StartNew(async () =>
            {
                Server server = new Server
                {
                    Services = { Chat.TwitchChat.BindService(twitchMock) },
                    Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
                };
                server.Start();

                Console.WriteLine("TwitchChat server listening on port " + port);
                Console.WriteLine("Press any key to stop the server...");
                Console.ReadKey();

                await Task.Delay(-1);

                await server.ShutdownAsync();
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);

            Console.WriteLine("ShredBot started.");
            var config = await mConfigFacade.GetConfig();
            //var bot = new TwitchBot(new ConnectionCredentials(config.UserName, config.OAuth), null, config.ClientId, "shredder89100");
            var bot = new TwitchBot(twitchMock, null);
            bot.Initialize(mServiceProvider);
            Console.ReadLine();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
