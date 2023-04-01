using Microsoft.Extensions.Logging;
using static TheDialgaTeam.Core.Logging.Microsoft.AnsiEscapeCodeConstants;

namespace Xenorig;

public static partial class Logger
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

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "")]
    public static partial void PrintEmpty(ILogger logger);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = $"{DarkRedForegroundColor}[{{host}}] Login failed. Reason: {{reason}}{Reset}")]
    public static partial void PrintLoginFailed(ILogger logger, string host, string reason);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = $"{DarkRedForegroundColor}[{{host}}] Disconnected. Reason: {{reason}}{Reset}")]
    public static partial void PrintDisconnected(ILogger logger, string host, string reason);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = $"use {{mode}} {CyanForegroundColor}{{host}}{Reset}")]
    public static partial void PrintConnected(ILogger logger, string mode, string host);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = $"{GreenForegroundColor}READY (CPU) {WhiteForegroundColor}threads {CyanForegroundColor}{{threads}}{Reset}")]
    public static partial void PrintCpuMinerReady(ILogger logger, int threads);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = $"{MagentaForegroundColor}{{job}}{Reset} from {WhiteForegroundColor}{{host}}{Reset} diff {WhiteForegroundColor}{{difficulty}}{Reset} algo {WhiteForegroundColor}{{algorithm}}{Reset} height {WhiteForegroundColor}{{height}}{Reset}")]
    public static partial void PrintJob(ILogger logger, string job, string host, long difficulty, string algorithm, int height);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = $"{BlueForegroundColor}Thread: {{threadId,-2}} | Job Type: {{jobType}} | Job Iterations: {{jobTotal}} | Job Chunk: {{startIndex}}-{{endIndex}} ({{size}}){Reset}")]
    public static partial void PrintCurrentChunkedThreadJob(ILogger logger, int threadId, string jobType, long jobTotal, long startIndex, long endIndex, long size);
    
    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = $"{BlueForegroundColor}Thread: {{threadId,-2}} | Job Type: {{jobType}}{Reset}")]
    public static partial void PrintCurrentThreadJob(ILogger logger, int threadId, string jobType);

    
    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = $"{GreenForegroundColor}Thread: {{threadId,-2}} | Job Type: {{jobType}} | Block found: {{firstNumber}} {{operatorSymbol}} {{secondNumber}} = {{result}}{Reset}")]
    public static partial void PrintBlockFound(ILogger logger, int threadId, string jobType, long firstNumber, char operatorSymbol, long secondNumber, long result);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = $"{GreenForegroundColor}accepted {Reset}({{goodTotal}}/{{badTotal}}) {GrayForegroundColor}({{ping}} ms){Reset}")]
    public static partial void PrintBlockAcceptResult(ILogger logger, int goodTotal, int badTotal, double ping);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = $"{RedForegroundColor}rejected {Reset}({{goodTotal}}/{{badTotal}}) - {{reason}} {GrayForegroundColor}({{ping}} ms){Reset}")]
    public static partial void PrintBlockRejectResult(ILogger logger, int goodTotal, int badTotal, string reason, double ping);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = $"{WhiteForegroundColor}speed {DarkGrayForegroundColor}10s/60s/15m {CyanForegroundColor}{{average10SecondsSum:F1}} {BlueForegroundColor}{{average60SecondsSum:F1}} {{average15MinutesSum:F1}} {CyanForegroundColor}H/s {DarkGrayForegroundColor}max {CyanForegroundColor}{{maxHash:F1}}{Reset}")]
    public static partial void PrintCpuMinerSpeed(ILogger logger, double average10SecondsSum, double average60SecondsSum, double average15MinutesSum, double maxHash);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "|      | Easy Blocks | Semi Random | Random Blocks | Total Blocks |")]
    public static partial void PrintXenophyteCentralizedStatsHeader(ILogger logger);

    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = $"| {GreenForegroundColor}Good{Reset} | {{easy,-11}} | {{semi,-11}} | {{random,-13}} | {{total,-12}} |")]
    public static partial void PrintXenophyteCentralizedStatsGood(ILogger logger, int easy, int semi, int random, int total);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = $"| {RedForegroundColor}Bad{Reset}  | {{easy,-11}} | {{semi,-11}} | {{random,-13}} | {{total,-12}} |")]
    public static partial void PrintXenophyteCentralizedStatsBad(ILogger logger, int easy, int semi, int random, int total);

    [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "| THREAD |  10s H/s  |  60s H/s  |  15m H/s  |")]
    public static partial void PrintCpuMinerSpeedHeader(ILogger logger);

    [LoggerMessage(EventId = 23, Level = LogLevel.Information, Message = "| {threadId,-6} | {hash,-9:F1} | {hash2,-9:F1} | {hash3,-9:F1} |")]
    public static partial void PrintCpuMinerSpeedBreakdown(ILogger logger, int threadId, double hash, double hash2, double hash3);
    
    [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = $"{BlueForegroundColor}Thread: {{threadId,-2}} | Thread has finished all the possible combinations.{Reset}")]
    public static partial void PrintCurrentThreadJobDone(ILogger logger, int threadId);
}