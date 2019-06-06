using System;
using System.Collections.Generic;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Console
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

    public sealed class ConsoleMessageBuilder
    {
        private List<ConsoleMessage> MessagesToLog { get; } = new List<ConsoleMessage>();

        public ConsoleMessageBuilder Write(string message, ConsoleColor color = ConsoleColor.White, bool includeDateTime = false)
        {
            MessagesToLog.Add(new ConsoleMessage($"{message}", color, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, ConsoleColor color = ConsoleColor.White, bool includeDateTime = true)
        {
            MessagesToLog.Add(new ConsoleMessage($"{message}{Environment.NewLine}", color, includeDateTime));
            return this;
        }

        public List<ConsoleMessage> Build()
        {
            return MessagesToLog;
        }
    }
}