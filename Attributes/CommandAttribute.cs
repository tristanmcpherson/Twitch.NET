using System;

namespace DitsyTwitch.Attributes
{

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; set; }
        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}
