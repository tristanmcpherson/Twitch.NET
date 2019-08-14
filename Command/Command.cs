using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TwitchNET.Modules;
using TwitchLib.Client.Models;
using System.Linq.Expressions;
using System.Linq;
using System.IO;
using TwitchNET.Attributes;
using System.Reflection.Emit;

namespace TwitchNET.Parsing
{
	public class Command
    {
        public delegate Task CommandDelegate(BotModule instance, object[] arguments);
        public delegate BotModule CreateModule();
        public CreateModule CreateModuleInstance { get; set; }
        public string Name { get; set; }
        public MethodInfo Method { get; set; }
	    public IEnumerable<Type> Arguments { get; set; }

        public static Dictionary<Type, Func<string, (bool, object)>> ParseArgument =
            new Dictionary<Type, Func<string, (bool, object)>>
            {
                { typeof(string), s => (true, s) },
                { typeof(bool), s => (bool.TryParse(s, out var val), val) },
                { typeof(int), s => (int.TryParse(s, out var val), val) },
                { typeof(float), s => (float.TryParse(s, out var val), val) },
                { typeof(double), s => (double.TryParse(s, out var val), val) }
            };

        public static List<Command> GetCommands()
        {
            var assemblies = new List<Assembly>
            {
                Assembly.GetExecutingAssembly()
            };

            var executingPath = assemblies.First().Location;
            var directory = Path.GetDirectoryName(executingPath);

            foreach (var file in Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                var rawAssembly = File.ReadAllBytes(file);
                var assembly = Assembly.Load(rawAssembly);
                var hasModules = assembly.GetTypes().Any(t => t.IsClass && typeof(BotModule).IsAssignableFrom(t));
                if (hasModules)
                {
                    assemblies.Add(assembly);
                }
            }

            var commands = new List<Command>();

            foreach (var assembly in assemblies)
            {
                var moduleTypes = assembly.GetTypes().Where(t => typeof(BotModule).IsAssignableFrom(t));

                foreach (var moduleType in moduleTypes)
                {
                    var methods = moduleType.GetMethods()
                            .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
                            .Where(m => m.ReturnType == typeof(Task) && m.GetParameters().First().ParameterType == typeof(ChatMessage))
                            .ToArray();

                    var constructorInfo = moduleType.GetConstructor(Type.EmptyTypes);
                    var newExpression = Expression.New(constructorInfo);
                    var newLambda = Expression.Lambda(newExpression);

                    commands.AddRange(methods.Select(method => new Command
                    {
                        Name = method.GetCustomAttribute<CommandAttribute>().CommandName,
                        Arguments = method.GetParameters().Select(p => p.ParameterType),
                        Method = method,
                        CreateModuleInstance = ((CreateModule)newLambda.Compile())
                    }));
                }
            }

            return commands;
        }

        public Command.CommandDelegate CreateTask(object[] args)
        {
            DynamicMethod dynamicMethod = new DynamicMethod("ExecuteCommand", typeof(Task), new[] { typeof(BotModule), typeof(ChatMessage), typeof(object[]) });
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldc_I4, i);

                    var arg = args[i];
                    if (arg.GetType() == typeof(string))
                    {
                        il.Emit(OpCodes.Ldelem_Ref);
                    }
                    else if (arg.GetType() == typeof(int))
                    {
                        il.Emit(OpCodes.Ldelem_I4);
                    }
                }
            }

            il.EmitCall(OpCodes.Call, Method, null);
            il.Emit(OpCodes.Ret);
            return (Command.CommandDelegate)dynamicMethod.CreateDelegate(typeof(Command.CommandDelegate));
        }
    }
}
