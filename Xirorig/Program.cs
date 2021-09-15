// Xirorig
// Copyright 2021 Yong Jian Ming
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using TheDialgaTeam.Core.Logger.Serilog.Sinks;
using Xirorig.Options;

namespace Xirorig
{
    internal static class Program
    {
        public const string XirorigNativeLibrary = "xirorig_native";

        private static async Task Main(string[] args)
        {
            try
            {
                await CreateHostBuilder(args).RunConsoleAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostBuilderContext, serviceCollection) =>
                {
                    serviceCollection.Configure<XirorigOptions>(hostBuilderContext.Configuration.GetSection("Xirorig"));

                    // Logger
                    serviceCollection.AddLoggingTemplate(new LoggerTemplateConfiguration(configuration =>
                    {
                        configuration.Global.DefaultPrefixTemplate = $"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{DateTimeOffset:yyyy-MM-dd HH:mm:ss}}{AnsiEscapeCodeConstants.Reset} ";
                        configuration.Global.DefaultPrefixTemplateArgs = () => new object[] { DateTimeOffset.Now };
                    }));

                    // Application Context
                    serviceCollection.AddSingleton<ProgramContext>();

                    // Program
                    serviceCollection.AddHostedService<ProgramHostedService>();
                })
                .UseSerilog((hostBuilderContext, _, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .WriteTo.AnsiConsole(new AnsiOutputTemplateTextFormatter("{Message}{NewLine}{Exception}"));
                });
        }
    }
}