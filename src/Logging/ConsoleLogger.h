#ifndef XENORIG_LOGGING_CONSOLELOGGER_H
#define XENORIG_LOGGING_CONSOLELOGGER_H

#include "Abstractions/ILogger.h"

#include <optional>
#include <memory>
#include <spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>

namespace Xenorig::Logging
{
    struct ConsoleLoggerConfiguration
    {
        std::optional<std::unique_ptr<spdlog::formatter>> Formatter;
        std::optional<std::string> Pattern;
        std::optional<spdlog::level::level_enum> FlushLevel;
        std::optional<spdlog::err_handler> ErrorHandler;
    };

    class ConsoleLogger final : public Abstractions::ILogger
    {
        std::shared_ptr<spdlog::logger> console;

        static spdlog::level::level_enum ConvertFrom(Abstractions::LogLevel logLevel);

    public:
        explicit ConsoleLogger(const std::string &categoryName, ConsoleLoggerConfiguration &configuration);
        ~ConsoleLogger() override = default;

        bool IsEnabled(Abstractions::LogLevel logLevel) override;
        void Log(Abstractions::LogLevel logLevel, std::string_view message) override;
    };
} // namespace Xenorig::Logging

#endif // XENORIG_LOGGING_CONSOLELOGGER_H
