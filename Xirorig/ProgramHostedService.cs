using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Miner;
using Xirorig.Utility;

namespace Xirorig
{
    internal class ProgramHostedService : IHostedService, IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly Thread _consoleThread;

        private MinerInstance[] _minerInstances = Array.Empty<MinerInstance>();

        public ProgramHostedService(ApplicationContext context)
        {
            _context = context;
            _consoleThread = new Thread(StartConsoleThread) { Name = "Console Thread", IsBackground = true };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";

            var logger = _context.Logger;

            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}{{ApplicationName:l}}/{{Version:l}}{AnsiEscapeCodeConstants.Reset} {{FrameworkVersion:l}}", false, "ABOUT", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {{Color:l}}{{IsVectorSupported:l}}{AnsiEscapeCodeConstants.Reset}", false, "SIMD", Vector.IsHardwareAccelerated ? AnsiEscapeCodeConstants.GreenForegroundColor : AnsiEscapeCodeConstants.RedForegroundColor, Vector.IsHardwareAccelerated ? "available" : "unavailable");
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {{CpuInfo:l}} {AnsiEscapeCodeConstants.GreenForegroundColor}{{CpuInstructionSets:l}}{AnsiEscapeCodeConstants.Reset}", false, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
            logger.LogInformation($"   {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.GrayForegroundColor}L2:{AnsiEscapeCodeConstants.Reset}{{L2Cache:F1}} MB {AnsiEscapeCodeConstants.GrayForegroundColor}L3:{AnsiEscapeCodeConstants.Reset}{{L3Cache:F1}} MB {AnsiEscapeCodeConstants.CyanForegroundColor}{{Core}}{AnsiEscapeCodeConstants.Reset}C/{AnsiEscapeCodeConstants.CyanForegroundColor}{{Thread}}{AnsiEscapeCodeConstants.Reset}T", false, "", CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {{DonatePercentage}}%", false, "DONATE", _context.Options.GetDonatePercentage());
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.MagentaForegroundColor}h{AnsiEscapeCodeConstants.Reset}ashrate, {AnsiEscapeCodeConstants.MagentaForegroundColor}s{AnsiEscapeCodeConstants.Reset}tats, {AnsiEscapeCodeConstants.MagentaForegroundColor}j{AnsiEscapeCodeConstants.Reset}ob", false, "COMMANDS");

            _consoleThread.Start();

            try
            {
                // Initialize Miner Instances
                var minerInstances = _context.Options.GetMinerInstances();

                _minerInstances = new MinerInstance[minerInstances.Length];

                for (var i = 0; i < minerInstances.Length; i++)
                {
                    _minerInstances[i] = new MinerInstance(_context, minerInstances[i], i);
                }

                for (var i = 0; i < minerInstances.Length; i++)
                {
                    _minerInstances[i].StartInstance();
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Oops, this miner has caught an exception.", true);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void StartConsoleThread()
        {
            var cancellationToken = _context.ApplicationShutdownCancellationToken;

            while (!cancellationToken.IsCancellationRequested)
            {
                var keyPressed = Console.ReadKey(true);

                switch (keyPressed.KeyChar)
                {
                    case 'h':
                        break;

                    case 's':
                        break;

                    case 'j':
                        break;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}