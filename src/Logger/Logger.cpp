#include "Logger.h"

#include "AnsiEscapeCodeConstants.h"

#include <spdlog/sinks/stdout_color_sinks.h>
#include <sstream>

#ifdef _WIN32
    #include <windows.h>
#endif

namespace Xenorig::Logger
{
    Logger::Logger() :
        console(spdlog::stdout_color_mt("console")),
        consoleHeader(spdlog::stdout_color_mt("consoleHeader"))
    {
#if _WIN32
        const auto stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        DWORD consoleMode;

        if (stdoutHandle != nullptr && GetConsoleMode(stdoutHandle, &consoleMode))
        {
            SetConsoleMode(stdoutHandle, consoleMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
#endif

        std::stringstream ss;
        ss << AnsiEscapeCodeConstants::DarkGrayForegroundColor << "%Y-%m-%d %H:%M:%S" << AnsiEscapeCodeConstants::Reset
           << " %v";

        console->set_pattern(ss.str());
        consoleHeader->set_pattern("%v");

        set_default_logger(console);
    }

    const std::shared_ptr<spdlog::logger> &Logger::GetConsole(const bool isHeader) const
    {
        return isHeader ? consoleHeader : console;
    }
} // namespace Xenorig::Logger
