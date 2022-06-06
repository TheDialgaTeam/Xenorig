using Microsoft.Extensions.Logging;
using static TheDialgaTeam.Core.Logging.Microsoft.AnsiEscapeCodeConstants;

namespace Xenorig;

internal static partial class Logger
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = $" {GreenForegroundColor}* {WhiteForegroundColor}{{category,-12}} {CyanForegroundColor}{{applicationName}}/{{version}} {DarkGrayForegroundColor}{{frameworkVersion}}{Reset}")]
    public static partial void PrintAbout(ILogger logger, string category, string applicationName, string version, string frameworkVersion);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = $" {GreenForegroundColor}* {WhiteForegroundColor}{{category,-12}} {DarkGrayForegroundColor}{{processorName}} {GreenForegroundColor}{{processorInstructionsSupported}}{Reset}")]
    public static partial void PrintCpu(ILogger logger, string category, string processorName, string processorInstructionsSupported);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = $"   {WhiteForegroundColor}{{category,-12}} {DarkGrayForegroundColor}L2: {CyanForegroundColor}{{l2Cache:F1}} {DarkGrayForegroundColor}MB L3: {CyanForegroundColor}{{l3Cache:F1}} {DarkGrayForegroundColor}MB {CyanForegroundColor}{{coreCount}}{DarkGrayForegroundColor}C/{CyanForegroundColor}{{threadCount}}{DarkGrayForegroundColor}T{Reset}")]
    public static partial void PrintCpuCont(ILogger logger, string category, double l2Cache, double l3Cache, int coreCount, int threadCount);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = $" {GreenForegroundColor}* {WhiteForegroundColor}{{category,-12:l}} {DarkGrayForegroundColor}{{donatePercentage}}%{Reset}")]
    public static partial void PrintDonatePercentage(ILogger logger, string category, int donatePercentage);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = $" {GreenForegroundColor}* {WhiteForegroundColor}{{category,-12:l}} {MagentaForegroundColor}h{DarkGrayForegroundColor}ashrate, {MagentaForegroundColor}s{DarkGrayForegroundColor}tats, {MagentaForegroundColor}j{DarkGrayForegroundColor}ob{Reset}")]
    public static partial void PrintCommand(ILogger logger, string category);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = $" {GreenForegroundColor}* {WhiteForegroundColor}{{category,-12:l}} {{url}} algo {{algorithm}}")]
    public static partial void PrintPool(ILogger logger, string category, string url, string algorithm);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = $"{DarkRedForegroundColor}[{{host}}] Login failed. Reason: {{reason}}{Reset}")]
    public static partial void PrintLoginFailed(ILogger logger, string host, string reason);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = $"{DarkRedForegroundColor}Disconnected.{Reset}")]
    public static partial void PrintDisconnected(ILogger logger);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = $"use {{mode}} {CyanForegroundColor}{{host}} {GrayForegroundColor}({{ping}} ms){Reset}")]
    public static partial void PrintConnected(ILogger logger, string mode, string host, double ping);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = $"{GreenForegroundColor}READY (CPU) {WhiteForegroundColor}threads {CyanForegroundColor}{{threads}}{Reset}")]
    public static partial void PrintCpuMinerReady(ILogger logger, int threads);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = $"{MagentaForegroundColor}{{job}}{Reset} from {WhiteForegroundColor}{{host}}{Reset} diff {WhiteForegroundColor}{{difficulty}}{Reset} algo {WhiteForegroundColor}{{algorithm}}{Reset} height {WhiteForegroundColor}{{height}}{Reset}")]
    public static partial void PrintJob(ILogger logger, string job, string host, ulong difficulty, string algorithm, int height);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = $"{BlueForegroundColor}Thread: {{threadId,-2}} | Job Type: {{jobType}} | Job Total: {{jobTotal}} | Job Chunk: {{startIndex}}-{{endIndex}} ({{size}}){Reset}")]
    public static partial void PrintCurrentThreadJob(ILogger logger, int threadId, string jobType, long jobTotal, long startIndex, long endIndex, long size);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = $"{GreenForegroundColor}Thread: {{threadId,-2}} | Job Type: {{jobType}} | Block found: {{firstNumber}} {{operatorSymbol}} {{secondNumber}} = {{result}}{Reset}")]
    public static partial void PrintBlockFound(ILogger logger, int threadId, string jobType, long firstNumber, char operatorSymbol, long secondNumber, long result);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = $"{GreenForegroundColor}accepted {Reset}({{goodTotal}}/{{badTotal}}) {GrayForegroundColor}({{ping}} ms){Reset}")]
    public static partial void PrintBlockAcceptResult(ILogger logger, ulong goodTotal, ulong badTotal, double ping);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = $"{RedForegroundColor}rejected {Reset}({{goodTotal}}/{{badTotal}}) - {{reason}} {GrayForegroundColor}({{ping}} ms){Reset}")]
    public static partial void PrintBlockRejectResult(ILogger logger, ulong goodTotal, ulong badTotal, string reason, double ping);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = $"{WhiteForegroundColor}speed {DarkGrayForegroundColor}10s/60s/15m {CyanForegroundColor}{{average10SecondsSum:F0}} {BlueForegroundColor}{{average60SecondsSum:F0}} {{average15MinutesSum:F0}} {CyanForegroundColor}H/s {DarkGrayForegroundColor}max {CyanForegroundColor}{{maxHash:F0}}{Reset}")]
    public static partial void PrintCpuMinerSpeed(ILogger logger, decimal average10SecondsSum, decimal average60SecondsSum, decimal average15MinutesSum, decimal maxHash);

#if DEBUG
    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "Receive Packet: {packet}")]
    public static partial void PrintRawPacket(ILogger logger, string packet);

    [LoggerMessage(EventId = 17, Level = LogLevel.Debug, Message = "Error: {message}")]
    public static partial void PrintException(ILogger logger, Exception exception, string message);
#endif
}