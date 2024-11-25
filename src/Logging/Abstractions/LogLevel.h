#ifndef XENORIG_LOGGING_ABSTRACTIONS_LOGLEVEL_H
#define XENORIG_LOGGING_ABSTRACTIONS_LOGLEVEL_H

namespace Xenorig::Logging::Abstractions
{
    enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical,
        None
    };
} // namespace Xenorig::Logging::Abstractions

#endif // XENORIG_LOGGING_ABSTRACTIONS_LOGLEVEL_H
