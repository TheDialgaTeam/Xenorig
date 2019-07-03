using System;

namespace TheDialgaTeam.Xiropht.Xirorig.Console
{
    public sealed class ConsoleMessage
    {
        public string Message { get; }

        public ConsoleColor Color { get; }

        public bool IncludeDateTime { get; }

        public ConsoleMessage(string message, ConsoleColor color, bool includeDateTime)
        {
            Message = message;
            Color = color;
            IncludeDateTime = includeDateTime;
        }
    }
}