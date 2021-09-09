using System;
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
        private readonly ProgramContext _context;
        private readonly Thread _consoleThread;

        private readonly Timer _calculateTotalAverageHash;

        private MinerInstance[] _minerInstances = Array.Empty<MinerInstance>();
        private double[] _maxHashes = Array.Empty<double>();

        public ProgramHostedService(ProgramContext context)
        {
            _context = context;
            _consoleThread = new Thread(StartConsoleThread) { Name = "Console Thread", IsBackground = true };
            _calculateTotalAverageHash = new Timer(CalculateTotalAverageHash, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";

            var logger = _context.Logger;

            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}{{ApplicationName:l}}/{{Version:l}}{AnsiEscapeCodeConstants.Reset} {{FrameworkVersion:l}}", false, "ABOUT", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
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
                _maxHashes = new double[minerInstances.Length];

                for (var i = 0; i < minerInstances.Length; i++)
                {
                    _minerInstances[i] = new MinerInstance(_context, minerInstances[i], i);
                }

                for (var i = 0; i < minerInstances.Length; i++)
                {
                    _minerInstances[i].StartInstance();
                }

                _calculateTotalAverageHash.Change(TimeSpan.FromSeconds(_context.Options.GetPrintTime()), TimeSpan.FromSeconds(_context.Options.GetPrintTime()));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Oops, this miner has caught an exception.", true);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _calculateTotalAverageHash.Change(0, Timeout.Infinite);

            foreach (var minerInstance in _minerInstances)
            {
                minerInstance.StopInstance();
            }

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
                    {
                        _context.Logger.LogInformation("{Dash:l}", true, "=".PadRight(75, '='));

                        for (var index = 0; index < _minerInstances.Length; index++)
                        {
                            var minerInstance = _minerInstances[index];
                            double average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
                            var threadCount = minerInstance.AverageHashCalculatedIn10Seconds.Length;

                            for (var i = 0; i < threadCount; i++)
                            {
                                average10SecondsSum += minerInstance.AverageHashCalculatedIn10Seconds[i];
                                average60SecondsSum += minerInstance.AverageHashCalculatedIn60Seconds[i];
                                average15MinutesSum += minerInstance.AverageHashCalculatedIn15Minutes[i];
                            }

                            _context.Logger.LogInformation("MINER INSTANCE #{Index}", true, index + 1);
                            _context.Logger.LogInformation("| THREAD | 10s H/s | 60s H/s | 15m H/s |", true);

                            for (var i = 0; i < threadCount; i++)
                            {
                                _context.Logger.LogInformation("| {i,-6} | {j,-7:F1} | {k,-7:F1} | {l,-7:F1} |", true, i + 1, minerInstance.AverageHashCalculatedIn10Seconds[i], minerInstance.AverageHashCalculatedIn60Seconds[i], minerInstance.AverageHashCalculatedIn15Minutes[i]);
                            }

                            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.WhiteForegroundColor}speed{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}10s/60s/15m{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}{{Average10SecondsSum:F1}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.BlueForegroundColor}{{Average60SecondsSum:F1}} {{Average15MinutesSum:F1}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}H/s{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}max{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}{{MaxHash:F0}}{AnsiEscapeCodeConstants.Reset}", true, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHashes[index]);
                        }

                        _context.Logger.LogInformation("{Dash:l}", true, "=".PadRight(75, '='));
                        break;
                    }

                    case 's':
                        break;

                    case 'j':
                        break;
                }
            }
        }

        private void CalculateTotalAverageHash(object? state)
        {
            var logger = _context.Logger;

            logger.LogInformation("{Dash:l}", true, "=".PadRight(75, '='));

            var minerInstances = _minerInstances;
            var minerInstancesLength = minerInstances.Length;

            for (var index = 0; index < minerInstancesLength; index++)
            {
                var minerInstance = minerInstances[index];
                double average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
                var threadCount = minerInstance.AverageHashCalculatedIn10Seconds.Length;

                for (var i = 0; i < threadCount; i++)
                {
                    average10SecondsSum += minerInstance.AverageHashCalculatedIn10Seconds[i];
                    average60SecondsSum += minerInstance.AverageHashCalculatedIn60Seconds[i];
                    average15MinutesSum += minerInstance.AverageHashCalculatedIn15Minutes[i];
                }

                _maxHashes[index] = Math.Max(Math.Max(Math.Max(_maxHashes[index], average10SecondsSum), average60SecondsSum), average15MinutesSum);

                logger.LogInformation("MINER INSTANCE #{Index}", true, index + 1);
                logger.LogInformation($"{AnsiEscapeCodeConstants.WhiteForegroundColor}speed {AnsiEscapeCodeConstants.DarkGrayForegroundColor}10s/60s/15m {AnsiEscapeCodeConstants.CyanForegroundColor}{{Average10SecondsSum:F1}} {AnsiEscapeCodeConstants.BlueForegroundColor}{{Average60SecondsSum:F1}} {{Average15MinutesSum:F1}} {AnsiEscapeCodeConstants.CyanForegroundColor}H/s {AnsiEscapeCodeConstants.DarkGrayForegroundColor}max {AnsiEscapeCodeConstants.CyanForegroundColor}{{MaxHash:F1}}{AnsiEscapeCodeConstants.Reset}", true, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHashes[index]);
            }

            logger.LogInformation("{Dash:l}", true, "=".PadRight(75, '='));
        }

        public void Dispose()
        {
            _calculateTotalAverageHash.Dispose();

            foreach (var minerInstance in _minerInstances)
            {
                minerInstance.Dispose();
            }
        }
    }
}