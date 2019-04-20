using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace TwitchBot.Command
{
	public static class Argument
	{
		public static Dictionary<Type, Func<string, (bool, object)>> Parse =
			new Dictionary<Type, Func<string, (bool, object)>>
			{
				{ typeof(string), s => (true, s) },
				{ typeof(int), s =>
					{
						var success = int.TryParse(s, out var num);
						return (success, num);
					}
				},
				{ typeof(float), s =>
					{
						var success = float.TryParse(s, out var num);
						return (success, num);
					}
				}
			};

	}


	public class Command
    {
        public delegate Task CommandDelegate(ChatMessage chatMessage, string[] arguments);
        public string Name { get; set; }
        public CommandDelegate Execute { get; set; }
	    public Type[] Arguments { get; set; }
    }
}
