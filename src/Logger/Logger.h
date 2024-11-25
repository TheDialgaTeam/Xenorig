#ifndef LOGGER_H
#define LOGGER_H

#include "spdlog/spdlog.h"

#include <memory>

namespace Xenorig::Logger
{
    class Logger
    {
        std::shared_ptr<spdlog::logger> console;
        std::shared_ptr<spdlog::logger> consoleHeader;

    public:
        Logger();

        const std::shared_ptr<spdlog::logger> &GetConsole(bool isHeader) const;
    };
} // namespace Xenorig::Logger

#endif // LOGGER_H
