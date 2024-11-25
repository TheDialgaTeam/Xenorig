#include "ConsoleLogger.h"

#include <spdlog/logger.h>

#ifdef _WIN32
    #include <windows.h>
#endif

namespace Xenorig::Logging
{
    spdlog::level::level_enum ConsoleLogger::ConvertFrom(const Abstractions::LogLevel logLevel)
    {
        switch (logLevel)
        {
            case Abstractions::Trace: return spdlog::level::trace;
            case Abstractions::Debug: return spdlog::level::debug;
            case Abstractions::Information: return spdlog::level::info;
            case Abstractions::Warning: return spdlog::level::warn;
            case Abstractions::Error: return spdlog::level::err;
            case Abstractions::Critical: return spdlog::level::critical;
            default: return spdlog::level::off;
        }
    }

    ConsoleLogger::ConsoleLogger(const std::string &categoryName, ConsoleLoggerConfiguration &configuration) :
        console(spdlog::stdout_color_mt(categoryName))
    {
#if _WIN32
        const auto stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        DWORD consoleMode;

        if (stdoutHandle != nullptr && GetConsoleMode(stdoutHandle, &consoleMode))
        {
            SetConsoleMode(stdoutHandle, consoleMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
#endif

        if (configuration.Formatter.has_value())
        {
            console->set_formatter(std::move(configuration.Formatter.value()));
        }

        if (configuration.Pattern.has_value())
        {
            console->set_pattern(configuration.Pattern.value());
        }

        if (configuration.FlushLevel.has_value())
        {
            console->flush_on(configuration.FlushLevel.value());
        }

        if (configuration.ErrorHandler.has_value())
        {
            console->set_error_handler(std::move(configuration.ErrorHandler.value()));
        }
    }

    bool ConsoleLogger::IsEnabled(const Abstractions::LogLevel logLevel)
    {
        return console->should_log(ConvertFrom(logLevel));
    }

    void ConsoleLogger::Log(const Abstractions::LogLevel logLevel, const std::string_view message)
    {
        if (IsEnabled(logLevel))
        {
            console->log(ConvertFrom(logLevel), message);
        }
    }
} // namespace Xenorig::Logging