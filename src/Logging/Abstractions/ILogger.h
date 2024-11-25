#ifndef XENORIG_LOGGING_ABSTRACTIONS_ILOGGER_H
#define XENORIG_LOGGING_ABSTRACTIONS_ILOGGER_H

#include "LogLevel.h"

#include <string>

namespace Xenorig::Logging::Abstractions
{
    class ILogger
    {
    public:
        virtual ~ILogger() = default;

        virtual bool IsEnabled(LogLevel logLevel) = 0;

        virtual void Log(LogLevel logLevel, std::string_view message) = 0;

        virtual void LogTrace(const std::string_view message)
        {
            Log(Trace, message);
        }

        virtual void LogDebug(const std::string_view message)
        {
            Log(Debug, message);
        }

        virtual void LogInformation(const std::string_view message)
        {
            Log(Information, message);
        }

        virtual void LogWarning(const std::string_view message)
        {
            Log(Warning, message);
        }

        virtual void LogError(const std::string_view message)
        {
            Log(Error, message);
        }

        virtual void LogCritical(const std::string_view message)
        {
            Log(Critical, message);
        }
    };
} // namespace Xenorig::Logging::Abstractions

#endif // XENORIG_LOGGING_ABSTRACTIONS_ILOGGER_H
