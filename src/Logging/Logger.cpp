#include "Logging/Logger.h"

#include <fmt/format.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/spdlog.h>

#include "About.h"
#include "Logging/Console/AnsiEscapeCodeConstants.h"
#include "Utility/CpuInformation.h"

#ifdef _WIN32
    #include <windows.h>
#endif

namespace Xenorig::Logging
{
    void ConfigureLogging()
    {
#if _WIN32
        const auto stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        DWORD consoleMode;

        if (stdoutHandle != nullptr && GetConsoleMode(stdoutHandle, &consoleMode))
        {
            SetConsoleMode(stdoutHandle, consoleMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
#endif

        const auto console = spdlog::stdout_color_mt("Console");
        console->set_pattern(fmt::format("{}%Y-%m-%d %H:%M:%S{} %v",
                                         Console::AnsiEscapeCodeConstants::DarkGrayForegroundColor,
                                         Console::AnsiEscapeCodeConstants::Reset));

        const auto consoleNoTimestamp = spdlog::stdout_color_mt("ConsoleNoTimestamp");
        consoleNoTimestamp->set_pattern("%v");

#ifdef NDEBUG
        spdlog::set_level(spdlog::level::info);
#else
        spdlog::set_level(spdlog::level::trace);
#endif
    }

    void PrintAbout()
    {
        const auto console = spdlog::get("ConsoleNoTimestamp");
        console->info(" {0}* {1}{5:<12} {2}{6}/{7} {3}{8}/{9}{4}",
                      Console::AnsiEscapeCodeConstants::GreenForegroundColor,
                      Console::AnsiEscapeCodeConstants::WhiteForegroundColor,
                      Console::AnsiEscapeCodeConstants::CyanForegroundColor,
                      Console::AnsiEscapeCodeConstants::DarkGrayForegroundColor,
                      Console::AnsiEscapeCodeConstants::Reset, "About", PROJECT_NAME,
                      BUILD_COMMIT_ID == "" ? PROJECT_VERSION : PROJECT_VERSION_LONG, COMPILER_ID, COMPILER_VERSION);
    }

    void PrintCpu()
    {
        const auto cpuInfo = Utility::CpuInformation::GetCpuInformation();
        const auto console = spdlog::get("ConsoleNoTimestamp");
        console->info(" {0}* {1}{5:<12} {2}{6} L2: {3}{7:.1f}{4}MB L3: {3}{8:.1f}{4}MB {3}{9:d}{4}C/{3}{10:d}{4}T",
                      Console::AnsiEscapeCodeConstants::GreenForegroundColor,
                      Console::AnsiEscapeCodeConstants::WhiteForegroundColor,
                      Console::AnsiEscapeCodeConstants::DarkGrayForegroundColor,
                      Console::AnsiEscapeCodeConstants::CyanForegroundColor, Console::AnsiEscapeCodeConstants::Reset,
                      "CPU", cpuInfo.ProcessorName, cpuInfo.L2CacheSize / 1024.0 / 1024.0,
                      cpuInfo.L3CacheSize / 1024.0 / 1024.0, cpuInfo.ProcessorCoreCount, cpuInfo.ProcessorCount);
    }
} // namespace Xenorig::Logging
