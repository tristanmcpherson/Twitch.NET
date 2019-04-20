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
using System.IO;

namespace DitsyTwitch.Modules
{
    public abstract class BotModule
    {
        internal List<Command> Commands = new List<Command>();
        public static TwitchClient Client { get; set; }
        public static TwitchAPI Api { get; set; }
        public const string GlobalPrefix = "!";

        public static List<BotModule> GetModules(params object[] parameters)
        {
            var assemblies = new List<Assembly>
            {
                Assembly.GetExecutingAssembly()
            };
            
            var executingPath = assemblies.First().Location;
            var directory = Path.GetDirectoryName(executingPath);

            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var rawAssembly = File.ReadAllBytes(file);
                var assembly = Assembly.Load(rawAssembly);
                var hasModules = assembly.GetTypes().Any(t => t.GetCustomAttribute<CommandAttribute>() != null);
                if (hasModules)
                {
                    assemblies.Add(assembly);
                }
            }

            var modules = new List<BotModule>();

            foreach (var assembly in assemblies)
            {
                var moduleTypes = assembly.GetTypes().Where(t => t.GetCustomAttribute<ModuleAttribute>() != null);

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
            }

            return modules;
        }

        public virtual Task Initialize() {
            return Task.CompletedTask;
        }

        public void Debug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}: {message}");
        }
    }
}
