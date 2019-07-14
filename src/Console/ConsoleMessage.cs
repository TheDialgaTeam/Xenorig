using System;

namespace TheDialgaTeam.Xiropht.Xirorig.Console
{
    public sealed class ConsoleMessage
    {
        public bool IncludeDateTime { get; }

        public string Message { get; }

        public ConsoleColor Color { get; }

        public ConsoleMessage(string message, ConsoleColor color) : this(message, color, true)
        {
        }

        public ConsoleMessage(string message, bool includeDateTime) : this(message, ConsoleColor.White, includeDateTime)
        {
        }

        public ConsoleMessage(string message, ConsoleColor color = ConsoleColor.White, bool includeDateTime = true)
        {
            Message = message;
            Color = color;
            IncludeDateTime = includeDateTime;
        }
    }
}