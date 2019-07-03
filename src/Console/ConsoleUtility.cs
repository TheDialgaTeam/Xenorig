using System;

namespace TheDialgaTeam.Xiropht.Xirorig.Console
{
    public static class ConsoleUtility
    {
        public static void DoYesNoOptions(string question, out bool selectedOption)
        {
            string option;

            do
            {
                System.Console.Out.WriteLine($"{question} [Y/N]:");
                option = System.Console.In.ReadLine();

                if (!string.IsNullOrWhiteSpace(option) && (option.Equals("y", StringComparison.OrdinalIgnoreCase) || option.Equals("n", StringComparison.OrdinalIgnoreCase)))
                    continue;

                System.Console.BackgroundColor = ConsoleColor.Red;
                System.Console.Out.WriteLine("Invalid option. Please try again.");
                System.Console.ResetColor();
            } while (string.IsNullOrWhiteSpace(option) || !option.Equals("y", StringComparison.OrdinalIgnoreCase) && !option.Equals("n", StringComparison.OrdinalIgnoreCase));

            selectedOption = option.Equals("y", StringComparison.OrdinalIgnoreCase);
        }
    }
}