#ifndef XENORIG_LOGGING_ILOGGERPROVIDER_H
#define XENORIG_LOGGING_ILOGGERPROVIDER_H

#include "ILogger.h"

#include <memory>
#include <string>

namespace Xenorig::Logging::Abstractions
{
    class ILoggerProvider
    {
    public:
        virtual ~ILoggerProvider() = default;
        virtual std::shared_ptr<ILogger> CreateLogger(std::string categoryName) = 0;
    };
} // namespace Xenorig::Logging::Abstractions

#endif // XENORIG_LOGGING_ILOGGERPROVIDER_H
