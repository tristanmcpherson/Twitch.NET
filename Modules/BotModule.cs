using System;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using MongoDB.Driver;
using System.Threading.Tasks;
using TwitchLib.Api;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using DitsyTwitch.Attributes;

namespace DitsyTwitch.Modules
{
    public abstract class BotModule
    {
        internal List<Command> Commands = new List<Command>();
        public TwitchClient Client { get; set; }
        public TwitchAPI Api { get; set; }
        public const string GlobalPrefix = "!";
        public BotModule(TwitchClient client, TwitchAPI api)
        {
            Client = client;
            Api = api;
        }

        public static List<BotModule> GetModules(params object[] parameters)
        {
            var assembly = Assembly.GetCallingAssembly();
            var moduleTypes = assembly.GetTypes().Where(t => t.GetCustomAttribute<ModuleAttribute>() != null);

            var modules = new List<BotModule>();

            foreach (var moduleType in moduleTypes)
            {

                var module = (BotModule)Activator.CreateInstance(moduleType, parameters);

                var methods = moduleType.GetMethods()
                        .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
                        .Where(m => m.ReturnType == typeof(Task) && m.GetParameters().First().ParameterType == typeof(ChatMessage))
                        .ToArray();

                var commands = methods.Select(m => new Command
                {
                    Name = m.GetCustomAttribute<CommandAttribute>().CommandName,
                    Execute = (Command.CommandDelegate)m.CreateDelegate(typeof(Command.CommandDelegate), module)
                }).ToList();

                module.Commands = commands;

                modules.Add(module);
            }

            return modules;
        }

        public abstract Task Initialize();

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}: {message}");
        }
    }
}
