using System;
using System.Collections.Generic;

namespace TheDialgaTeam.Xiropht.Xirorig.Console
{
    public sealed class ConsoleMessageBuilder
    {
        private List<ConsoleMessage> MessagesToLog { get; } = new List<ConsoleMessage>();

        public ConsoleMessageBuilder Write(string message)
        {
            MessagesToLog.Add(new ConsoleMessage(message, ConsoleColor.White, true));
            return this;
        }

        public ConsoleMessageBuilder Write(string message, ConsoleColor color)
        {
            MessagesToLog.Add(new ConsoleMessage(message, color, true));
            return this;
        }

        public ConsoleMessageBuilder Write(string message, bool includeDateTime)
        {
            MessagesToLog.Add(new ConsoleMessage(message, ConsoleColor.White, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder Write(string message, ConsoleColor color, bool includeDateTime)
        {
            MessagesToLog.Add(new ConsoleMessage(message, color, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message)
        {
            MessagesToLog.Add(new ConsoleMessage($"{message}{Environment.NewLine}", ConsoleColor.White, true));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, ConsoleColor color)
        {
            MessagesToLog.Add(new ConsoleMessage($"{message}{Environment.NewLine}", color, true));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, bool includeDateTime)
        {
            MessagesToLog.Add(new ConsoleMessage($"{message}{Environment.NewLine}", ConsoleColor.White, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, ConsoleColor color, bool includeDateTime)
        {
            MessagesToLog.Add(new ConsoleMessage($"{message}{Environment.NewLine}", color, includeDateTime));
            return this;
        }

        public IEnumerable<ConsoleMessage> Build()
        {
            return MessagesToLog;
        }
    }
}