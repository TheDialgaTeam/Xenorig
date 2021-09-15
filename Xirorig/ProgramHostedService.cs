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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Miner;
using Xirorig.Utilities;

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

            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}* {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {AnsiEscapeCodeConstants.CyanForegroundColor}{{ApplicationName:l}}/{{Version:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{FrameworkVersion:l}}{AnsiEscapeCodeConstants.Reset}", false, "ABOUT", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}* {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{CpuInfo:l}} {AnsiEscapeCodeConstants.GreenForegroundColor}{{CpuInstructionSets:l}}{AnsiEscapeCodeConstants.Reset}", false, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
            logger.LogInformation($"   {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}L2:{AnsiEscapeCodeConstants.CyanForegroundColor}{{L2Cache:F1}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}MB L3:{AnsiEscapeCodeConstants.CyanForegroundColor}{{L3Cache:F1}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}MB {AnsiEscapeCodeConstants.CyanForegroundColor}{{Core}}{AnsiEscapeCodeConstants.DarkGrayForegroundColor}C/{AnsiEscapeCodeConstants.CyanForegroundColor}{{Thread}}{AnsiEscapeCodeConstants.DarkGrayForegroundColor}T{AnsiEscapeCodeConstants.Reset}", false, "", CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}* {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{DonatePercentage}}%{AnsiEscapeCodeConstants.Reset}", false, "DONATE", _context.Options.GetDonatePercentage());
            logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}* {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {AnsiEscapeCodeConstants.MagentaForegroundColor}h{AnsiEscapeCodeConstants.DarkGrayForegroundColor}ashrate, {AnsiEscapeCodeConstants.MagentaForegroundColor}s{AnsiEscapeCodeConstants.DarkGrayForegroundColor}tats, {AnsiEscapeCodeConstants.MagentaForegroundColor}j{AnsiEscapeCodeConstants.DarkGrayForegroundColor}ob{AnsiEscapeCodeConstants.Reset}", false, "COMMANDS");

            try
            {
                // Initialize Miner Instances
                var optionsMinerInstances = _context.Options.GetMinerInstances();

                _minerInstances = new MinerInstance[optionsMinerInstances.Length];
                _maxHashes = new double[optionsMinerInstances.Length];

                for (var i = 0; i < optionsMinerInstances.Length; i++)
                {
                    _minerInstances[i] = new MinerInstance(_context, i, optionsMinerInstances[i]);
                }

                for (var i = 0; i < optionsMinerInstances.Length; i++)
                {
                    _minerInstances[i].StartInstance();
                }

                _calculateTotalAverageHash.Change(TimeSpan.FromSeconds(_context.Options.GetPrintTime()), TimeSpan.FromSeconds(_context.Options.GetPrintTime()));
                _consoleThread.Start();
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
                var logger = _context.Logger;

                switch (keyPressed.KeyChar)
                {
                    case 'h':
                    {
                        logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Dash:l}}{AnsiEscapeCodeConstants.Reset}", true, "=".PadRight(75, '='));

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

                            logger.LogInformation("MINER INSTANCE #{Index}", true, index + 1);
                            logger.LogInformation("| THREAD | 10s H/s | 60s H/s | 15m H/s |", true);

                            for (var i = 0; i < threadCount; i++)
                            {
                                logger.LogInformation("| {i,-6} | {j,-7:F1} | {k,-7:F1} | {l,-7:F1} |", true, i + 1, minerInstance.AverageHashCalculatedIn10Seconds[i], minerInstance.AverageHashCalculatedIn60Seconds[i], minerInstance.AverageHashCalculatedIn15Minutes[i]);
                            }

                            logger.LogInformation($"{AnsiEscapeCodeConstants.WhiteForegroundColor}speed {AnsiEscapeCodeConstants.DarkGrayForegroundColor}10s/60s/15m {AnsiEscapeCodeConstants.CyanForegroundColor}{{Average10SecondsSum:F1}} {AnsiEscapeCodeConstants.BlueForegroundColor}{{Average60SecondsSum:F1}} {{Average15MinutesSum:F1}} {AnsiEscapeCodeConstants.CyanForegroundColor}H/s {AnsiEscapeCodeConstants.DarkGrayForegroundColor}max {AnsiEscapeCodeConstants.CyanForegroundColor}{{MaxHash:F1}}{AnsiEscapeCodeConstants.Reset}", true, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHashes[index]);
                        }

                        logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Dash:l}}{AnsiEscapeCodeConstants.Reset}", true, "=".PadRight(75, '='));
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

            var minerInstances = _minerInstances;
            var minerInstancesLength = minerInstances.Length;

            for (var index = 0; index < minerInstancesLength; index++)
            {
                var minerInstance = minerInstances[index];
                double average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
                var threadCount = minerInstance.AverageHashCalculatedIn10Seconds.Length;

                for (var i = threadCount - 1; i >= 0; i--)
                {
                    average10SecondsSum += minerInstance.AverageHashCalculatedIn10Seconds[i];
                    average60SecondsSum += minerInstance.AverageHashCalculatedIn60Seconds[i];
                    average15MinutesSum += minerInstance.AverageHashCalculatedIn15Minutes[i];
                }

                _maxHashes[index] = Math.Max(Math.Max(Math.Max(_maxHashes[index], average10SecondsSum), average60SecondsSum), average15MinutesSum);

                logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}speed {AnsiEscapeCodeConstants.DarkGrayForegroundColor}10s/60s/15m {AnsiEscapeCodeConstants.CyanForegroundColor}{{Average10SecondsSum:F1}} {AnsiEscapeCodeConstants.BlueForegroundColor}{{Average60SecondsSum:F1}} {{Average15MinutesSum:F1}} {AnsiEscapeCodeConstants.CyanForegroundColor}H/s {AnsiEscapeCodeConstants.DarkGrayForegroundColor}max {AnsiEscapeCodeConstants.CyanForegroundColor}{{MaxHash:F1}}{AnsiEscapeCodeConstants.Reset}", true, index + 1, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHashes[index]);
            }
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