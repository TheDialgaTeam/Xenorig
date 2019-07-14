using System;
using System.Collections.Generic;

namespace TheDialgaTeam.Xiropht.Xirorig.Console
{
    public sealed class ConsoleMessageBuilder
    {
        private List<ConsoleMessage> ConsoleMessages { get; } = new List<ConsoleMessage>();

        public ConsoleMessageBuilder Write(string message)
        {
            ConsoleMessages.Add(new ConsoleMessage(message));
            return this;
        }

        public ConsoleMessageBuilder Write(string message, ConsoleColor color)
        {
            ConsoleMessages.Add(new ConsoleMessage(message, color, true));
            return this;
        }

        public ConsoleMessageBuilder Write(string message, bool includeDateTime)
        {
            ConsoleMessages.Add(new ConsoleMessage(message, ConsoleColor.White, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder Write(string message, ConsoleColor color, bool includeDateTime)
        {
            ConsoleMessages.Add(new ConsoleMessage(message, color, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message)
        {
            ConsoleMessages.Add(new ConsoleMessage($"{message}{Environment.NewLine}"));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, ConsoleColor color)
        {
            ConsoleMessages.Add(new ConsoleMessage($"{message}{Environment.NewLine}", color, true));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, bool includeDateTime)
        {
            ConsoleMessages.Add(new ConsoleMessage($"{message}{Environment.NewLine}", ConsoleColor.White, includeDateTime));
            return this;
        }

        public ConsoleMessageBuilder WriteLine(string message, ConsoleColor color, bool includeDateTime)
        {
            ConsoleMessages.Add(new ConsoleMessage($"{message}{Environment.NewLine}", color, includeDateTime));
            return this;
        }

        public List<ConsoleMessage> Build()
        {
            return ConsoleMessages;
        }
    }
}