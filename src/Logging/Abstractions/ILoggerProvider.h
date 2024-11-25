#ifndef XENORIG_LOGGING_ILOGGERPROVIDER_H
#define XENORIG_LOGGING_ILOGGERPROVIDER_H

#include "ILogger.h"

#include <memory>

namespace Xenorig::Logging::Abstractions
{
    class ILoggerProvider
    {
    public:
        virtual ~ILoggerProvider() = default;
        virtual std::shared_ptr<ILogger> CreateLogger(std::string categoryName) = 0;
    };
} // namespace Xenorig::Logging

#endif // XENORIG_LOGGING_ILOGGERPROVIDER_H
