using System;

namespace TwitchBot.Attributes
{

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; }
        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}
